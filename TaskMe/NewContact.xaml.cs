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
using System.Text;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Collections.ObjectModel;
using Microsoft.Hawaii.Rendezvous.Client;
using Microsoft.Phone.Shell;

namespace TaskMe
{
    public partial class NewContact : PhoneApplicationPage
    {
        IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
        int contactID = -1;
        string acceptNameString = "";
        string acceptIDString = "";
        string hID = "";

        public NewContact()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            string contactIDString = "";
            
            if (NavigationContext.QueryString.TryGetValue("Edit", out contactIDString)) //checks for a contact edit index
            {
                contactID = int.Parse(contactIDString);
                PageTitle.Text = "edit contact";
                DataContext = (Person)((ObservableCollection<Person>)settings["ContactsList"])[contactID];
                UsernameBox.IsEnabled = false;
            }
            else if (NavigationContext.QueryString.TryGetValue("Accept", out acceptNameString))
            {
                if (NavigationContext.QueryString.TryGetValue("ID", out acceptIDString))
                {
                    PageTitle.Text = "accept";
                    UsernameBox.Text = acceptNameString;
                    UsernameBox.IsEnabled = false;
                }
            }
        }

        private RelayContext RelayContext
        {
            get { return ((App)App.Current).RelayContext; }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // If no end point is created so far, return.
            if (this.RelayContext.Endpoint == null)
            {
                this.DisplayMessage("Please set up a username in order to add contacts.", "Error");
                return;
            }

            if (string.IsNullOrEmpty(NameBox.Text))
            {
                this.DisplayMessage("Please enter a name for this contact.", "Error");
                return;
            }

            // If no end point is created so far, return.
            string recipientId = this.UsernameBox.Text;
            if (string.IsNullOrEmpty(recipientId))
            {
                this.DisplayMessage("Please enter a username.", "Error");
                return;
            }

            ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
            ProgressBar.IsVisible = true;
            NameBox.IsEnabled = false;
            UsernameBox.IsEnabled = false;

            Person fromContacts = ((ObservableCollection<Person>)settings["ContactsList"]).Where(X => X.username == UsernameBox.Text).FirstOrDefault();
            Person fromRequests = ((ObservableCollection<Person>)settings["RequestList"]).Where(X => X.username == UsernameBox.Text).FirstOrDefault();

            if (contactID != -1 && fromContacts != null)
            {
                ((Person)((ObservableCollection<Person>)settings["ContactsList"])[contactID]).name = NameBox.Text;
                NavigationService.GoBack();
            }
            else if (!string.IsNullOrEmpty(acceptNameString) && !string.IsNullOrEmpty(acceptIDString) && fromRequests != null)
            {
                SendAccept(this.RelayContext.Endpoint, acceptIDString);
            }
            else
            {
                // Send the message.
                if (((string)settings["MyUsername"]).Equals(UsernameBox.Text))
                {
                    this.DisplayMessage("Can't request yourself as a contact.", "Error");
                    return;
                }
                else if (fromContacts != null)
                {
                    this.DisplayMessage("Contact already exists.", "Error");
                    return;
                }
                else if (fromRequests != null)
                {
                    this.DisplayMessage("You have already requested this person as a contact, or you have a pending request from them.", "Error");
                    return;
                }

                if (!string.IsNullOrEmpty(recipientId))
                {
                    RendezvousService.LookupNameAsync(HawaiiClient.HawaiiApplicationId, recipientId, this.OnCompleteLookupName);
                }
                else
                {
                    this.DisplayMessage("No username found. Enter a username and try again.", "Error");
                }
            }
        }

        private void OnCompleteLookupName(NameRegistrationResult result)
        {
            NameRegistration nameRegistration = (NameRegistration)result.NameRegistration;
            if (result.Status == Status.Success)
            {
                this.Dispatcher.BeginInvoke(
                   delegate
                   {
                       System.Diagnostics.Debug.WriteLine("Found the name!: " + nameRegistration.Id);
                       hID = nameRegistration.Id;
                       SendRequest(this.RelayContext.Endpoint, nameRegistration.Id);
                   });
            }
            else
            {
                this.Dispatcher.BeginInvoke(
                    delegate
                    {
                        ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = true;
                        ProgressBar.IsVisible = false;
                        NameBox.IsEnabled = true;
                        UsernameBox.IsEnabled = true;
                    });
                this.DisplayMessage("Name lookup failed. Check your wifi/cellular connection.", "Error");
            }
        }

