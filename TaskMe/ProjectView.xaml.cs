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
using Microsoft.Phone.Shell;
using System.Text;
using Microsoft.Hawaii;
using Microsoft.Hawaii.Relay.Client;
using Microsoft.Phone.Scheduler;

namespace TaskMe
{
    public partial class ProjectView : PhoneApplicationPage
    {
        public ProjectView()
        {
            InitializeComponent();
        }

        string selectedProjectString = "";
        int selectedProject = -1;
        MyProject selectedProj = null;

        string completeString = "";

        IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (NavigationContext.QueryString.TryGetValue("Project", out selectedProjectString)) //set the datacontext and selected project
            {
                selectedProject = int.Parse(selectedProjectString);
                if (NavigationContext.QueryString.TryGetValue("Complete", out completeString))
                {
                    DataContext = ((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]);
                    selectedProj = ((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]);
                }
                else
                {
                    DataContext = ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]);
                    selectedProj = ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]);
                }
            }

            if (selectedProject == -1) //error checking. If no project was passed in, go back and don't display an empty project
            {
                MessageBox.Show("Error: No project selected.");
                NavigationService.GoBack();
            }

            if (string.IsNullOrEmpty(completeString)) //refresh the datacontext
            {
                DataContext = null;
                DataContext = ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]);

                if (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).dueDate == DateTime.MinValue) //If no duedate, hide duedate section
                {
                    DuePanel.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                    DuePanel.Visibility = System.Windows.Visibility.Visible;

                TaskListBox.ItemsSource = null;
                TaskListBox.ItemsSource = (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks); //refreshes and sets items source for tasklistbox

                DoneBox.ItemsSource = null;
                DoneBox.ItemsSource = (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).doneTasks);

                if (!(this.RelayContext.Endpoint == null || this.RelayContext.Endpoint.RegistrationId == ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).creator.hawaiiID))
                {
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = false;
                }
            }
            else if (DataContext == ((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]))
            {
                DataContext = null;
                DataContext = ((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]);

                if (((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]).dueDate == DateTime.MinValue) //If no duedate, hide duedate section
                {
                    DuePanel.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                    DuePanel.Visibility = System.Windows.Visibility.Visible;

                TaskListBox.ItemsSource = null;
                TaskListBox.ItemsSource = (((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]).tasks); //refreshes and sets items source for tasklistbox

                DoneBox.ItemsSource = null;
                DoneBox.ItemsSource = (((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]).doneTasks);
                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
                ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).Text = "reopen";
                ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IconUri = new Uri("/icons/appbar.upload.rest.png", UriKind.RelativeOrAbsolute);
                ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = false;
            }
        }

        private void EditAddButton_Click(object sender, EventArgs e) //Button behaves differently based on which panorama item is selected
        {
            if (((PanoramaItem)ProjectPanorama.SelectedItem).Name == "DetailItem") //if on the project detail screen, edits project
            {
                if (this.RelayContext.Endpoint == null || 
                    this.RelayContext.Endpoint.RegistrationId == ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).creator.hawaiiID)
                    NavigationService.Navigate(new Uri("/NewProject.xaml?Project=" + selectedProject, UriKind.RelativeOrAbsolute));
                else
                    MessageBox.Show("You do not have permission to do that.", "Error", MessageBoxButton.OK);
            }
            else if (((PanoramaItem)ProjectPanorama.SelectedItem).Name == "TaskItem") //if on the tasks list, adds a new task
            {
                NavigationService.Navigate(new Uri("/NewTask.xaml?Project=" + selectedProject, UriKind.RelativeOrAbsolute));
            }
        }

        private void CompleteButton_Click(object sender, EventArgs e) //marks the project as complete.
        {
            ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
            ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = false;
            ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = false;
            ProgressBar.IsVisible = true;
            if (this.RelayContext.Endpoint == null || this.RelayContext.Endpoint.RegistrationId == selectedProj.creator.hawaiiID)
            {
                if (string.IsNullOrEmpty(completeString))
                {
                    System.Diagnostics.Debug.WriteLine("Completing");
                    if (!string.IsNullOrEmpty(((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).recipients))
                    {
                        byte[] message = Encoding.Unicode.GetBytes("ProjectComplete" + '\0' + ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).projKey);
                        RelayService.SendMessageAsync(HawaiiClient.HawaiiApplicationId, this.RelayContext.Endpoint, ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).recipients
                            , message, this.OnCompleteSendComplete); //OK to send to just recipients because we are owner
                    }
                    else
                    {
                        ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).isComplete = true;
                        ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).status = "Complete";
                        foreach (MyTask searchTask in ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks)
                        {
                            try
                            {
                                ScheduledActionService.Remove(searchTask.taskKey);
                            }
                            catch (InvalidOperationException) { }
                        }
                        ((ObservableCollection<MyProject>)settings["DoneProjectList"]).Add(((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]));
                        settings["DoneProjectList"] = ProjectSort(((ObservableCollection<MyProject>)settings["DoneProjectList"]));
                        ((ObservableCollection<MyProject>)settings["ProjectList"]).Remove(((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]));
                        NavigationService.GoBack();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Reopening");
                    if (!string.IsNullOrEmpty(((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]).recipients))
                    {
                        byte[] message = Encoding.Unicode.GetBytes("ProjectUndo" + '\0' + ((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]).projKey);
                        RelayService.SendMessageAsync(HawaiiClient.HawaiiApplicationId, this.RelayContext.Endpoint, ((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]).recipients
                            , message, this.OnCompleteSendIncomplete); //OK to send to just recipients because we are owner
                    }
                    else
                    {
                        ((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]).isComplete = false;
                        ((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]).status = "Incomplete";
                        foreach (MyTask searchTask in ((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]).tasks)
                        {
                            if (searchTask.remindDate != DateTime.MinValue && searchTask.remindDate > DateTime.Now)
                            {
                                try
                                {
                                    ScheduledActionService.Remove(searchTask.taskKey);
                                }
                                catch (InvalidOperationException) { }
                                Reminder reminder = new Reminder(searchTask.taskKey);
                                Uri navUri = new Uri("/TaskView.xaml?TaskKey=" + searchTask.taskKey + "&ProjectKey=" + 
                                    ((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]).projKey, UriKind.RelativeOrAbsolute);
                                reminder.Title = searchTask.name;
                                reminder.Content = searchTask.description;
                                reminder.BeginTime = searchTask.remindDate;
                                reminder.ExpirationTime = searchTask.remindDate;
                                reminder.NavigationUri = navUri;
                                ScheduledActionService.Add(reminder);
                            }
                            else if (searchTask.remindDate < DateTime.Now)
                                searchTask.remindDate = DateTime.MinValue;
                        }
                        ((ObservableCollection<MyProject>)settings["ProjectList"]).Add(((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]));
                        settings["ProjectList"] = ProjectSort(((ObservableCollection<MyProject>)settings["ProjectList"]));
                        ((ObservableCollection<MyProject>)settings["DoneProjectList"]).Remove(((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]));
                        NavigationService.GoBack();
                    }
                }
            }
            else
                MessageBox.Show("You do not have permission to do that.", "Error", MessageBoxButton.OK);
        }

        private void OnCompleteSendComplete(MessagingResult result)
        {
            if (result.Status == Status.Success)
            {
                this.Dispatcher.BeginInvoke(
                    delegate
                    {
                        ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).isComplete = true;
                        ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).status = "Complete";
                        foreach (MyTask searchTask in ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks)
                        {
                            try
                            {
                                ScheduledActionService.Remove(searchTask.taskKey);
                            }
                            catch (InvalidOperationException) { }
                        }
                        ((ObservableCollection<MyProject>)settings["DoneProjectList"]).Add(((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]));
                        settings["DoneProjectList"] = ProjectSort(((ObservableCollection<MyProject>)settings["DoneProjectList"]));
                        ((ObservableCollection<MyProject>)settings["ProjectList"]).Remove(((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]));
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
                        ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = true;
                        ProgressBar.IsVisible = false;
                    });
                this.DisplayMessage("Operation failed. Check your wifi/cellular connection.", "Error");
            }
        }

        private void OnCompleteSendIncomplete(MessagingResult result)
        {
            if (result.Status == Status.Success)
            {
                this.Dispatcher.BeginInvoke(
                    delegate
                    {
                        ((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]).isComplete = false;
                        ((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]).status = "Incomplete";
                        foreach (MyTask searchTask in ((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]).tasks)
                        {
                            if (searchTask.remindDate != DateTime.MinValue && searchTask.remindDate > DateTime.Now)
                            {
                                try
                                {
                                    ScheduledActionService.Remove(searchTask.taskKey);
                                }
                                catch (InvalidOperationException) { }
                                Reminder reminder = new Reminder(searchTask.taskKey);
                                Uri navUri = new Uri("/TaskView.xaml?TaskKey=" + searchTask.taskKey + "&ProjectKey=" + 
                                    ((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]).projKey, UriKind.RelativeOrAbsolute);
                                reminder.Title = searchTask.name;
                                reminder.Content = searchTask.description;
                                reminder.BeginTime = searchTask.remindDate;
                                reminder.ExpirationTime = searchTask.remindDate;
                                reminder.NavigationUri = navUri;
                                ScheduledActionService.Add(reminder);
                            }
                            else if (searchTask.remindDate < DateTime.Now)
                                searchTask.remindDate = DateTime.MinValue;
                        }
                        ((ObservableCollection<MyProject>)settings["ProjectList"]).Add(((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]));
                        settings["ProjectList"] = ProjectSort(((ObservableCollection<MyProject>)settings["ProjectList"]));
                        ((ObservableCollection<MyProject>)settings["DoneProjectList"]).Remove(((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]));
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
                        ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = true;
                        ProgressBar.IsVisible = false;
                    });
                this.DisplayMessage("Operation failed. Check your wifi/cellular connection.", "Error");
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e) //deletes the project
        {
            ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
            ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = false;
            ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = false;
            ProgressBar.IsVisible = true;

            if (!string.IsNullOrEmpty(((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).recipients)
                && this.RelayContext.Endpoint.RegistrationId != ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).creator.hawaiiID)
            {
                byte[] message = Encoding.Unicode.GetBytes("ProjectDelete" + '\0' + ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).projKey);
                RelayService.SendMessageAsync(HawaiiClient.HawaiiApplicationId,
                    this.RelayContext.Endpoint,
                    ((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject].creator.hawaiiID + ","
                    + ((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject].recipients,
                    message,
                    this.OnCompleteSendDelete);
            }
            else if (!string.IsNullOrEmpty(((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).recipients)
                && this.RelayContext.Endpoint.RegistrationId == ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).creator.hawaiiID)
            {
                byte[] message = Encoding.Unicode.GetBytes("ProjectDelete" + '\0' + ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).projKey);
                RelayService.SendMessageAsync(HawaiiClient.HawaiiApplicationId,
                    this.RelayContext.Endpoint,
                    ((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject].recipients,
                    message,
                    this.OnCompleteSendDelete);
            }
            else if (string.IsNullOrEmpty(((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).recipients))
            {
                ((ObservableCollection<MyProject>)settings["ProjectList"]).RemoveAt(selectedProject);
                NavigationService.GoBack();
            }
        }

        private void OnCompleteSendDelete(MessagingResult result)
        {
            if (result.Status == Status.Success)
            {
                this.Dispatcher.BeginInvoke(
                    delegate
                    {
                        foreach (MyTask searchTask in ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks)
                        {
                            try
                            {
                                ScheduledActionService.Remove(searchTask.taskKey);
                            }
                            catch (InvalidOperationException) { }
                        }
                        ((ObservableCollection<MyProject>)settings["ProjectList"]).RemoveAt(selectedProject);
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
                        ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = true;
                        ProgressBar.IsVisible = false;
                    });
                this.DisplayMessage("Operation failed. Check your wifi/cellular connection.", "Error");
            }
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

        //private void RefreshButton_Click(object sender, EventArgs e) //pulls new info from Hawaii
        //{

        //}

        private void ProjectPanorama_SelectionChanged(object sender, SelectionChangedEventArgs e) //changes the app buttons
        {
            ApplicationBarIconButton btn = (ApplicationBarIconButton)ApplicationBar.Buttons[0];

            if (selectedProj.isComplete)
            {
                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
                ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = false;
                return;
            }

            if (((PanoramaItem)ProjectPanorama.SelectedItem).Name == "DetailItem") //edit project button
            {
                btn.Text = "edit";
                btn.IconUri = new Uri("/icons/appbar.edit.rest.png", UriKind.RelativeOrAbsolute);
                if (this.RelayContext.Endpoint == null || this.RelayContext.Endpoint.RegistrationId == selectedProj.creator.hawaiiID)
                {
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = true;
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = true;
                }
                else
                {
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = false;
                }
                if (string.IsNullOrEmpty(completeString))
                {
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = true;
                }
            }
            else if (((PanoramaItem)ProjectPanorama.SelectedItem).Name == "TaskItem") //new task button
            {
                btn.Text = "new";
                btn.IconUri = new Uri("/icons/appbar.add.rest.png", UriKind.RelativeOrAbsolute);
                if (this.RelayContext.Endpoint == null || this.RelayContext.Endpoint.RegistrationId == selectedProj.creator.hawaiiID)
                {
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = true;
                }
                else
                {
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
                }
                ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = false;
                ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = false;
            }
            else if (((PanoramaItem)ProjectPanorama.SelectedItem).Name == "DoneItems")
            {
                btn.Text = "new";
                btn.IconUri = new Uri("/icons/appbar.add.rest.png", UriKind.RelativeOrAbsolute);
                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
                ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = false;
                ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = false;
            }
        }

        MyTask _selectedTask = null;

        private void TaskListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) //navigates to taskview screen from a selected task
        {
            if (TaskListBox.SelectedIndex == -1)
                return;
            _selectedTask = (MyTask)TaskListBox.SelectedItem;
            System.Diagnostics.Debug.WriteLine(_selectedTask.taskKey);
            if (string.IsNullOrEmpty(completeString))
                NavigationService.Navigate(new Uri("/TaskView.xaml?TaskKey=" + ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks.IndexOf(_selectedTask)
                    + "&ProjectKey=" + selectedProject, UriKind.RelativeOrAbsolute));
            else
                NavigationService.Navigate(new Uri("/TaskView.xaml?TaskKey=" + ((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]).tasks.IndexOf(_selectedTask)
                    + "&ProjectKey=" + selectedProject + "&Complete=1", UriKind.RelativeOrAbsolute));
            TaskListBox.SelectedIndex = -1;
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

        private void DoneBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DoneBox.SelectedIndex == -1)
                return;
            _selectedTask = (MyTask)DoneBox.SelectedItem;
            System.Diagnostics.Debug.WriteLine(_selectedTask.taskKey);
            if (string.IsNullOrEmpty(completeString))
                NavigationService.Navigate(new Uri("/TaskView.xaml?DoneTaskKey=" + ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).doneTasks.IndexOf(_selectedTask)
                    + "&ProjectKey=" + selectedProject, UriKind.RelativeOrAbsolute));
            else
                NavigationService.Navigate(new Uri("/TaskView.xaml?DoneTaskKey=" + ((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]).doneTasks.IndexOf(_selectedTask)
                    + "&ProjectKey=" + selectedProject + "&Complete=1", UriKind.RelativeOrAbsolute));
            TaskListBox.SelectedIndex = -1;
        }
    }
}