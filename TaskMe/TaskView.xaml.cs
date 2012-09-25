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
using Microsoft.Phone.Scheduler;
using Microsoft.Hawaii;
using Microsoft.Hawaii.Relay.Client;
using System.Text;
using Microsoft.Phone.Shell;

namespace TaskMe
{
    public partial class TaskView : PhoneApplicationPage
    {
        IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
        string selectedKeyString = "";
        int selectedKey = -1;
        bool taskDone = false;
        MyTask currentTask = null;

        string selectedProjectString = "";
        int selectedProject = -1;
        MyProject currentProject = null;

        string completeString = "";
        int compdelete = -1;

        public TaskView()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) //called whenever the page is navigated to, sets default conditions for page
        {
            if (NavigationContext.QueryString.TryGetValue("TaskKey", out selectedKeyString))
            {
                selectedKey = int.Parse(selectedKeyString);
            }
            else if (NavigationContext.QueryString.TryGetValue("DoneTaskKey", out selectedKeyString))
            {
                taskDone = true;
                selectedKey = int.Parse(selectedKeyString);
            }

            if (NavigationContext.QueryString.TryGetValue("ProjectKey", out selectedProjectString))
            {
                selectedProject = int.Parse(selectedProjectString);
            }

            NavigationContext.QueryString.TryGetValue("Complete", out completeString);

            if (selectedKey == -1) //error checking for if no task is selected
            {
                MessageBox.Show("Error: No task selected.");
                NavigationService.GoBack();
                return;
            }

            if (selectedProject != -1) //If we're looking at a task with an associated project
            {
                LoadProjectTask();
            }
            else if (selectedProject == -1 && selectedKey != -1) //if we're looking at a standalone task
            {
                LoadSoloTask();
            }
        }

        private void LoadProjectTask() //sets conditions for loading a task from a project
        {
            if (string.IsNullOrEmpty(completeString))
            {
                currentProject = ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]);
                if (!taskDone)
                    currentTask = ((MyTask)((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey]);
                else
                    currentTask = ((MyTask)((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).doneTasks[selectedKey]);
            }
            else
            {
                currentProject = ((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]);
                if (!taskDone)
                    currentTask = ((MyTask)((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]).tasks[selectedKey]);
                else
                    currentTask = ((MyTask)((MyProject)((ObservableCollection<MyProject>)settings["DoneProjectList"])[selectedProject]).doneTasks[selectedKey]);
            }

            if (currentTask.isComplete)
            {
                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
                ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).Text = "reopen";
                ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IconUri = new Uri("/icons/appbar.upload.rest.png", UriKind.RelativeOrAbsolute);
                ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = false;
            }

            if (this.RelayContext.Endpoint != null && ((!(currentTask.assignedTo.hawaiiID == this.RelayContext.Endpoint.RegistrationId) && !(currentProject.creator.hawaiiID == this.RelayContext.Endpoint.RegistrationId))) || currentProject.isComplete)
            {
                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
                ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = false;
                ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = false;
            }

            if (currentTask.dueDate == DateTime.MinValue)
            {
                DuePanel.Visibility = System.Windows.Visibility.Collapsed; //if it doesn't have a duedate, hide the duedate field
            }
            else
                DuePanel.Visibility = System.Windows.Visibility.Visible;

            if (currentTask.remindDate == DateTime.MinValue)
            {
                RemindPanel.Visibility = System.Windows.Visibility.Collapsed; //if it doesn't have a reminddate, hide the reminddate field
            }
            else
                RemindPanel.Visibility = System.Windows.Visibility.Visible;

            if (currentTask.assignedTo == null)
            {
                
                AssignedPanel.Visibility = System.Windows.Visibility.Collapsed; //if it's not assigned to anyone, hide the assigned field
            }
            else
                AssignedPanel.Visibility = System.Windows.Visibility.Visible;