        private void SendRequest(Endpoint from, string recipients)
        {

            byte[] message = Encoding.Unicode.GetBytes("FReq" + '\0' + (string)settings["MyUsername"]);

            RelayService.SendMessageAsync(
                HawaiiClient.HawaiiApplicationId,
                from,
                recipients,
                message,
                this.OnCompleteSendMessage);
        }

        private void OnCompleteSendMessage(MessagingResult result)
        {
            Debug.Assert(result != null, "result is null");

            if (result.Status == Status.Success)
            {
                //this.DisplayMessage("Contact request succeeded.", "Info");
                this.AddContact();
            }
            else
            {
                this.Dispatcher.BeginInvoke(
                    delegate
                    {
                        ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = true;
                        ProgressBar.IsVisible = false;
                        NameBox.IsEnabled = true;
                        UsernameBox.IsEnabled = true;
                    });
                this.DisplayMessage("Contact request failed. Check your wifi/cellular connection.", "Error");
            }
        }

        private void SendAccept(Endpoint from, string recipients)
        {
            byte[] message = Encoding.Unicode.GetBytes("Accept");
            RelayService.SendMessageAsync(
                HawaiiClient.HawaiiApplicationId,
                from,
                recipients,
                message,
                this.OnCompleteSendAccept);
        }

        private void OnCompleteSendAccept(MessagingResult result)
        {
            Debug.Assert(result != null, "result is null");

            if (result.Status == Status.Success)
            {
                //this.DisplayMessage("Contact request succeeded.", "Info");
                this.Dispatcher.BeginInvoke(
                    delegate
                    {
                        ((ObservableCollection<Person>)settings["ContactsList"]).Add(new Person { name = NameBox.Text, username = UsernameBox.Text, hawaiiID = acceptIDString, reqSent = true, accepted = true });
                        ((ObservableCollection<Person>)settings["ContactsList"]).RemoveAt(0);
                        settings["ContactsList"] = ContactSort(((ObservableCollection<Person>)settings["ContactsList"]));
                        ((ObservableCollection<Person>)settings["ContactsList"]).Insert(0,
                            new Person { name = "Me", username = (string)settings["MyUsername"], hawaiiID = this.RelayContext.Endpoint.RegistrationId, accepted = true, reqSent = true });

                        Person reqPerson = ((ObservableCollection<Person>)settings["RequestList"]).Where(X => X.hawaiiID == acceptIDString).FirstOrDefault();
                        if (reqPerson != null)
                        {
                            System.Diagnostics.Debug.WriteLine("Removing " + reqPerson.username + " from request list.");
                            ((ObservableCollection<Person>)settings["RequestList"]).Remove(reqPerson);
                        }
                        NavigationService.GoBack();
                    });
            }
            else
            {
                this.Dispatcher.BeginInvoke(
                    delegate
                    {
                        ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = true;
                        ProgressBar.IsVisible = false;
                        NameBox.IsEnabled = true;
                        UsernameBox.IsEnabled = true;
                    });
                this.DisplayMessage("Accept failed. Check your wifi/cellular connection.", "Error");
            }
        }

        private void DisplayMessage(string message, string caption)
        {
            this.Dispatcher.BeginInvoke(
                    delegate
                    {
                        MessageBox.Show(message, caption, MessageBoxButton.OK);
                    });
        }

        private void AddContact()
        {
            this.Dispatcher.BeginInvoke(
                delegate
                {
                    ((ObservableCollection<Person>)settings["RequestList"]).Add(new Person { name = NameBox.Text, username = UsernameBox.Text, hawaiiID = hID, reqSent = true, accepted = false });
                    settings["RequestList"] = ContactSort(((ObservableCollection<Person>)settings["RequestList"]));
                    NavigationService.GoBack();
                });
        }

        private ObservableCollection<Person> ContactSort(ObservableCollection<Person> person)
        {
            List<Person> sorter = new List<Person>(person);
            sorter.Sort(ContactCmp);
            ObservableCollection<Person> sorted = new ObservableCollection<Person>(sorter);
            return sorted;
        }

        private int ContactCmp(Person a, Person b)
        {
            if (a.name.Equals(b.name))
                return a.username.CompareTo(b.username);
            return a.name.CompareTo(b.name);
        }

        private NameRegistrationContext NameRegistrationContext
        {
            get { return ((App)App.Current).NameRegistrationContext; }
            set { ((App)App.Current).NameRegistrationContext = value; }
        }
    }
}