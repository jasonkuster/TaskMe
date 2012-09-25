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
using Microsoft.Hawaii.Relay.Client;
using Microsoft.Hawaii;
using Microsoft.Phone.Shell;

namespace TaskMe
{
    public partial class NewTask : PhoneApplicationPage
    {
        IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
        string selectedKeyString = "";
        int selectedKey = -1;
        MyTask currentTask = null;
        string selectedProjectString = "";
        int selectedProject = -1;
        MyProject currentProject = null;
        bool loaded = false;
        
        public NewTask()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) //fires whenever page is navigated to
        {
            if (NavigationContext.QueryString.TryGetValue("Edit", out selectedKeyString)) //checks for a task edit index
            {
                selectedKey = int.Parse(selectedKeyString);
                PageTitle.Text = "edit task";
            }
            if (NavigationContext.QueryString.TryGetValue("Project", out selectedProjectString)) //checks for a project index
            {
                selectedProject = int.Parse(selectedProjectString);
            }
            if (selectedKey == -1 && selectedProject == -1 && !loaded) //first load, and we're creating a new task with no project
            {
                DueCanvas.Visibility = System.Windows.Visibility.Collapsed;
                RemindCanvas.Visibility = System.Windows.Visibility.Collapsed;
                AssignPicker.Visibility = System.Windows.Visibility.Collapsed;
                AssignToSwitch.Visibility = System.Windows.Visibility.Collapsed;
                TaskPanel.Height = 404;
                loaded = true;
            }
            else if (selectedKey != -1 && selectedProject == -1) //editing a standalone task
            {
                currentTask = ((MyTask)((ObservableCollection<MyTask>)settings["TaskList"])[selectedKey]);
                DataContext = currentTask; //set the data context

                if (currentTask.dueDate == DateTime.MinValue) //hide or show the due section
                {
                    DueCanvas.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    DueSwitch.IsChecked = true;
                    TaskPanel.Height += 100;
                }

                if (currentTask.remindDate == DateTime.MinValue) //hide or show the remind section
                {
                    RemindCanvas.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    RemindSwitch.IsChecked = true;
                    TaskPanel.Height += 100;
                }
                AssignToSwitch.Visibility = System.Windows.Visibility.Collapsed;
                AssignPicker.Visibility = System.Windows.Visibility.Collapsed;
            }
            else if (selectedKey == -1 && selectedProject != -1 && !loaded) //create a new task with an associated project
            {
                DueCanvas.Visibility = System.Windows.Visibility.Collapsed;
                RemindCanvas.Visibility = System.Windows.Visibility.Collapsed;
                if (((ObservableCollection<Person>)settings["ContactsList"]).Count != 0)
                {
                    AssignToSwitch.Visibility = System.Windows.Visibility.Visible;
                    TaskPanel.Height += 111;
                    AssignPicker.Visibility = System.Windows.Visibility.Collapsed;
                    AssignPicker.ItemsSource = ((ObservableCollection<Person>)settings["ContactsList"]);
                }
                else
                {
                    AssignToSwitch.Visibility = System.Windows.Visibility.Collapsed;
                    AssignPicker.Visibility = System.Windows.Visibility.Collapsed;
                }
                loaded = true;
            }
            else if (selectedKey != -1 && selectedProject != -1) //editing a task with an associated project
            {
                currentProject = ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]);
                DataContext = currentProject.tasks[selectedKey]; //set datacontext
                if (currentProject.tasks[selectedKey].dueDate == DateTime.MinValue) //hide or show due picker
                {
                    DueCanvas.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    DueSwitch.IsChecked = true;
                    TaskPanel.Height += 100;
                }

                if (currentProject.tasks[selectedKey].remindDate == DateTime.MinValue) //hide or show remind picker
                {
                    RemindCanvas.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    RemindSwitch.IsChecked = true;
                    TaskPanel.Height += 100;
                }