            DataContext = null;
            DataContext = currentTask; //set the datacontext for the page
        }

        private void LoadSoloTask() //sets conditions for loading a standalone task
        {
            ProjectPanel.Visibility = System.Windows.Visibility.Collapsed; //doesn't have a project, so don't show
            AssignedPanel.Visibility = System.Windows.Visibility.Collapsed; //isn't assigned to anybody, so don't show

            if (string.IsNullOrEmpty(completeString))
                currentTask = ((MyTask)((ObservableCollection<MyTask>)settings["TaskList"])[selectedKey]);
            else
                currentTask = ((MyTask)((ObservableCollection<MyTask>)settings["DoneTaskList"])[selectedKey]);

            if (currentTask.isComplete)
            {
                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
                ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).Text = "reopen";
                ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IconUri = new Uri("/icons/appbar.upload.rest.png", UriKind.RelativeOrAbsolute);
                ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = false;
            }

            if (currentTask.dueDate == DateTime.MinValue)
            {
                DuePanel.Visibility = System.Windows.Visibility.Collapsed;//if it doesn't have a duedate, hide the duedate field
            }
            else
                DuePanel.Visibility = System.Windows.Visibility.Visible;

            if (currentTask.remindDate == DateTime.MinValue)
            {
                RemindPanel.Visibility = System.Windows.Visibility.Collapsed;//if it doesn't have a reminddate, hide the reminddate field
            }
            else
                RemindPanel.Visibility = System.Windows.Visibility.Visible;
            DataContext = null;
            DataContext = currentTask; //set the datacontext for the page
        }

        private void DeleteButton_Click(object sender, EventArgs e) //delete the task
        {
            ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
            ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = false;
            ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = false;
            ProgressBar.IsVisible = true;

            if (selectedProject != -1) //if it has an associated project
            {
                if (!string.IsNullOrEmpty(currentProject.recipients))
                {
                    compdelete = 1;
                    SendCompDel(this.RelayContext.Endpoint, currentProject.creator.hawaiiID + "," + currentProject.recipients, compdelete);
                }
                else
                {
                    try
                    {
                        ScheduledActionService.Remove(((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey].taskKey); //remove reminder if it exists
                    }
                    catch (InvalidOperationException) { }

                    ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks.RemoveAt(selectedKey); //remove task
                    NavigationService.GoBack();
                }
            }
            else //if standalone
            {
                try
                {
                    ScheduledActionService.Remove(((ObservableCollection<MyTask>)settings["TaskList"])[selectedKey].taskKey); //remove reminder if it exists
                }
                catch (InvalidOperationException) { }
                ((ObservableCollection<MyTask>)settings["TaskList"]).RemoveAt(selectedKey); //remove task
                NavigationService.GoBack(); //prevents the user from hitting back and accessing a page that doesn't exist
            }
            
        }

        private void CompleteButton_Click(object sender, EventArgs e) //mark the task as complete
        {
            ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
            ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = false;
            ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = false;
            ProgressBar.IsVisible = true;

            if (currentTask.isComplete)
            {
                if (selectedProject != -1)
                {
                    if (!string.IsNullOrEmpty(currentProject.recipients))
                    {
                        compdelete = 2;
                        SendCompDel(this.RelayContext.Endpoint, currentProject.creator.hawaiiID + "," + currentProject.recipients, compdelete);
                    }
                    else
                    {
                        currentTask.isComplete = false;
                        currentTask.status = "Incomplete";
                        currentProject.tasks.Add(currentTask);
                        currentProject.tasks = TaskSort(currentProject.tasks);
                        currentProject.doneTasks.Remove(currentTask);
                        NavigationService.GoBack();
                    }
                }
                else
                {
                    currentTask.isComplete = false;
                    currentTask.status = "Incomplete";
                    ((ObservableCollection<MyTask>)settings["TaskList"]).Add(currentTask);
                    settings["TaskList"] = TaskSort((ObservableCollection<MyTask>)settings["TaskList"]);
                    ((ObservableCollection<MyTask>)settings["DoneTaskList"]).Remove(currentTask);
                    NavigationService.GoBack();
                }
            }
            else
            {
                if (selectedProject != -1) //if it has an associated project
                {
                    if (!string.IsNullOrEmpty(currentProject.recipients))
                    {
                        compdelete = 0;
                        SendCompDel(this.RelayContext.Endpoint, currentProject.creator.hawaiiID + "," + currentProject.recipients, compdelete);
                    }
                    else
                    {
                        try
                        {
                            ScheduledActionService.Remove(((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey].taskKey); //remove reminder if it exists
                        }
                        catch (InvalidOperationException) { }

                        ((MyTask)((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey]).isComplete = true; //mark as complete
                        ((MyTask)((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey]).status = "Complete"; //mark as complete
                        ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).doneTasks.Add(currentTask);
                        ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).doneTasks = TaskSort(
                            ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).doneTasks);
                        ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks.Remove(currentTask);
                        NavigationService.GoBack();
                    }
                }
                else
                {
                    try
                    {
                        ScheduledActionService.Remove(((ObservableCollection<MyTask>)settings["TaskList"])[selectedKey].taskKey); //remove reminder if it exists
                    }
                    catch (InvalidOperationException) { }
                    ((ObservableCollection<MyTask>)settings["TaskList"])[selectedKey].isComplete = true; //mark as complete
                    ((ObservableCollection<MyTask>)settings["TaskList"])[selectedKey].status = "Complete"; //mark as complete
                    ((ObservableCollection<MyTask>)settings["DoneTaskList"]).Add(currentTask);
                    settings["DoneTaskList"] = TaskSort((ObservableCollection<MyTask>)settings["DoneTaskList"]);
                    ((ObservableCollection<MyTask>)settings["TaskList"]).Remove(currentTask);
                    NavigationService.GoBack();
                }
            }
        }

        private void EditButton_Click(object sender, EventArgs e) //Navigates to the edit page
        {
            NavigationService.Navigate(new Uri("/NewTask.xaml?Edit=" + selectedKey + "&Project=" + selectedProject, UriKind.RelativeOrAbsolute));
        }

        private void SendCompDel(Endpoint from, string recipients, int compdel)
        {
            byte[] message = {};
            if (compdel == 0)
                message = Encoding.Unicode.GetBytes("TaskComplete" + '\0' + currentProject.projKey + '\0'
                     + currentTask.taskKey);
            else if (compdel == 1)
                message = Encoding.Unicode.GetBytes("TaskDelete" + '\0' + currentProject.projKey + '\0'
                    + currentTask.taskKey);
            else if (compdel == 2)
                message = Encoding.Unicode.GetBytes("TaskUndo" + '\0' + currentProject.projKey + '\0'
                    + currentTask.taskKey);
            RelayService.SendMessageAsync(
                HawaiiClient.HawaiiApplicationId,
                from,
                recipients,
                message,
                this.OnCompleteSendMessage);
        }

        private void OnCompleteSendMessage(MessagingResult result)
        {
            if (result.Status == Status.Success)
            {
                this.Dispatcher.BeginInvoke(
                    delegate
                    {
                        if (compdelete == 0)
                        {
                            try
                            {
                                ScheduledActionService.Remove(((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey].taskKey); //remove reminder if it exists
                            }
                            catch (InvalidOperationException) { }

                            ((MyTask)((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey]).isComplete = true; //mark as complete
                            ((MyTask)((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey]).status = "Complete"; //mark as complete
                            ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).doneTasks.Add(currentTask);
                            ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).doneTasks = TaskSort(
                                ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).doneTasks);
                            ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks.Remove(currentTask);
                        }
                        else if (compdelete == 1)
                        {
                            try
                            {
                                ScheduledActionService.Remove(((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey].taskKey); //remove reminder if it exists
                            }
                            catch (InvalidOperationException) { }

                            ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks.RemoveAt(selectedKey); //remove task
                        }
                        else if (compdelete == 2)
                        {
                            currentTask.isComplete = false;
                            currentTask.status = "Incomplete";

                            if (currentTask.remindDate > DateTime.Now)
                            {
                                try
                                {
                                    ScheduledActionService.Remove(currentTask.taskKey);
                                }
                                catch (InvalidOperationException) { }
                                Reminder reminder = new Reminder(currentTask.taskKey);
                                Uri navUri = new Uri("/TaskView.xaml?TaskKey=" + selectedKey + "&ProjectKey=" + selectedProject, UriKind.RelativeOrAbsolute);
                                reminder.Title = currentTask.name;
                                reminder.Content = currentTask.description;
                                reminder.BeginTime = currentTask.remindDate;
                                reminder.ExpirationTime = currentTask.remindDate;
                                reminder.NavigationUri = navUri;
                                ScheduledActionService.Add(reminder);
                            }
                            else
                            {
                                currentTask.remindDate = DateTime.MinValue;
                            }

                            currentProject.tasks.Add(currentTask);
                            currentProject.tasks = TaskSort(currentProject.tasks);
                            currentProject.doneTasks.Remove(currentTask);
                        }
                        NavigationService.GoBack();
                        //this.DisplayMessage("Accept succeeded.", "Info");
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

        private ObservableCollection<MyTask> TaskSort(ObservableCollection<MyTask> task)
        {
            List<MyTask> sorter = new List<MyTask>(task);
            sorter.Sort(TaskCmp);
            ObservableCollection<MyTask> sorted = new ObservableCollection<MyTask>(sorter);
            return sorted;
        }

        private int TaskCmp(MyTask a, MyTask b)
        {
            if (a.dueDate.Equals(b.dueDate))
                return a.name.CompareTo(b.name);
            return a.dueDate.CompareTo(b.dueDate);
        }
    }
}