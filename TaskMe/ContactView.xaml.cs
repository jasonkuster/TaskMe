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
using System.IO.IsolatedStorage;
using System.Collections.ObjectModel;
using Microsoft.Hawaii;
using Microsoft.Hawaii.Relay.Client;
using System.Text;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;

namespace TaskMe
{
    public partial class ContactView : PhoneApplicationPage
    {
        IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

        string request = "";

        public ContactView()
        {
            InitializeComponent();
        }

        int selectedContact = -1;

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            string selectedContactString = "";
            if (NavigationContext.QueryString.TryGetValue("ContactKey", out selectedContactString))
            {
                selectedContact = int.Parse(selectedContactString);
                if (NavigationContext.QueryString.TryGetValue("Request", out request))
                {
                    if (int.Parse(request) == 1)
                    {
                        DataContext = null;
                        DataContext = (Person)((ObservableCollection<Person>)settings["RequestList"])[selectedContact];
                        ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
                        ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = false;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Setting non-request data context with index: " + selectedContact);

                    DataContext = null;
                    DataContext = (Person)((ObservableCollection<Person>)settings["ContactsList"])[selectedContact];

                    if (((Person)((ObservableCollection<Person>)settings["ContactsList"])[selectedContact]).hawaiiID == this.RelayContext.Endpoint.RegistrationId)
                    {

                        ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
                        ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = false;
                    }
                }
            }

            if (selectedContact == -1)
            {
                NavigationService.GoBack();
            }

        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/NewContact.xaml?Edit=" + selectedContact, UriKind.RelativeOrAbsolute));
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = true;
            ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = true;
            ProgressBar.IsVisible = false;
            byte[] message = Encoding.Unicode.GetBytes("ContactDelete" + '\0' + this.RelayContext.Endpoint.RegistrationId);
            RelayService.SendMessageAsync(
                HawaiiClient.HawaiiApplicationId,
                this.RelayContext.Endpoint,
                ((Person)((ObservableCollection<Person>)settings["ContactsList"])[selectedContact]).hawaiiID,
                message, this.OnCompleteSendMessage);
        }

        private void OnCompleteSendMessage(MessagingResult result) 
        {
            if (result.Status == Status.Success)
            {
                this.Dispatcher.BeginInvoke(
                    delegate
                    {
                        foreach (MyProject search in ((ObservableCollection<MyProject>)settings["ProjectList"]))
                        {
                            if (search.creator.hawaiiID.Equals(((Person)((ObservableCollection<Person>)settings["ContactsList"])[selectedContact]).hawaiiID))
                            {
                                foreach (MyTask searchTask in search.tasks)
                                {
                                    try
                                    {
                                        ScheduledActionService.Remove(searchTask.taskKey);
                                    }
                                    catch (InvalidOperationException) { }
                                }
                                ((ObservableCollection<MyProject>)settings["ProjectList"]).Remove(search);
                            }
                        }
                        Person remove = ((ObservableCollection<Person>)settings["ContactsList"]).Where(X => X.hawaiiID == ((Person)((ObservableCollection<Person>)settings["ContactsList"])[selectedContact]).hawaiiID).FirstOrDefault();
                        if (remove == null)
                            System.Diagnostics.Debug.WriteLine("Remove was 0.");
                        ((ObservableCollection<Person>)settings["ContactsList"]).Remove(remove);
                        NavigationService.GoBack();
                    });
            }
            else
            {
                this.Dispatcher.BeginInvoke(
                    delegate
                    {
                        ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = true;
                        ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = true;
                        ProgressBar.IsVisible = false;
                    });
            }
        }

        private RelayContext RelayContext
        {
            get { return ((App)App.Current).RelayContext; }
        }
    }
}