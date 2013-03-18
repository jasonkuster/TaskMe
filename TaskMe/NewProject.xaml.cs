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
using Microsoft.Hawaii.Relay.Client;
using Microsoft.Hawaii;
using Microsoft.Phone.Shell;

namespace TaskMe
{
    public partial class NewProject : PhoneApplicationPage
    {
        IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
        string selectedProjectString = "";
        int selectedProject = -1;
        bool loaded = false;

        public NewProject()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) //fires when navigated to
        {
            if (NavigationContext.QueryString.TryGetValue("Project", out selectedProjectString)) //gets index of project if page will be in edit mode
            {
                selectedProject = int.Parse(selectedProjectString); //saves index
                DataContext = ((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]; //sets data context
                PageTitle.Text = "edit project"; //sets page title
            }

            if (!loaded && selectedProject == -1) //If first load and we're creating a new project
            {
                DueCanvas.Visibility = System.Windows.Visibility.Collapsed;
                loaded = true;
            }
            else if (selectedProject != -1) //else if we're editing a project
            {
                if (((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject].dueDate != DateTime.MinValue)
                    DueSwitch.IsChecked = true;
                else
                    DueCanvas.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void SaveButton_Click(object sender, EventArgs e) //fires when save button is clicked
        {
            if (NameTextBox.Text == "") //can't save a project without a name.
            {
                MessageBox.Show("Please enter a name.");
                return;
            }

            if ((bool)DueSwitch.IsChecked)
            {
                if (DueTimePicker.Value == null) //if the due time hasn't been set
                {
                    MessageBox.Show("Please select a due time.");
                    return;
                }
                if (DueDatePicker.Value == null) //if the due date hasn't been set
                {
                    MessageBox.Show("Please select a due date.");
                    return;
                }
            }

            ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
            ProgressBar.IsVisible = true;
            NameTextBox.IsEnabled = false;
            DescTextBox.IsEnabled = false;
            DueSwitch.IsEnabled = false;
            DueDatePicker.IsEnabled = false;
            DueTimePicker.IsEnabled = false;

            if (selectedProject != -1) //if we're editing
            {
                if (this.RelayContext.Endpoint != null && this.RelayContext.Endpoint.RegistrationId == ((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject].creator.hawaiiID)
                    EditProject(1);
                else if (this.RelayContext.Endpoint == null)
                    EditProject(0);
                else
                {
                    MessageBox.Show("You are not the owner of this project.", "Error", MessageBoxButton.OK);
                    return;
                }
            }
            else //if we're creating
            {
                CreateProject();
            }
        }

        #region Group (Not Used)

        //private void CreateGroupProject(string groupID)
        //{
        //    MyProject newProject = new MyProject();
        //    newProject.name = NameTextBox.Text;
        //    newProject.projKey = System.Guid.NewGuid().ToString();
        //    newProject.description = DescTextBox.Text;
        //    if ((bool)DueSwitch.IsChecked) //sets a due date if the button is checked
        //    {
        //        var dueDate = (DateTime)DueDatePicker.Value;
        //        var dueTime = (DateTime)DueTimePicker.Value;
        //        newProject.dueDate = (new DateTime(dueDate.Year, dueDate.Month, dueDate.Day, dueTime.Hour, dueTime.Minute, dueTime.Second));
        //    }
        //    else
        //        newProject.dueDate = DateTime.MinValue;
        //    newProject.tasks = new ObservableCollection<MyTask>();
        //    newProject.isComplete = false;
        //    newProject.status = "Incomplete";
        //    foreach (Person p in (ObservableCollection<Person>)settings["ContactsList"])
        //    {
        //        if (p.hawaiiID == "" || p.hawaiiID == this.RelayContext.Endpoint.RegistrationId)
        //            newProject.creator = p;
        //    }
        //    newProject.recipients = groupID;
        //    ((ObservableCollection<MyProject>)settings["ProjectList"]).Add(newProject);
        //    NavigationService.GoBack();
        //}

        //private void OnCompleteCreateGroup(GroupResult result)
        //{
        //    if (result.Status == Status.Success)
        //    {
        //        // Add the new group into available group data object.
        //        this.RelayContext.Groups.Add(result.Group);

        //        // Refresh the list box to reflect the chages.
        //        Group group = this.RelayContext.Groups.Find(result.Group.RegistrationId);
        //        RelayService.JoinGroupAsync(
        //        HawaiiClient.HawaiiApplicationId,
        //        this.RelayContext.Endpoint,
        //        group,
        //        this.OnCompleteJoinGroup,
        //        group);
        //    }
        //    else
        //    {
        //        this.DisplayMessage("Creating a new group failed.", "Error");
        //    }
        //}

        //private void OnCompleteJoinGroup(GroupResult result)
        //{

        //    if (result.Status == Status.Success)
        //    {
        //        // Get the group object which we passed through DeleteGroupAsync method
        //        Group group = (Group)result.StateObject;

        //        this.RelayContext.Endpoint.Groups.Add(group);

        //        // Refresh the list box to reflect the change.
        //        this.Dispatcher.BeginInvoke(
        //            delegate
        //            {
        //                CreateGroupProject(group.RegistrationId);
        //            });
        //    }
        //    else
        //    {
        //        this.DisplayMessage("Joining group failed.", "Error");
        //    }
        //}

        #endregion

        private void CreateProject() //creates a new project
        {
            MyProject newProject = new MyProject();
            newProject.name = NameTextBox.Text;
            newProject.projKey = System.Guid.NewGuid().ToString();
            newProject.description = DescTextBox.Text;
            if ((bool)DueSwitch.IsChecked) //sets a due date if the button is checked
            {
                var dueDate = (DateTime)DueDatePicker.Value;
                var dueTime = (DateTime)DueTimePicker.Value;
                newProject.dueDate = (new DateTime(dueDate.Year, dueDate.Month, dueDate.Day, dueTime.Hour, dueTime.Minute, dueTime.Second));
            }
            else
                newProject.dueDate = DateTime.MinValue;
            newProject.tasks = new ObservableCollection<MyTask>();
            newProject.doneTasks = new ObservableCollection<MyTask>();
            newProject.isComplete = false;
            newProject.status = "Incomplete";
            if (this.RelayContext.Endpoint == null)
                newProject.creator = null;
            else
                newProject.creator = ((ObservableCollection<Person>)settings["ContactsList"])[0];
            newProject.recipients = "";
            ((ObservableCollection<MyProject>)settings["ProjectList"]).Add(newProject);
            settings["ProjectList"] = ProjectSort((ObservableCollection<MyProject>)settings["ProjectList"]);
            NavigationService.GoBack();
        }

        MyProject editProj = null;
        private void EditProject(int relay) //editing a project, 0 for non-relay, 1 for relay
        {
            if (relay == 1)
            {
                DateTime editDueDate;
                if ((bool)DueSwitch.IsChecked)
                {
                    var dueDate = (DateTime)DueDatePicker.Value;
                    var dueTime = (DateTime)DueTimePicker.Value;
                    editDueDate = (new DateTime(dueDate.Year, dueDate.Month, dueDate.Day, dueTime.Hour, dueTime.Minute, dueTime.Second));
                }
                else
                    editDueDate = DateTime.MinValue;

                editProj = new MyProject
                {
                    name = NameTextBox.Text,
                    description = DescTextBox.Text,
                    status = ((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject].status,
                    creator = ((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject].creator,
                    doneTasks = ((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject].doneTasks,
                    dueDate = editDueDate,
                    isComplete = ((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject].isComplete,
                    projKey = ((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject].projKey,
                    recipients = ((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject].recipients,
                    tasks = ((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject].tasks
                };

                string pKey = editProj.projKey;

                SendProject(this.RelayContext.Endpoint, ((ObservableCollection<MyProject>)settings["ProjectList"]).Where(X => X.projKey == pKey).FirstOrDefault().recipients); //ok to not send to creator because this is creator
            }
            else
            {
                ((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject].name = NameTextBox.Text;
                ((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject].description = DescTextBox.Text;
                if ((bool)DueSwitch.IsChecked) //changing due date
                {
                    var dueDate = (DateTime)DueDatePicker.Value;
                    var dueTime = (DateTime)DueTimePicker.Value;
                    ((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject].dueDate = (new DateTime(dueDate.Year, dueDate.Month, dueDate.Day, dueTime.Hour, dueTime.Minute, dueTime.Second));
                }
                else //else get rid of the due date
                    ((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject].dueDate = DateTime.MinValue;

                settings["ProjectList"] = ProjectSort((ObservableCollection<MyProject>)settings["ProjectList"]);

                NavigationService.GoBack();
            }
        }

        private void SendProject(Endpoint from, string recipients)
        {
            if (!String.IsNullOrEmpty(recipients))
            {
                byte[] message = editProj.Serialize();
                RelayService.SendMessageAsync(
                    HawaiiClient.HawaiiApplicationId,
                    from,
                    recipients,
                    message,
                    this.OnCompleteSendMessage);
            }
            else
            {
                MessagingResult ret = new MessagingResult();
                ret.Status = Status.Success;
                OnCompleteSendMessage(ret);
            }
        }

        private void OnCompleteSendMessage(MessagingResult result)
        {
            if (result.Status == Status.Success)
            {
                //this.DisplayMessage("Update succeeded.", "Info");
                this.Dispatcher.BeginInvoke(
                    delegate
                    {
                        ((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject].name = NameTextBox.Text;
                        ((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject].description = DescTextBox.Text;
                        if ((bool)DueSwitch.IsChecked) //changing due date
                        {
                            var dueDate = (DateTime)DueDatePicker.Value;
                            var dueTime = (DateTime)DueTimePicker.Value;
                            ((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject].dueDate = (new DateTime(dueDate.Year, dueDate.Month, dueDate.Day, dueTime.Hour, dueTime.Minute, dueTime.Second));
                        }
                        else //else get rid of the due date
                            ((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject].dueDate = DateTime.MinValue;

                        settings["ProjectList"] = ProjectSort((ObservableCollection<MyProject>)settings["ProjectList"]);

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
                        NameTextBox.IsEnabled = true;
                        DescTextBox.IsEnabled = true;
                        DueSwitch.IsEnabled = true;
                        DueDatePicker.IsEnabled = true;
                        DueTimePicker.IsEnabled = true;
                    });
                this.DisplayMessage("Project edit failed. Check your wifi/cellular connection.", "Error");
            }
        }

        private void DueSwitch_Checked(object sender, RoutedEventArgs e) //shows the duedate picker
        {
            DueCanvas.Visibility = System.Windows.Visibility.Visible;
        }

        private void DueSwitch_Unchecked(object sender, RoutedEventArgs e) //hides the duedate picker
        {
            DueCanvas.Visibility = System.Windows.Visibility.Collapsed;
        }

        private RelayContext RelayContext
        {
            get { return ((App)App.Current).RelayContext; }
        }

        private void DisplayMessage(string message, string caption)
        {
            this.Dispatcher.BeginInvoke(
                    delegate
                    {
                        MessageBox.Show(message, caption, MessageBoxButton.OK);
                    });
        }

        private ObservableCollection<MyProject> ProjectSort(ObservableCollection<MyProject> proj)
        {
            List<MyProject> sorter = new List<MyProject>(proj);
            sorter.Sort(ProjectCmp);
            ObservableCollection<MyProject> sorted = new ObservableCollection<MyProject>(sorter);
            return sorted;
        }

        private int ProjectCmp(MyProject a, MyProject b)
        {
            if (a.dueDate.Equals(b.dueDate))
                return a.name.CompareTo(b.name);
            return a.dueDate.CompareTo(b.dueDate);
        }
    }
}