                AssignToSwitch.Visibility = System.Windows.Visibility.Collapsed;
                AssignPicker.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void Save_Click(object sender, EventArgs e) //save button clicked
        {
            if (NameTextBox.Text == "") //project must have a name
            {
                MessageBox.Show("please enter a task name");
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

            if ((bool)RemindSwitch.IsChecked) //Checks if the remind date is allowed
            {
                if (ReminderTimePicker.Value == null) //if the reminder time hasn't been set
                {
                    MessageBox.Show("Please select a reminder time.");
                    return; //exit
                }
                if (ReminderDatePicker.Value == null) //if the reminder date hasn't been set
                {
                    MessageBox.Show("Please select a reminder date.");
                    return; //exit
                }

                var remindDate = (DateTime)ReminderDatePicker.Value;
                var remindTime = (DateTime)ReminderTimePicker.Value;
                var remindDateTime = new DateTime(remindDate.Year,remindDate.Month,remindDate.Day,remindTime.Hour,remindTime.Minute,remindTime.Second);
                System.Diagnostics.Debug.WriteLine(remindDateTime.ToString());
                if (remindDateTime < DateTime.Now)
                {
                    MessageBox.Show("the remind time must be in the future");
                    return;
                }
            }

            if ((bool)AssignToSwitch.IsChecked)
            {
                if (AssignPicker.SelectedItem == null)
                {
                    MessageBox.Show("Please select a person to whom to assign the project.");
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
            RemindSwitch.IsEnabled = false;
            ReminderDatePicker.IsEnabled = false;
            ReminderTimePicker.IsEnabled = false;
            AssignToSwitch.IsEnabled = false;
            AssignPicker.IsEnabled = false;

            if (selectedKey != -1 && selectedProject == -1) //Editing a standalone task
                EditTask();
            else if (selectedKey == -1 && selectedProject != -1) //creating a new project-associated task
                ProjectTask();
            else if (selectedKey != -1 && selectedProject != -1) //editing a project-associated task
                EditProjectTask();
            else //creating a new standalone task
                SaveNewTask();
        }

        private void SaveNewTask() //standalone task
        {
            MyTask newTask = new MyTask();
            newTask.name = NameTextBox.Text;
            newTask.description = DescTextBox.Text;
            String taskKey = System.Guid.NewGuid().ToString(); //sets key to guid for ease of reminder creation
            newTask.taskKey = taskKey;

            if ((bool)DueSwitch.IsChecked) //sets due date
            {
                var dueDate = (DateTime)DueDatePicker.Value;
                var dueTime = (DateTime)DueTimePicker.Value;
                newTask.dueDate = new DateTime(dueDate.Year, dueDate.Month, dueDate.Day, dueDate.Hour, dueDate.Minute, dueDate.Second);
            }
            else
                newTask.dueDate = DateTime.MinValue;

            if ((bool)RemindSwitch.IsChecked) //sets remind date
            {
                var remindDate = (DateTime)ReminderDatePicker.Value;
                var remindTime = (DateTime)ReminderTimePicker.Value;
                newTask.remindDate = new DateTime(remindDate.Year, remindDate.Month, remindDate.Day, remindTime.Hour, remindTime.Minute, remindTime.Second);
            }
            else
                newTask.remindDate = DateTime.MinValue;

            newTask.isComplete = false;
            newTask.status = "Incomplete";
            newTask.projKey = "";
            newTask.project = "";
            ((ObservableCollection<MyTask>)settings["TaskList"]).Add(newTask);

            settings["TaskList"] = TaskSort(((ObservableCollection<MyTask>)settings["TaskList"]));

            if ((bool)RemindSwitch.IsChecked) //creates reminder in the system. Has to go after adding the new task because we need to set a navigation uri.
            {
                Reminder reminder = new Reminder(taskKey);
                Uri navUri = new Uri("/TaskView.xaml?TaskKey=" + ((ObservableCollection<MyTask>)settings["TaskList"]).IndexOf(newTask), UriKind.RelativeOrAbsolute);
                reminder.Title = newTask.name;
                reminder.Content = newTask.description;
                reminder.BeginTime = newTask.remindDate;
                reminder.ExpirationTime = newTask.remindDate;
                reminder.NavigationUri = navUri;
                ScheduledActionService.Add(reminder);
            }
            NavigationService.GoBack();
        }

        private void EditTask() //edit standalone task
        {
            ((ObservableCollection<MyTask>)settings["TaskList"])[selectedKey].name = NameTextBox.Text;
            ((ObservableCollection<MyTask>)settings["TaskList"])[selectedKey].description = DescTextBox.Text;

            if ((bool)DueSwitch.IsChecked) //change duedate
            {
                var dueDate = (DateTime)DueDatePicker.Value;
                var dueTime = (DateTime)DueTimePicker.Value;
                ((ObservableCollection<MyTask>)settings["TaskList"])[selectedKey].dueDate = new DateTime(dueDate.Year, dueDate.Month, dueDate.Day, dueDate.Hour, dueDate.Minute, dueDate.Second);
            }
            else
                ((ObservableCollection<MyTask>)settings["TaskList"])[selectedKey].dueDate = DateTime.MinValue;

            if ((bool)RemindSwitch.IsChecked) //change remind date and set reminder in system
            {
                var remindDate = (DateTime)ReminderDatePicker.Value;
                var remindTime = (DateTime)ReminderTimePicker.Value;
                ((ObservableCollection<MyTask>)settings["TaskList"])[selectedKey].remindDate = new DateTime(remindDate.Year, remindDate.Month, remindDate.Day, remindTime.Hour, remindTime.Minute, remindTime.Second);

                try
                {
                    ScheduledActionService.Remove(((ObservableCollection<MyTask>)settings["TaskList"])[selectedKey].taskKey); //removes the current reminder
                }
                catch (InvalidOperationException) { }
                Reminder reminder = new Reminder(((ObservableCollection<MyTask>)settings["TaskList"])[selectedKey].taskKey); //creates a new reminder
                Uri navUri = new Uri("/TaskView.xaml?TaskKey=" + selectedKey, UriKind.RelativeOrAbsolute);
                reminder.Title = ((ObservableCollection<MyTask>)settings["TaskList"])[selectedKey].name;
                reminder.Content = ((ObservableCollection<MyTask>)settings["TaskList"])[selectedKey].description;
                reminder.BeginTime = ((ObservableCollection<MyTask>)settings["TaskList"])[selectedKey].remindDate;
                reminder.ExpirationTime = ((ObservableCollection<MyTask>)settings["TaskList"])[selectedKey].remindDate;
                reminder.NavigationUri = navUri;
                ScheduledActionService.Add(reminder); //adds to the system
            }
            else
                ((ObservableCollection<MyTask>)settings["TaskList"])[selectedKey].remindDate = DateTime.MinValue;

            settings["TaskList"] = TaskSort(((ObservableCollection<MyTask>)settings["TaskList"]));

            NavigationService.GoBack();
        }

        MyTask sendTask = null;
        private void ProjectTask() //adds a new project-associated task
        {
            MyTask newTask = new MyTask();
            newTask.name = NameTextBox.Text;
            newTask.description = DescTextBox.Text;
            String taskKey = System.Guid.NewGuid().ToString();
            newTask.taskKey = taskKey;

            if ((bool)DueSwitch.IsChecked) //sets due date
            {
                var dueDate = (DateTime)DueDatePicker.Value;
                var dueTime = (DateTime)DueTimePicker.Value;
                newTask.dueDate = new DateTime(dueDate.Year, dueDate.Month, dueDate.Day, dueDate.Hour, dueDate.Minute, dueDate.Second);
            }
            else
                newTask.dueDate = DateTime.MinValue;

            if ((bool)RemindSwitch.IsChecked) //sets remind date
            {
                var remindDate = (DateTime)ReminderDatePicker.Value;
                var remindTime = (DateTime)ReminderTimePicker.Value;
                newTask.remindDate = new DateTime(remindDate.Year, remindDate.Month, remindDate.Day, remindTime.Hour, remindTime.Minute, remindTime.Second);
            }
            else
                newTask.remindDate = DateTime.MinValue;

            newTask.isComplete = false;
            newTask.status = "Incomplete";
            if ((bool)AssignToSwitch.IsChecked)
            {
                newTask.assignedTo = (Person)AssignPicker.SelectedItem;
                if (((Person)AssignPicker.SelectedItem).hawaiiID != this.RelayContext.Endpoint.RegistrationId)
                    (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject])).addRecipient(((Person)AssignPicker.SelectedItem).hawaiiID);
            }
            else
                newTask.assignedTo = ((ObservableCollection<Person>)settings["ContactsList"])[0];

            newTask.projKey = ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).projKey;
            newTask.project = ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).name;
            

