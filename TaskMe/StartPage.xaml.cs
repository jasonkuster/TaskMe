using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Hawaii;
using Microsoft.Hawaii.Relay.Client;
using Microsoft.Hawaii.Rendezvous.Client;
using System.IO.IsolatedStorage;
using System.Collections.ObjectModel;

namespace TaskMe
{
    public partial class StartPage : PhoneApplicationPage
    {
        IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

        public StartPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (!(bool)settings["FirstRun"])
                DescTextBox.Visibility = System.Windows.Visibility.Collapsed;
            else
                settings["FirstRun"] = false;
        }

        private void AcceptButton_Click(object sender, EventArgs e)
        {
            // Check whether the active endpoint object is null. If null, create a 
            // new end point. If not null, delete the existing one and recreate another
            // new end point.
            if (this.RelayContext.Endpoint != null)
            {
                // Delete the active endpoint.
                RelayService.DeleteEndPointAsync(HawaiiClient.HawaiiApplicationId, this.RelayContext.Endpoint, null);

                // Clear the created groups which were restored from storage.
                this.RelayContext.Groups.Clear();
            }

            string name = this.UsernameTextBox.Text;

            if (!string.IsNullOrEmpty(name) && name.ToCharArray().Length <= 50)
            {
                // Create a new endpoint.
                UsernameTextBox.IsEnabled = false;
                RelayService.CreateEndPointAsync(HawaiiClient.HawaiiApplicationId, "TaskMe", this.OnCompleteCreateEndPoint);
                ProgressBar.IsVisible = true;
            }
            else if (name.ToCharArray().Length > 50)
            {
                this.DisplayMessage("Username too long. Must be less than 50 characters.", "Error");
            }
            else
            {
                this.DisplayMessage("No name found. Enter a name and try again.", "Error");
            }

        }

        #region HawaiiCode

        private void OnCompleteCreateEndPoint(EndpointResult result)
        {
            if (result.Status == Status.Success)
            {
                // Set the newly created endpoint available in result as active(my) endpoint.
                this.RelayContext.Endpoint = result.EndPoint;
                this.Dispatcher.BeginInvoke(
                    delegate
                    {
                        string name = UsernameTextBox.Text;
                        RendezvousService.RegisterNameAsync(HawaiiClient.HawaiiApplicationId, name, this.OnCompleteRegisterName);
                    });
                NameRegistration nameRegistration = (NameRegistration)result.StateObject;
            }
            else
            {
                // Display a message box, in case of any error creating a new endpoint.
                DisplayMessage("Error creating endpoint.", "Error");
                error();
            }
        }

        private void OnCompleteRegisterName(NameRegistrationResult result)
        {
            if (result.Status == Status.Success)
            {
                this.Dispatcher.BeginInvoke(
                   delegate
                   {
                       this.NameRegistrationContext = new NameRegistrationContext(result.NameRegistration);

                       // Store the secret key in isolated storage.
                       RendezvousStorage.SetSecretKey(result.NameRegistration.Name, result.NameRegistration.SecretKey);

                       string name = result.NameRegistration.Name;
                       string registrationId = this.RelayContext.Endpoint.RegistrationId;
                       System.Diagnostics.Debug.WriteLine("Registered a name!: " + name+registrationId);

                       if (!string.IsNullOrEmpty(name) &&
                           !string.IsNullOrEmpty(registrationId))
                       {
                           string secretKey = result.NameRegistration.SecretKey;
                           if (string.IsNullOrEmpty(secretKey))
                           {
                               this.DisplayMessage("You are not the owner of this name or the name ownership details are lost. You can't perform this operation.", "Error");
                               error();
                               return;
                           }

                           NameRegistration nameRegistration = new NameRegistration()
                           {
                               Name = name,
                               Id = registrationId,
                               SecretKey = secretKey
                           };

                           RendezvousService.AssociateIdAsync(HawaiiClient.HawaiiApplicationId, nameRegistration, this.OnCompleteAssociateId, nameRegistration);
                       }
                   });
            }
            else
            {
                this.DisplayMessage("Name registration failed. Check your wifi/cellular connection.", "Error");
                error();
            }
        }

        public void OnCompleteAssociateId(NameRegistrationResult result)
        {
            System.Diagnostics.Debug.WriteLine(this.RelayContext.Endpoint.RegistrationId);
            if (result.Status == Status.Success)
            {
                NameRegistration nameRegistration = (NameRegistration)result.StateObject;
                this.Dispatcher.BeginInvoke(
                   delegate
                   {
                       this.NameRegistrationContext = new NameRegistrationContext(nameRegistration);
                       settings["MyUsername"] = nameRegistration.Name;
                       ((ObservableCollection<Person>)settings["ContactsList"]).Add(new Person
                       {
                           name = "Me",
                           username = nameRegistration.Name,
                           hawaiiID = this.RelayContext.Endpoint.RegistrationId,
                           reqSent = true,
                           accepted = true
                       });
                       foreach (MyProject search in ((ObservableCollection<MyProject>)settings["ProjectList"]))
                       {
                           search.creator = ((ObservableCollection<Person>)settings["ContactsList"])[0];
                           foreach (MyTask searchTask in search.tasks)
                           {
                               searchTask.assignedTo = ((ObservableCollection<Person>)settings["ContactsList"])[0];
                           }
                           foreach (MyTask searchTask in search.doneTasks)
                           {
                                searchTask.assignedTo = ((ObservableCollection<Person>)settings["ContactsList"])[0];
                           }
                       }
                   });

                DisplayMessage("You have successfully registered the name " + nameRegistration.Name, "Success!");
                this.Dispatcher.BeginInvoke(
                    delegate
                    {
                        ProgressBar.IsVisible = false;
                        NavigationService.GoBack();
                    });
            }
            else
            {
                this.DisplayMessage("Associating a registration id with a name failed. Check your wifi/cellular connection.", "Error");
                error();
            }
        }

        #endregion

        private void SkipButton_Click(object sender, EventArgs e)
        {
            settings["FirstRun"] = false;
            NavigationService.GoBack();
        }

        private void DisplayMessage(string message, string caption)
        {
            this.Dispatcher.BeginInvoke(
                    delegate
                    {
                        MessageBox.Show(message, caption, MessageBoxButton.OK);
                    });
        }

        private void error()
        {
            this.Dispatcher.BeginInvoke(
                delegate
                {
                    if (this.RelayContext.Endpoint != null)
                    {
                        // Delete the active endpoint.
                        RelayService.DeleteEndPointAsync(HawaiiClient.HawaiiApplicationId, this.RelayContext.Endpoint, null);
                        this.RelayContext.Endpoint = null;

                        // Clear the created groups which were restored from storage.
                        this.RelayContext.Groups.Clear();
                    }
                    ProgressBar.IsVisible = false;
                    UsernameTextBox.IsEnabled = true;
                });
        }

        private RelayContext RelayContext
        {
            get { return ((App)App.Current).RelayContext; }
        }

        private NameRegistrationContext NameRegistrationContext
        {
            get { return ((App)App.Current).NameRegistrationContext; }
            set { ((App)App.Current).NameRegistrationContext = value; }
        }
    }
}