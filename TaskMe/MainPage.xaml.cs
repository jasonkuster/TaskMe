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
using Microsoft.Hawaii;
using Microsoft.Hawaii.Relay.Client;
using System.Text;
using Microsoft.Phone.Scheduler;

namespace TaskMe
{
    public partial class MainPage : PhoneApplicationPage
    {
        IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

        private delegate void DisplayMessagesDelegate();

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            //DataContext = App.ViewModel;
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);

            initSettings();
        }

        private void initSettings()
        {
            //Code to initialize all app storage
            if (!settings.Contains("TaskList"))
            {
                settings["TaskList"] = new ObservableCollection<MyTask>();
            }
            if (!settings.Contains("DoneTaskList"))
            {
                settings["DoneTaskList"] = new ObservableCollection<MyTask>();
            }
            if (!settings.Contains("ProjectList"))
            {
                settings["ProjectList"] = new ObservableCollection<MyProject>();
            }
            if (!settings.Contains("DoneProjectList"))
            {
                settings["DoneProjectList"] = new ObservableCollection<MyProject>();
            }
            if (!settings.Contains("ContactsList"))
            {
                settings["ContactsList"] = new ObservableCollection<Person>();
            }
            if (!settings.Contains("RequestList"))
            {
                settings["RequestList"] = new ObservableCollection<Person>();
            }
            if (!settings.Contains("MessageList"))
            {
                settings["MessageList"] = new ObservableCollection<TaskMeMessage>();
            }
            if (!settings.Contains("AllMessageList"))
            {
                settings["AllMessageList"] = new ObservableCollection<TaskMeMessage>();
            }
        }

        // Load data for the ViewModel Items
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            //if (!App.ViewModel.IsDataLoaded)
            //{
            //    App.ViewModel.LoadData();
            //}
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Navigated To Main");
            if ((bool)settings["FirstRun"]) //Checks if this is the first run of the app
            {
                NavigationService.Navigate(new Uri("/StartPage.xaml", UriKind.RelativeOrAbsolute)); //If it is, navigates to the startpage to set up an Id
                return;
            }
            if (this.RelayContext.Endpoint != null) //Retrieves messages (Update to check if network is connected)
            {
                ((ApplicationBarMenuItem)ApplicationBar.MenuItems[0]).IsEnabled = true;
                RelayService.ReceiveMessagesAsync(HawaiiClient.HawaiiApplicationId, this.RelayContext.Endpoint, string.Empty, this.ReceiveMessages);
            }
            else
                ((ApplicationBarMenuItem)ApplicationBar.MenuItems[0]).IsEnabled = false;

            //Updates sources and performs logic for task, project, and contact pivots
            SetTaskSource();
            SetProjectSource();
            SetContactsSource();

            //Updates the main LiveTile with number of tasks/projects and a list of top tasks/projects
            UpdateLT();
        }

        #region ListBox Sources

        private void SetTaskSource() //Performs the logic for the Tasks pivot.
        {
            if (TaskListBox.ItemsSource == ((ObservableCollection<MyTask>)settings["DoneTaskList"])
                && ((ObservableCollection<MyTask>)settings["DoneTaskList"]).Count != 0) //Checks if the list is pointing to done tasks and if so, updates.
            {
                //System.Diagnostics.Debug.WriteLine("Done Task List, count: " + ((ObservableCollection<MyTask>)settings["DoneTaskList"]).Count);
                TaskListBox.ItemsSource = null;
                TaskListBox.ItemsSource = ((ObservableCollection<MyTask>)settings["DoneTaskList"]);
                TaskViewButton.Content = "View Incomplete Tasks";
                TaskViewButton.IsEnabled = true;
            }
            else //It's pointing to the open tasks box.
            {
                TaskListBox.ItemsSource = null; //Refresh ListBox
                TaskListBox.ItemsSource = ((ObservableCollection<MyTask>)settings["TaskList"]);

                if (((ObservableCollection<MyTask>)settings["DoneTaskList"]).Count == 0) //Checks if there are completed tasks
                    TaskViewButton.IsEnabled = false; //If there are not, disables the button
                else
                    TaskViewButton.IsEnabled = true; //If there are, enables the button

                if (((ObservableCollection<MyTask>)settings["TaskList"]).Count() != 0) //Enable TLB if >0 tasks in the tasklist
                {
                    //System.Diagnostics.Debug.WriteLine("TaskList Wasn't Empty: " + ((ObservableCollection<MyTask>)settings["TaskList"]).Count());
                    TaskListBox.IsEnabled = true;
                    TaskListBox.Visibility = System.Windows.Visibility.Visible;
                    NoTasksTB.Visibility = System.Windows.Visibility.Collapsed;
                    AddNewTaskTB.Visibility = System.Windows.Visibility.Collapsed;
                }
                else //Else disable it.
                {
                    TaskListBox.IsEnabled = false;
                    TaskListBox.Visibility = System.Windows.Visibility.Collapsed;
                    NoTasksTB.Visibility = System.Windows.Visibility.Visible;
                    AddNewTaskTB.Visibility = System.Windows.Visibility.Visible;
                }
            }
        }

        private void SetProjectSource() //Performs logic for the Projects pivot
        {
            if (ProjectListBox.ItemsSource == ((ObservableCollection<MyProject>)settings["DoneProjectList"])
                && ((ObservableCollection<MyProject>)settings["DoneProjectList"]).Count != 0) //Checks if the list is pointing to done projects and if so, updates.
            {
                ProjectListBox.ItemsSource = null;
                ProjectListBox.ItemsSource = ((ObservableCollection<MyProject>)settings["DoneProjectList"]);
                ProjViewButton.Content = "View Incomplete Projects";
                ProjViewButton.IsEnabled = true;
            }
            else //It's pointing to the open projects box.
            {
                ProjectListBox.ItemsSource = null; //Refresh ListBox
                ProjectListBox.ItemsSource = ((ObservableCollection<MyProject>)settings["ProjectList"]);

                if (((ObservableCollection<MyProject>)settings["DoneProjectList"]).Count == 0) //Checks if there are completed projects
                    ProjViewButton.IsEnabled = false; //If there are not, disables the button
                else
                    ProjViewButton.IsEnabled = true; //If there are, enables the button

                if (((ObservableCollection<MyProject>)settings["ProjectList"]).Count() != 0) //Enable PLB if >0 projects in projectlist
                {
                    System.Diagnostics.Debug.WriteLine("ProjectList Wasn't Empty" + ((ObservableCollection<MyProject>)settings["ProjectList"]).Count());
                    ProjectListBox.IsEnabled = true;
                    ProjectListBox.Visibility = System.Windows.Visibility.Visible;
                    NoProjectsTB.Visibility = System.Windows.Visibility.Collapsed;
                    AddNewProjectTB.Visibility = System.Windows.Visibility.Collapsed;
                }
                else //else disable it
                {
                    ProjectListBox.IsEnabled = false;
                    ProjectListBox.Visibility = System.Windows.Visibility.Collapsed;
                    NoProjectsTB.Visibility = System.Windows.Visibility.Visible;
                    AddNewProjectTB.Visibility = System.Windows.Visibility.Visible;
                }
            }
        }

        private void SetContactsSource() //Performs logic for the contacts pivot
        {
            if (ContactsListBox.ItemsSource == ((ObservableCollection<Person>)settings["RequestList"])
                && ((ObservableCollection<Person>)settings["RequestList"]).Count != 0)  //Checks if the list is pointing to contact requests and if so, updates.
            {
                ContactsListBox.ItemsSource = null;
                ContactsListBox.ItemsSource = ((ObservableCollection<Person>)settings["RequestList"]);
                ContactsViewButton.Content = "View Contacts";
                ContactsViewButton.IsEnabled = true;
            }
            else //It's pointing to the accepted contacts box
            {
                ContactsListBox.ItemsSource = null; //Refresh ListBox
                ContactsListBox.ItemsSource = ((ObservableCollection<Person>)settings["ContactsList"]);
                ContactsViewButton.Content = "View Requests";

                if (((ObservableCollection<Person>)settings["RequestList"]).Count == 0)
                    ContactsViewButton.IsEnabled = false;
                else
                    ContactsViewButton.IsEnabled = true;

                if (this.RelayContext.Endpoint != null) //If there is a username attached
                {
                    NoContactsTB.Text = "no contacts";
                    Canvas.SetLeft(NoContactsTB, 181);
                    AddNewContactsTB.Text = "add a new one below\n";
                    Canvas.SetLeft(AddNewContactsTB, 139);
                    if (((ObservableCollection<Person>)settings["ContactsList"]).Count() != 0) //If there are contacts
                    {
                        ContactsListBox.IsEnabled = true;
                        ContactsListBox.Visibility = System.Windows.Visibility.Visible;
                        NoContactsTB.Visibility = System.Windows.Visibility.Collapsed;
                        AddNewContactsTB.Visibility = System.Windows.Visibility.Collapsed;
                    }
                    else //If there are no contacts
                    {
                        ContactsListBox.IsEnabled = false;
                        ContactsListBox.Visibility = System.Windows.Visibility.Collapsed;
                        NoContactsTB.Visibility = System.Windows.Visibility.Visible;
                        AddNewContactsTB.Visibility = System.Windows.Visibility.Visible;
                    }
                }
                else //No username has been set yet. Allow the user to create one
                {
                    ContactsListBox.IsEnabled = false;
                    ContactsListBox.Visibility = System.Windows.Visibility.Collapsed;
                    NoContactsTB.Visibility = System.Windows.Visibility.Visible;
                    AddNewContactsTB.Visibility = System.Windows.Visibility.Visible;
                    NoContactsTB.Text = "no username set";
                    Canvas.SetLeft(NoContactsTB, 160);
                    AddNewContactsTB.Text = "click add to set one up";
                    Canvas.SetLeft(AddNewContactsTB, 129);
                }
            }
        }

        #endregion

        private void UpdateLT() //Code to update the main live tile
        {
            int newCount = 0;
            string newBack = "";
            int iter = 3;
            if (((ObservableCollection<MyProject>)settings["ProjectList"]).Count() != 0)
            {
                foreach (MyProject proj in ((ObservableCollection<MyProject>)settings["ProjectList"]))
                {
                    newBack += proj.name + "\n";
                    if (!proj.dueDate.Equals(DateTime.MinValue))
                        newBack += "- " + proj.dueDate.ToShortDateString() + "\n";
                    if (--iter < 2)
                        break;
                }
            }

            if (((ObservableCollection<MyTask>)settings["TaskList"]).Count() != 0)
            {
                foreach (MyTask task in ((ObservableCollection<MyTask>)settings["TaskList"]))
                {
                    newBack += task.name + "\n";
                    if (!task.dueDate.Equals(DateTime.MinValue))
                        newBack += "- " + task.dueDate.ToShortDateString() + "\n";
                    if (--iter < 0)
                        break;
                }
            }

            if (string.IsNullOrEmpty(newBack))
                newBack = "No Active Items";

            // Application Tile is always the first Tile, even if it is not pinned to Start.
            ShellTile TileToFind = ShellTile.ActiveTiles.First();

            // Application should always be found
            if (TileToFind != null)
            {
                // Count will be the number of projects plus the number of tasks
                newCount = ((ObservableCollection<MyProject>)settings["ProjectList"]).Count + ((ObservableCollection<MyTask>)settings["TaskList"]).Count;

                // Set the properties to update for the Application Tile.
                // Empty strings for the text values and URIs will result in the property being cleared.
                StandardTileData NewTileData = new StandardTileData
                {
                    //Title = textBoxTitle.Text,
                    //BackgroundImage = new Uri(textBoxBackgroundImage.Text, UriKind.Relative),
                    Count = newCount,
                    //BackTitle = textBoxBackTitle.Text,
                    //BackBackgroundImage = new Uri(textBoxBackBackgroundImage.Text, UriKind.Relative),
                    BackContent = newBack
                };

                // Update the Application Tile
                TileToFind.Update(NewTileData);
            }
        }

        #region Hawaii Messages

        private void ReceiveMessages(MessagingResult result)
        {
            if (result.Status == Status.Success)
            {
                if (result.Messages == null || result.Messages.Count == 0)
                {
                    this.Dispatcher.BeginInvoke(
                        delegate
                        {
                            UnlockUI();
                        });
                    //this.DisplayMessage("No new messages available.", "Info");
                }
                else
                {
                    this.Dispatcher.BeginInvoke(
                        delegate
                        {
                            LockUI();
                        });
                    foreach (Message message in result.Messages)
                    {
                        this.RelayContext.Messages.Add(message);
                    }

                    this.Dispatcher.BeginInvoke(new DisplayMessagesDelegate(this.ParseMessages));
                }
            }
            else
            {
                this.Dispatcher.BeginInvoke(
                    delegate
                    {
                        UnlockUI();
                    });
                this.DisplayMessage("Error receiving messages. Check your wifi/cellular connection.", "Error");
            }
        }

        private void LockUI()
        {
            ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
            ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = false;
            ProgressBar.IsVisible = true;
            TaskListBox.IsEnabled = false;
            ProjectListBox.IsEnabled = false;
            ContactsListBox.IsEnabled = false;
        }

        private void UnlockUI()
        {
            ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = true;
            ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = true;
            ProgressBar.IsVisible = false;
            TaskListBox.IsEnabled = true;
            ProjectListBox.IsEnabled = true;
            ContactsListBox.IsEnabled = true;
        }

        private void DisplayMessage(string message, string caption)
        {
            this.Dispatcher.BeginInvoke(
                    delegate
                    {
                        MessageBox.Show(message, caption, MessageBoxButton.OK);
                    });
        }

        private void ParseMessages()
        {
            foreach (Message message in this.RelayContext.Messages)
            {
                string from = message.From;
                if (from.Equals(this.RelayContext.Endpoint.RegistrationId))
                    continue;
                string[] contents = Encoding.Unicode.GetString(message.Body, 0, message.Body.Length).Split('\0');
                if (contents[0].Equals("FReq")) //Contact request message
                {
                    ParseFReq(from, contents);
                }
                else if (contents[0].Equals("Accept")) //Contact request accepted message
                {
                    ParseAccept(from, contents);
                }
                else if (contents[0].Equals("ContactDelete")) //Deleting a contact. Removes the person and reassigns all of their tasks to the project creator
                {
                    ParseContactDelete(from, contents);
                }
                else if (contents[0].Equals("Project"))
                {
                    ParseProject(from, contents);
                }
                else if (contents[0].Equals("Task"))
                {
                    ParseTask(from, contents);
                }
                else if (contents[0].Equals("DoneTask"))
                {
                    ParseDoneTask(from, contents);
                }
                else if (contents[0].Equals("TaskUndo"))
                {
                    ParseTaskUndo(from, contents);
                }
                else if (contents[0].Equals("TaskDelete"))
                {
                    ParseTaskDelete(from, contents);
                }
                else if (contents[0].Equals("TaskComplete"))
                {
                    ParseTaskComplete(from, contents);
                }
                else if (contents[0].Equals("ProjectUndo"))
                {
                    ParseProjectUndo(from, contents);
                }
                else if (contents[0].Equals("ProjectComplete"))
                {
                    ParseProjectComplete(from, contents);
                }
                else if (contents[0].Equals("ProjectDelete"))
                {
                    ParseProjectDelete(from, contents);
                }
            }
            if (((ObservableCollection<TaskMeMessage>)settings["MessageList"]).Count > 0)
            {
                this.RelayContext.Messages.RemoveRange(0, this.RelayContext.Messages.Count());
                System.Diagnostics.Debug.WriteLine(((ObservableCollection<TaskMeMessage>)settings["MessageList"]).Count);
                if (MessageBox.Show("New messages received, view now?", "New Messages", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    NavigationService.Navigate(new Uri("/MessageView.xaml", UriKind.RelativeOrAbsolute));
                }
            }

            UnlockUI();

        }

        private void ParseProjectDelete(string from, string[] contents)
        {
            foreach (MyProject search in ((ObservableCollection<MyProject>)settings["ProjectList"]))
            {
                if (contents[1].Equals(search.projKey))
                {
                    if (from.Equals(search.creator.hawaiiID))
                    {
                        foreach (MyTask searchTask in search.tasks)
                        {
                            try
                            {
                                ScheduledActionService.Remove(searchTask.taskKey);
                            }
                            catch (InvalidOperationException) { }
                        }
                        ((ObservableCollection<TaskMeMessage>)settings["MessageList"]).Insert(0,
                            new TaskMeMessage
                            {
                                type = "Project Deleted",
                                content = "Project " + search.name
                                    + " was deleted by its creator, " + search.creator.username + ".",
                                timeStamp = DateTime.Now
                            });
                        ((ObservableCollection<MyProject>)settings["ProjectList"]).Remove(search);
                        break;
                    }
                    else
                    {
                        string name = "";
                        List<string> affectedTasks = new List<string>();
                        foreach (MyTask searchTask in search.tasks)
                        {
                            if (from.Equals(searchTask.assignedTo.hawaiiID))
                            {
                                name = searchTask.assignedTo.username;
                                affectedTasks.Add(searchTask.name);
                                searchTask.assignedTo = search.creator;
                            }
                        }

                        StringBuilder msg = new StringBuilder();
                        if (affectedTasks.Count == 0)
                            msg.Append(name + " deleted Project " + search.name + ". No tasks were affected.");
                        else
                        {
                            msg.Append(name + " deleted Project " + search.name + ". Affected Tasks: ");
                            foreach (string task in affectedTasks)
                            {
                                msg.Append(task + ",");
                            }
                            msg.Remove(msg.Length - 1, 1);
                            msg.Append(" have been assigned to " + search.creator.username + ".");
                        }
                        ((ObservableCollection<TaskMeMessage>)settings["MessageList"]).Insert(0,
                            new TaskMeMessage { type = "Project Deleted", content = msg.ToString(), timeStamp = DateTime.Now });
                        break;
                    }
                }
            }
            SetProjectSource();
        }

        private void ParseProjectComplete(string from, string[] contents)
        {
            foreach (MyProject search in ((ObservableCollection<MyProject>)settings["ProjectList"]))
            {
                if (contents[1].Equals(search.projKey))
                {
                    search.isComplete = true;
                    search.status = "Complete";
                    foreach (MyTask searchTask in search.tasks)
                    {
                        try
                        {
                            ScheduledActionService.Remove(searchTask.taskKey);
                        }
                        catch (InvalidOperationException) { }
                    }
                    ((ObservableCollection<MyProject>)settings["DoneProjectList"]).Add(search);
                    settings["DoneProjectList"] = ProjectSort(((ObservableCollection<MyProject>)settings["DoneProjectList"]));
                    ((ObservableCollection<MyProject>)settings["ProjectList"]).Remove(search);
                    ((ObservableCollection<TaskMeMessage>)settings["MessageList"]).Insert(0,
                        new TaskMeMessage { type = "Project Completed", content = "Project " + search.name + " was marked as complete.", timeStamp = DateTime.Now });
                    break;
                }
            }
            SetProjectSource();
        }

        private void ParseProjectUndo(string from, string[] contents)
        {
            foreach (MyProject search in ((ObservableCollection<MyProject>)settings["DoneProjectList"]))
            {
                if (contents[1].Equals(search.projKey))
                {
                    search.isComplete = false;
                    search.status = "Incomplete";
                    foreach (MyTask searchTask in search.tasks)
                    {
                        if (searchTask.remindDate != DateTime.MinValue && searchTask.remindDate > DateTime.Now && this.RelayContext.Endpoint.RegistrationId.Equals(searchTask.assignedTo.hawaiiID))
                        {
                            try
                            {
                                ScheduledActionService.Remove(searchTask.taskKey);
                            }
                            catch (InvalidOperationException) { }
                            Reminder reminder = new Reminder(searchTask.taskKey);
                            Uri navUri = new Uri("/TaskView.xaml?TaskKey=" + searchTask.taskKey + "&ProjectKey=" + search.projKey, UriKind.RelativeOrAbsolute);
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
                    ((ObservableCollection<MyProject>)settings["ProjectList"]).Add(search);
                    settings["ProjectList"] = ProjectSort(((ObservableCollection<MyProject>)settings["ProjectList"]));
                    ((ObservableCollection<MyProject>)settings["DoneProjectList"]).Remove(search);
                    ((ObservableCollection<TaskMeMessage>)settings["MessageList"]).Insert(0,
                        new TaskMeMessage { type = "Project Reopened", content = "Project " + search.name + " was reopened.", timeStamp = DateTime.Now });
                    break;
                }
            }
            SetProjectSource();
        }

        private void ParseTaskComplete(string from, string[] contents)
        {
            foreach (MyProject search in ((ObservableCollection<MyProject>)settings["ProjectList"]))
            {
                if (contents[1].Equals(search.projKey))
                {
                    foreach (MyTask searchTask in search.tasks)
                    {
                        if (contents[2].Equals(searchTask.taskKey))
                        {
                            try
                            {
                                ScheduledActionService.Remove(searchTask.taskKey);
                            }
                            catch (InvalidOperationException) { }
                            searchTask.isComplete = true;
                            searchTask.status = "Complete";
                            search.doneTasks.Add(searchTask);
                            search.doneTasks = TaskSort(search.doneTasks);
                            ((ObservableCollection<TaskMeMessage>)settings["MessageList"]).Insert(0,
                                new TaskMeMessage { type = "Task Completed", content = "Task " + searchTask.name + " in Project " + search.name + " was marked as complete.", timeStamp = DateTime.Now });
                            search.tasks.Remove(searchTask);
                            break;
                        }
                    }
                    break;
                }
            }
        }

        private void ParseTaskDelete(string from, string[] contents)
        {
            foreach (MyProject search in ((ObservableCollection<MyProject>)settings["ProjectList"]))
            {
                if (contents[1].Equals(search.projKey))
                {
                    foreach (MyTask searchTask in search.tasks)
                    {
                        if (contents[2].Equals(searchTask.taskKey))
                        {
                            try
                            {
                                ScheduledActionService.Remove(searchTask.taskKey);
                            }
                            catch (InvalidOperationException) { }
                            ((ObservableCollection<TaskMeMessage>)settings["MessageList"]).Insert(0,
                                new TaskMeMessage { type = "Task Deleted", content = "Task " + searchTask.name + " in Project " + search.name + " was deleted.", timeStamp = DateTime.Now });
                            search.tasks.Remove(searchTask);
                            break;
                        }
                    }
                    break;
                }
            }
        }

        private void ParseTaskUndo(string from, string[] contents)
        {
            foreach (MyProject search in ((ObservableCollection<MyProject>)settings["DoneProjectList"]))
            {
                if (contents[1].Equals(search.projKey))
                {
                    foreach (MyTask searchTask in search.doneTasks)
                    {
                        if (contents[2].Equals(searchTask.taskKey))
                        {
                            searchTask.isComplete = false;
                            searchTask.status = "Incomplete";
                            if (searchTask.remindDate != DateTime.MinValue && searchTask.remindDate > DateTime.Now && this.RelayContext.Endpoint.RegistrationId.Equals(searchTask.assignedTo.hawaiiID))
                            {
                                try
                                {
                                    ScheduledActionService.Remove(searchTask.taskKey);
                                }
                                catch (InvalidOperationException) { }
                                Reminder reminder = new Reminder(searchTask.taskKey);
                                Uri navUri = new Uri("/TaskView.xaml?TaskKey=" + searchTask.taskKey + "&ProjectKey=" + search.projKey, UriKind.RelativeOrAbsolute);
                                reminder.Title = searchTask.name;
                                reminder.Content = searchTask.description;
                                reminder.BeginTime = searchTask.remindDate;
                                reminder.ExpirationTime = searchTask.remindDate;
                                reminder.NavigationUri = navUri;
                                ScheduledActionService.Add(reminder);
                            }
                            else if (searchTask.remindDate < DateTime.Now)
                                searchTask.remindDate = DateTime.MinValue;
                            search.tasks.Add(searchTask);
                            search.tasks = TaskSort(search.tasks);
                            ((ObservableCollection<TaskMeMessage>)settings["MessageList"]).Insert(0,
                                new TaskMeMessage { type = "Task Reopened", content = "Completed Task " + searchTask.name + " in Project " + search.name + " was reopened.", timeStamp = DateTime.Now });
                            search.doneTasks.Remove(searchTask);
                            break;
                        }
                    }
                    break;
                }
            }
        }

        private void ParseDoneTask(string from, string[] contents)
        {
            foreach (MyProject search in ((ObservableCollection<MyProject>)settings["ProjectList"]))
            {
                if (contents[1].Equals(search.projKey))
                {
                    MyTask create = new MyTask();
                    create.projKey = contents[1];
                    create.name = contents[2];
                    create.description = contents[3];
                    create.taskKey = contents[4];
                    create.dueDate = DateTime.Parse(contents[5]);
                    if (DateTime.Parse(contents[6]) > DateTime.Now)
                        create.remindDate = DateTime.Parse(contents[6]);
                    else
                        create.remindDate = DateTime.MinValue;
                    create.isComplete = Convert.ToBoolean(contents[7]);
                    create.status = contents[8];
                    create.project = contents[9];
                    Person createP = new Person();
                    createP.name = contents[10];
                    createP.username = contents[11];
                    createP.hawaiiID = contents[12];
                    createP.reqSent = Convert.ToBoolean(contents[13]);
                    createP.accepted = Convert.ToBoolean(contents[14]);
                    create.assignedTo = createP;
                    search.doneTasks.Add(create);
                    search.doneTasks = TaskSort(search.doneTasks);
                    ((ObservableCollection<TaskMeMessage>)settings["MessageList"]).Insert(0,
                        new TaskMeMessage { type = "Task Created", content = "Completed task " + create.name + " in Project " + search.name + " was created.", timeStamp = DateTime.Now });
                    break;
                }
            }
        }

        private void ParseTask(string from, string[] contents)
        {
            foreach (MyProject search in ((ObservableCollection<MyProject>)settings["ProjectList"]))
            {
                if (contents[1].Equals(search.projKey))
                {
                    bool taskFound = false;
                    foreach (MyTask searchTask in search.tasks)
                    {
                        if (contents[4].Equals(searchTask.taskKey))
                        {
                            searchTask.name = contents[2];
                            searchTask.description = contents[3];
                            searchTask.dueDate = DateTime.Parse(contents[5]);
                            searchTask.remindDate = DateTime.Parse(contents[6]);
                            searchTask.isComplete = Convert.ToBoolean(contents[7]);
                            searchTask.status = contents[8];
                            searchTask.project = contents[9];
                            Person createP = new Person();
                            createP.name = contents[10];
                            createP.username = contents[11];
                            createP.hawaiiID = contents[12];
                            createP.reqSent = Convert.ToBoolean(contents[13]);
                            createP.accepted = Convert.ToBoolean(contents[14]);
                            searchTask.assignedTo = createP;
                            if (!searchTask.remindDate.Equals(DateTime.MinValue) && searchTask.remindDate > DateTime.Now && searchTask.assignedTo.hawaiiID == this.RelayContext.Endpoint.RegistrationId)
                            {
                                try
                                {
                                    ScheduledActionService.Remove(searchTask.taskKey);
                                }
                                catch (InvalidOperationException) { }
                                Reminder reminder = new Reminder(searchTask.taskKey);
                                Uri navUri = new Uri("/TaskView.xaml?TaskKey=" + searchTask.taskKey + "&ProjectKey=" + search.projKey, UriKind.RelativeOrAbsolute);
                                reminder.Title = searchTask.name;
                                reminder.Content = searchTask.description;
                                reminder.BeginTime = searchTask.remindDate;
                                reminder.ExpirationTime = searchTask.remindDate;
                                reminder.NavigationUri = navUri;
                                ScheduledActionService.Add(reminder);
                            }
                            else if (searchTask.remindDate <= DateTime.Now)
                                searchTask.remindDate = DateTime.MinValue;
                            search.tasks = TaskSort(search.tasks);
                            taskFound = true;
                            ((ObservableCollection<TaskMeMessage>)settings["MessageList"]).Insert(0,
                                new TaskMeMessage { type = "Task Updated", content = "Task " + searchTask.name + " in Project " + search.name + " was updated.", timeStamp = DateTime.Now });
                            break;
                        }
                    }
                    if (!taskFound)
                    {
                        MyTask create = new MyTask();
                        create.projKey = contents[1];
                        create.name = contents[2];
                        create.description = contents[3];
                        create.taskKey = contents[4];
                        create.dueDate = DateTime.Parse(contents[5]);
                        if (DateTime.Parse(contents[6]) > DateTime.Now)
                            create.remindDate = DateTime.Parse(contents[6]);
                        else
                            create.remindDate = DateTime.MinValue;
                        create.isComplete = Convert.ToBoolean(contents[7]);
                        create.status = contents[8];
                        create.project = contents[9];
                        Person createP = new Person();
                        createP.name = contents[10];
                        createP.username = contents[11];
                        createP.hawaiiID = contents[12];
                        createP.reqSent = Convert.ToBoolean(contents[13]);
                        createP.accepted = Convert.ToBoolean(contents[14]);
                        create.assignedTo = createP;
                        search.tasks.Add(create);
                        search.tasks = TaskSort(search.tasks);
                        if (!create.remindDate.Equals(DateTime.MinValue) && create.assignedTo.hawaiiID == this.RelayContext.Endpoint.RegistrationId)
                        {
                            try
                            {
                                ScheduledActionService.Remove(create.taskKey);
                            }
                            catch (InvalidOperationException) { }
                            Reminder reminder = new Reminder(create.taskKey);
                            Uri navUri = new Uri("/TaskView.xaml?TaskKey=" + create.taskKey + "&ProjectKey=" + search.projKey, UriKind.RelativeOrAbsolute);
                            reminder.Title = create.name;
                            reminder.Content = create.description;
                            reminder.BeginTime = create.remindDate;
                            reminder.ExpirationTime = create.remindDate;
                            reminder.NavigationUri = navUri;
                            ScheduledActionService.Add(reminder);
                        }
                        ((ObservableCollection<TaskMeMessage>)settings["MessageList"]).Insert(0,
                                new TaskMeMessage { type = "Task Created", content = "Task " + create.name + " in Project " + search.name + " was created.", timeStamp = DateTime.Now });
                    }
                    break;
                }
            }
        }

        private void ParseProject(string from, string[] contents)
        {
            bool foundProject = false;
            foreach (MyProject search in ((ObservableCollection<MyProject>)settings["ProjectList"]))
            {
                if (contents[2].Equals(search.projKey))
                {
                    search.name = contents[1];
                    search.projKey = contents[2];
                    search.description = contents[3];
                    search.dueDate = DateTime.Parse(contents[4]);
                    search.isComplete = Convert.ToBoolean(contents[5]);
                    search.status = contents[6];
                    Person createP = new Person();
                    createP.name = contents[7];
                    createP.username = contents[8];
                    createP.hawaiiID = contents[9];
                    createP.reqSent = Convert.ToBoolean(contents[10]);
                    createP.accepted = Convert.ToBoolean(contents[11]);
                    search.creator = createP;
                    search.recipients = contents[12];
                    settings["ProjectList"] = ProjectSort(((ObservableCollection<MyProject>)settings["ProjectList"]));
                    foundProject = true;
                    ((ObservableCollection<TaskMeMessage>)settings["MessageList"]).Insert(0,
                        new TaskMeMessage { type = "Project Updated", content = "Project " + search.name + " was updated.", timeStamp = DateTime.Now });
                    break;
                }
            }
            if (!foundProject)
            {
                MyProject create = new MyProject();
                create.name = contents[1];
                create.projKey = contents[2];
                create.description = contents[3];
                create.dueDate = DateTime.Parse(contents[4]);
                create.isComplete = Convert.ToBoolean(contents[5]);
                create.status = contents[6];
                Person createP = new Person();
                createP.name = contents[7];
                createP.username = contents[8];
                createP.hawaiiID = contents[9];
                createP.reqSent = Convert.ToBoolean(contents[10]);
                createP.accepted = Convert.ToBoolean(contents[11]);
                create.creator = createP;
                create.recipients = contents[12];
                create.tasks = new ObservableCollection<MyTask>();
                create.doneTasks = new ObservableCollection<MyTask>();
                ((ObservableCollection<MyProject>)settings["ProjectList"]).Add(create);
                settings["ProjectList"] = ProjectSort(((ObservableCollection<MyProject>)settings["ProjectList"]));
                ((ObservableCollection<TaskMeMessage>)settings["MessageList"]).Insert(0,
                        new TaskMeMessage { type = "Project Added", content = "Project " + create.name + " was added.", timeStamp = DateTime.Now });
            }
        }

        private void ParseContactDelete(string from, string[] contents)
        {
            List<string> affectedTasks = new List<string>(); //String list for affected tasks
            foreach (MyProject search in ((ObservableCollection<MyProject>)settings["ProjectList"]))
            {
                if (search.creator.hawaiiID.Equals(this.RelayContext.Endpoint.RegistrationId))
                {
                    foreach (MyTask searchTask in search.tasks)
                    {
                        if (from.Equals(searchTask.assignedTo.hawaiiID))
                        {
                            affectedTasks.Add(search.name + "/" + searchTask.name); //Add task name to affected tasks
                            searchTask.assignedTo = search.creator;
                            search.removeRecipient(from);

                            //Add a reminder if this specific endpoint is the creator
                            if (!searchTask.remindDate.Equals(DateTime.MinValue) && searchTask.assignedTo.hawaiiID.Equals(this.RelayContext.Endpoint.RegistrationId))
                            {
                                try
                                {
                                    ScheduledActionService.Remove(searchTask.taskKey);
                                }
                                catch (InvalidOperationException) { }
                                Reminder reminder = new Reminder(searchTask.taskKey);
                                Uri navUri = new Uri("/TaskView.xaml?TaskKey=" + searchTask.taskKey + "&ProjectKey=" + searchTask.projKey, UriKind.RelativeOrAbsolute);
                                reminder.Title = searchTask.name;
                                reminder.Content = searchTask.description;
                                reminder.BeginTime = searchTask.remindDate;
                                reminder.ExpirationTime = searchTask.remindDate;
                                reminder.NavigationUri = navUri;
                                ScheduledActionService.Add(reminder);
                            }

                            byte[] taskMessage = searchTask.Serialize();
                            RelayService.SendMessageAsync(
                                HawaiiClient.HawaiiApplicationId,
                                this.RelayContext.Endpoint,
                                search.recipients,
                                taskMessage, this.OnCompleteSendMessage);
                        }
                    }
                    foreach (MyTask searchTask in search.doneTasks) //Reassign completed tasks as well.
                    {
                        if (from.Equals(searchTask.assignedTo.hawaiiID))
                        {
                            affectedTasks.Add(search.name + "/" + searchTask.name);
                            searchTask.assignedTo = search.creator;
                            search.removeRecipient(from);
                            byte[] taskMessage = searchTask.Serialize();
                            RelayService.SendMessageAsync(
                                HawaiiClient.HawaiiApplicationId,
                                this.RelayContext.Endpoint,
                                search.recipients,
                                taskMessage, this.OnCompleteSendMessage);
                        }
                    }
                }
            }

            string name = "";
            foreach (Person search in ((ObservableCollection<Person>)settings["ContactsList"]))
            {
                if (from.Equals(search.hawaiiID))
                {
                    name = search.username;
                    ((ObservableCollection<Person>)settings["ContactsList"]).Remove(search);
                    break;
                }
            }

            StringBuilder msg = new StringBuilder();
            if (affectedTasks.Count == 0)
                msg.Append(name + " removed you as a contact. They have been removed from your contacts list. No projects or tasks were affected.");
            else
            {
                msg.Append(name + " removed you as a contact. They have been removed from your contacts list. Affected Projects/Tasks: ");
                foreach (string task in affectedTasks)
                {
                    msg.Append(task + ",");
                }
                msg.Remove(msg.Length - 1, 1);
                msg.Append(" have been assigned to you.");
            }

            SetContactsSource();

            ((ObservableCollection<TaskMeMessage>)settings["MessageList"]).Insert(0,
                new TaskMeMessage { type = "Contact Removed", content = msg.ToString(), timeStamp = DateTime.Now });
        }

        private void ParseAccept(string from, string[] contents)
        {
            Person accept = ((ObservableCollection<Person>)settings["RequestList"]).Where(X => X.hawaiiID == from).FirstOrDefault();
            if (accept != null)
            {
                string name = accept.name;
                accept.accepted = true;
                ((ObservableCollection<Person>)settings["ContactsList"]).Add(accept); //Add the contact to the accepted list
                if (((ObservableCollection<Person>)settings["ContactsList"])[0].hawaiiID == this.RelayContext.Endpoint.RegistrationId)
                {
                    ((ObservableCollection<Person>)settings["ContactsList"]).RemoveAt(0); //Remove "Me"
                }
                settings["ContactsList"] = ContactSort((ObservableCollection<Person>)settings["ContactsList"]); //Sort the list
                ((ObservableCollection<Person>)settings["ContactsList"]).Insert(0,
                    new Person
                    {
                        name = "Me",
                        username = (string)settings["MyUsername"],
                        hawaiiID = this.RelayContext.Endpoint.RegistrationId,
                        accepted = true,
                        reqSent = true
                    }); //Re-add "Me"
                ((ObservableCollection<Person>)settings["RequestList"]).Remove(accept); //Remove the contact from the request list

                ((ObservableCollection<TaskMeMessage>)settings["MessageList"]).Insert(0,
                    new TaskMeMessage { type = "Request Accepted", content = name + " accepted your contact request.", timeStamp = DateTime.Now });
            }

            SetContactsSource();
        }

        private void ParseFReq(string from, string[] contents)
        {
            ((ObservableCollection<TaskMeMessage>)settings["MessageList"]).Insert(0,
                        new TaskMeMessage
                        {
                            type = "Contact Request",
                            content = "Received a contact request from " + contents[1]
                                + ". Accept by going to 'requests' on the contacts screen.",
                            timeStamp = DateTime.Now
                        });
            ((ObservableCollection<Person>)settings["RequestList"]).Add(new Person
            {
                name = "Click to Accept",
                hawaiiID = from,
                username = contents[1],
                reqSent = false,
                accepted = false
            }); //Add a new person to the contacts list
            settings["RequestList"] = ContactSort(((ObservableCollection<Person>)settings["RequestList"]));

            SetContactsSource();
        }

        private void OnCompleteSendMessage(MessagingResult result)
        {
            if (result.Status == Status.Success)
            {
                //this.DisplayMessage("Accept succeeded.", "Info");
            }
            else
            {
                this.DisplayMessage("Message sending failed. Check your wifi/cellular connection.", "Error");
            }
        }

        private RelayContext RelayContext
        {
            get { return ((App)App.Current).RelayContext; }
        }

        #endregion

        #region Button Clicks

        private void AddButton_Click(object sender, EventArgs e) //adds a new task, project, or contact based on which pivot is selected
        {
            if(MainPivot.SelectedItem == TaskPivot)
                NavigationService.Navigate(new Uri("/NewTask.xaml", UriKind.RelativeOrAbsolute));
            else if (MainPivot.SelectedItem == ProjectPivot)
                NavigationService.Navigate(new Uri("/NewProject.xaml", UriKind.RelativeOrAbsolute));
            else if (MainPivot.SelectedItem == ContactPivot && this.RelayContext.Endpoint != null)
                NavigationService.Navigate(new Uri("/NewContact.xaml", UriKind.RelativeOrAbsolute));
            else if (MainPivot.SelectedItem == ContactPivot && this.RelayContext.Endpoint == null)
                NavigationService.Navigate(new Uri("/StartPage.xaml", UriKind.RelativeOrAbsolute));
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            if (this.RelayContext.Endpoint != null)
            {
                RelayService.ReceiveMessagesAsync(HawaiiClient.HawaiiApplicationId, this.RelayContext.Endpoint, string.Empty, this.ReceiveMessages);
                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
                ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = false;
                ProgressBar.IsVisible = true;
                TaskListBox.IsEnabled = false;
                ProjectListBox.IsEnabled = false;
                ContactsListBox.IsEnabled = false;
            }
            else
                DisplayMessage("You haven't set up a username yet.\nPlease go to settings and set one up.", "Error.");
        }

        private void AboutMenuItem_Click(object sender, EventArgs e) //Navigates to the about page
        {
            NavigationService.Navigate(new Uri("/About.xaml", UriKind.RelativeOrAbsolute));
        }

        private void MessagesMenuItem_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/MessageView.xaml", UriKind.RelativeOrAbsolute));
        }

        private void TaskViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (TaskListBox.ItemsSource == ((ObservableCollection<MyTask>)settings["DoneTaskList"]))
            {
                TaskListBox.ItemsSource = null;
                TaskListBox.ItemsSource = ((ObservableCollection<MyTask>)settings["TaskList"]);
                TaskViewButton.Content = "View Completed Tasks";
            }
            else
            {
                TaskListBox.ItemsSource = null; //Refresh ListBox
                TaskListBox.ItemsSource = ((ObservableCollection<MyTask>)settings["DoneTaskList"]);
                NoTasksTB.Visibility = System.Windows.Visibility.Collapsed;
                AddNewTaskTB.Visibility = System.Windows.Visibility.Collapsed;
                TaskListBox.Visibility = System.Windows.Visibility.Visible;
                TaskListBox.IsEnabled = true;
            }
            SetTaskSource();
        }

        private void ProjViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectListBox.ItemsSource == ((ObservableCollection<MyProject>)settings["DoneProjectList"]))
            {
                ProjectListBox.ItemsSource = null;
                ProjectListBox.ItemsSource = ((ObservableCollection<MyProject>)settings["ProjectList"]);
                ProjViewButton.Content = "View Completed Projects";
            }
            else
            {
                ProjectListBox.ItemsSource = null; //Refresh ListBox
                ProjectListBox.ItemsSource = ((ObservableCollection<MyProject>)settings["DoneProjectList"]);
                NoProjectsTB.Visibility = System.Windows.Visibility.Collapsed;
                AddNewProjectTB.Visibility = System.Windows.Visibility.Collapsed;
                ProjectListBox.Visibility = System.Windows.Visibility.Visible;
                ProjectListBox.IsEnabled = true;
            }
            SetProjectSource();
        }

        private void ContactsViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (ContactsListBox.ItemsSource == ((ObservableCollection<Person>)settings["RequestList"]))
            {
                ContactsListBox.ItemsSource = null;
                ContactsListBox.ItemsSource = ((ObservableCollection<Person>)settings["ContactsList"]);
                ContactsViewButton.Content = "View Requests";
            }
            else
            {
                ContactsListBox.ItemsSource = null; //Refresh ListBox
                ContactsListBox.ItemsSource = ((ObservableCollection<Person>)settings["RequestList"]);
                NoContactsTB.Visibility = System.Windows.Visibility.Collapsed;
                AddNewContactsTB.Visibility = System.Windows.Visibility.Collapsed;
                ContactsListBox.Visibility = System.Windows.Visibility.Visible;
                ContactsListBox.IsEnabled = true;
            }
            SetContactsSource();
        }

        #endregion

        #region Item Selected

        MyTask _selectedTask = null;
        private void TaskListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) //Called when a task in the TLB is selected
        {
            //System.Diagnostics.Debug.WriteLine("Hit TLBSelectionChanged: " + TaskListBox.SelectedIndex);
            if (TaskListBox.SelectedIndex == -1) //-1 is the default position
                return;
            _selectedTask = (MyTask)TaskListBox.SelectedItem;
            //System.Diagnostics.Debug.WriteLine(_selectedTask.TaskKey);
            if (TaskListBox.ItemsSource == ((ObservableCollection<MyTask>)settings["DoneTaskList"]))
            {
                NavigationService.Navigate(new Uri("/TaskView.xaml?TaskKey=" + ((ObservableCollection<MyTask>)settings["DoneTaskList"]).IndexOf(_selectedTask)
                    +"&Complete=1", UriKind.RelativeOrAbsolute)); //Navigate to TaskView
            }
            else
            {
                NavigationService.Navigate(new Uri("/TaskView.xaml?TaskKey=" + ((ObservableCollection<MyTask>)settings["TaskList"]).IndexOf(_selectedTask), UriKind.RelativeOrAbsolute)); //Navigate to TaskView
            }
            TaskListBox.SelectedIndex = -1;
        }

        MyProject _selectedProject = null;
        private void ProjectListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)//Called when a project in the PLB is selected
        {
            System.Diagnostics.Debug.WriteLine("Hit PLBSelectionChanged: " + ProjectListBox.SelectedIndex);
            if (ProjectListBox.SelectedIndex == -1)
                return;
            _selectedProject = (MyProject)ProjectListBox.SelectedItem;
            if (ProjectListBox.ItemsSource == ((ObservableCollection<MyProject>)settings["DoneProjectList"]))
            {
                NavigationService.Navigate(new Uri("/ProjectView.xaml?Project=" + ((ObservableCollection<MyProject>)settings["DoneProjectList"]).IndexOf(_selectedProject)
                    + "&Complete=1", UriKind.RelativeOrAbsolute)); //Navigate to ProjectView
            }
            else
            {
                NavigationService.Navigate(new Uri("/ProjectView.xaml?Project=" + ((ObservableCollection<MyProject>)settings["ProjectList"]).IndexOf(_selectedProject), UriKind.RelativeOrAbsolute)); //Navigate to ProjectView
            }
            ProjectListBox.SelectedIndex = -1;
        }

        Person _selectedPerson = null;
        private void ContactsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Hit CLBSelectionChanged: " + ContactsListBox.SelectedIndex);
            if (ContactsListBox.SelectedIndex == -1)
                return;
            _selectedPerson = (Person)ContactsListBox.SelectedItem;
            if (ContactsListBox.ItemsSource == ((ObservableCollection<Person>)settings["RequestList"]) && _selectedPerson.reqSent == true)
            {
                NavigationService.Navigate(new Uri("/ContactView.xaml?ContactKey=" + ((ObservableCollection<Person>)settings["RequestList"]).IndexOf(_selectedPerson)
                    + "&Request=1", UriKind.RelativeOrAbsolute));
            }
            else if (ContactsListBox.ItemsSource == ((ObservableCollection<Person>)settings["RequestList"]) && _selectedPerson.reqSent == false)
            {
                NavigationService.Navigate(new Uri("/NewContact.xaml?Accept=" + _selectedPerson.username + "&ID=" + _selectedPerson.hawaiiID, UriKind.RelativeOrAbsolute));
            }
            else
            {
                NavigationService.Navigate(new Uri("/ContactView.xaml?ContactKey=" + ((ObservableCollection<Person>)settings["ContactsList"]).IndexOf(_selectedPerson), UriKind.RelativeOrAbsolute));
            }
            ContactsListBox.SelectedIndex = -1;
        }

        #endregion

        #region Sort Code

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

        #endregion
    }
}