            if ((bool)RemindSwitch.IsChecked && newTask.assignedTo.hawaiiID == this.RelayContext.Endpoint.RegistrationId) //adds reminder in system
            {
                Reminder reminder = new Reminder(taskKey);
                Uri navUri = new Uri("/TaskView.xaml?TaskKey=" + ((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject].tasks.IndexOf(newTask) + "&ProjectKey=" + selectedProject, UriKind.RelativeOrAbsolute);
                reminder.Title = newTask.name;
                reminder.Content = newTask.description;
                reminder.BeginTime = newTask.remindDate;
                reminder.ExpirationTime = newTask.remindDate;
                reminder.NavigationUri = navUri;
                ScheduledActionService.Add(reminder);
            }

            MyProject testProj = ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]); //possibly implement this for all
            if (string.IsNullOrEmpty(testProj.recipients))
            {
                (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks).Add(newTask);
                ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks = TaskSort(
                    ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks);
                NavigationService.GoBack();
            }
            else
            {
                byte[] message = ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).Serialize();
                //System.Diagnostics.Debug.WriteLine(((Person)AssignPicker.SelectedItem).hawaiiID + " " + ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).recipients);

                sendTask = newTask;

                RelayService.SendMessageAsync(
                    HawaiiClient.HawaiiApplicationId,
                    this.RelayContext.Endpoint,
                    (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject])).creator.hawaiiID + ","
                        + (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject])).recipients,
                    message, this.OnCompleteSendProject);
            }
        }

        private void OnCompleteSendProject(MessagingResult result)
        {
            if (result.Status == Status.Success)
            {
                this.Dispatcher.BeginInvoke(
                    delegate
                    {
                        (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks).Add(sendTask);
                        ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks = TaskSort(
                            ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks);
                        foreach (MyTask send in (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject])).tasks)
                        {
                            byte[] message = send.Serialize();
                            RelayService.SendMessageAsync(
                                HawaiiClient.HawaiiApplicationId,
                                this.RelayContext.Endpoint,
                                (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject])).creator.hawaiiID + ","
                                + (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject])).recipients,
                                message,
                                this.OnCompleteSendTask);
                        }
                        foreach (MyTask send in (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject])).doneTasks)
                        {
                            byte[] message = send.Serialize();
                            RelayService.SendMessageAsync(
                                HawaiiClient.HawaiiApplicationId,
                                this.RelayContext.Endpoint,
                                (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject])).creator.hawaiiID + ","
                                + (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject])).recipients,
                                message,
                                this.OnCompleteSendTask);
                        }
                        NavigationService.GoBack();
                    });
                //this.DisplayMessage("Sending message to group(s) succeeded.", "Info");
                
            }
            else
            {
                this.Dispatcher.BeginInvoke(
                    delegate
                    {
                        ProgressBar.IsVisible = false;
                        NameTextBox.IsEnabled = true;
                        DescTextBox.IsEnabled = true;
                        DueSwitch.IsEnabled = true;
                        DueDatePicker.IsEnabled = true;
                        DueTimePicker.IsEnabled = true;
                        RemindSwitch.IsEnabled = true;
                        ReminderDatePicker.IsEnabled = true;
                        ReminderTimePicker.IsEnabled = true;
                        AssignToSwitch.IsEnabled = true;
                        AssignPicker.IsEnabled = true;
                    });
                this.DisplayMessage("Task was unable to be assigned. Check your wifi/cellular connection.", "Error");
            }
        }

        private void OnCompleteSendTask(MessagingResult result)
        {
            if (result.Status == Status.Success)
            {
            }
            else
            {
                this.Dispatcher.BeginInvoke(
                    delegate
                    {
                        ProgressBar.IsVisible = false;
                        NameTextBox.IsEnabled = true;
                        DescTextBox.IsEnabled = true;
                        DueSwitch.IsEnabled = true;
                        DueDatePicker.IsEnabled = true;
                        DueTimePicker.IsEnabled = true;
                        RemindSwitch.IsEnabled = true;
                        ReminderDatePicker.IsEnabled = true;
                        ReminderTimePicker.IsEnabled = true;
                        AssignToSwitch.IsEnabled = true;
                        AssignPicker.IsEnabled = true;
                    });
                this.DisplayMessage("Task was unable to be assigned. Check your wifi/cellular connection.", "Error");
            }
        }

        MyTask editTask = null;
        private void EditProjectTask() //edit a project-associated task
        {
            editTask = new MyTask
            {
                name = NameTextBox.Text,
                description = DescTextBox.Text,
                assignedTo = (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey]).assignedTo,
                isComplete = (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey]).isComplete,
                project = (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey]).project,
                projKey = (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey]).projKey,
                status = (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey]).status,
                taskKey = (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey]).taskKey
            };
            if ((bool)DueSwitch.IsChecked) //change duedate
            {
                var dueDate = (DateTime)DueDatePicker.Value;
                var dueTime = (DateTime)DueTimePicker.Value;
                editTask.dueDate = new DateTime(dueDate.Year, dueDate.Month, dueDate.Day, dueDate.Hour, dueDate.Minute, dueDate.Second);
            }
            else
                editTask.dueDate = DateTime.MinValue;

            if ((bool)RemindSwitch.IsChecked) //change remind date
            {
                var remindDate = (DateTime)ReminderDatePicker.Value;
                var remindTime = (DateTime)ReminderTimePicker.Value;
                editTask.remindDate = new DateTime(remindDate.Year, remindDate.Month, remindDate.Day, remindTime.Hour, remindTime.Minute, remindTime.Second);
            }
            else
                editTask.remindDate = DateTime.MinValue;

            byte[] message = editTask.Serialize();
            RelayService.SendMessageAsync(
                HawaiiClient.HawaiiApplicationId,
                this.RelayContext.Endpoint,
                (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject])).creator.hawaiiID + ","
                 + (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject])).recipients,
                message, this.OnCompleteSendEditTask);
        }

        private void OnCompleteSendEditTask(MessagingResult result)
        {
            if (result.Status == Status.Success)
            {
                this.Dispatcher.BeginInvoke(
                    delegate
                    {
                        (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey]).name = NameTextBox.Text;
                        (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey]).description = DescTextBox.Text;

                        if ((bool)DueSwitch.IsChecked) //change duedate
                        {
                            var dueDate = (DateTime)DueDatePicker.Value;
                            var dueTime = (DateTime)DueTimePicker.Value;
                            (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey]).dueDate = new DateTime(dueDate.Year, dueDate.Month, dueDate.Day, dueDate.Hour, dueDate.Minute, dueDate.Second);
                        }
                        else
                            (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey]).dueDate = DateTime.MinValue;

                        if ((bool)RemindSwitch.IsChecked) //change remind date and update reminder in system
                        {
                            var remindDate = (DateTime)ReminderDatePicker.Value;
                            var remindTime = (DateTime)ReminderTimePicker.Value;
                            (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey]).remindDate = new DateTime(remindDate.Year, remindDate.Month, remindDate.Day, remindTime.Hour, remindTime.Minute, remindTime.Second);
                            if (editTask.assignedTo.hawaiiID == this.RelayContext.Endpoint.RegistrationId)
                            {
                                try
                                {
                                    ScheduledActionService.Remove((((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey]).taskKey);
                                }
                                catch (InvalidOperationException) { }
                                Reminder reminder = new Reminder((((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey]).taskKey);
                                Uri navUri = new Uri("/TaskView.xaml?TaskKey=" + selectedKey + "&ProjectKey=" + selectedProject, UriKind.RelativeOrAbsolute);
                                reminder.Title = (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey]).name;
                                reminder.Content = (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey]).description;
                                reminder.BeginTime = (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey]).remindDate;
                                reminder.ExpirationTime = (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey]).remindDate;
                                reminder.NavigationUri = navUri;
                                ScheduledActionService.Add(reminder);
                            }
                        }
                        else
                            (((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks[selectedKey]).remindDate = DateTime.MinValue;

                        ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks = TaskSort(
                            ((MyProject)((ObservableCollection<MyProject>)settings["ProjectList"])[selectedProject]).tasks);

                        NavigationService.GoBack();
                    });
            }
            else
            {
                ProgressBar.IsVisible = false;
                NameTextBox.IsEnabled = true;
                DescTextBox.IsEnabled = true;
                DueSwitch.IsEnabled = true;
                DueDatePicker.IsEnabled = true;
                DueTimePicker.IsEnabled = true;
                RemindSwitch.IsEnabled = true;
                ReminderDatePicker.IsEnabled = true;
                ReminderTimePicker.IsEnabled = true;
                AssignToSwitch.IsEnabled = true;
                AssignPicker.IsEnabled = true;
                this.DisplayMessage("Task was unable to be edited. Check your wifi/cellular connection.", "Error");
            }
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

        private void DueSwitch_Checked(object sender, RoutedEventArgs e) //sets visibility of duedate picker
        {
            DueCanvas.Visibility = System.Windows.Visibility.Visible;
            TaskPanel.Height += 100;
        }

        private void DueSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            DueCanvas.Visibility = System.Windows.Visibility.Collapsed;
            TaskPanel.Height -= 100;
        }

        private void RemindSwitch_Checked(object sender, RoutedEventArgs e) //sets visibility of reminddate picker
        {
            RemindCanvas.Visibility = System.Windows.Visibility.Visible;
            TaskPanel.Height += 100;
        }

        private void RemindSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            RemindCanvas.Visibility = System.Windows.Visibility.Collapsed;
            TaskPanel.Height -= 100;
        }

        private void AssignToSwitch_Checked(object sender, RoutedEventArgs e)
        {
            AssignPicker.Visibility = System.Windows.Visibility.Visible;
            TaskPanel.Height += 87;
        }

        private void AssignToSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            AssignPicker.Visibility = System.Windows.Visibility.Collapsed;
            TaskPanel.Height -= 87;
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

        double assignHeight = -1;
        private void AssignPicker_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (AssignPicker.Height != 87)
            {
                TaskPanel.Height += AssignPicker.Height - 87;
                assignHeight = AssignPicker.Height;
            }
            else if (AssignPicker.Height == 87)
            {
                TaskPanel.Height -= assignHeight;
            }
        }
    }
}