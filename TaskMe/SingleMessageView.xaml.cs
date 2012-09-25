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
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;

namespace TaskMe
{
    public partial class SingleMessageView : PhoneApplicationPage
    {
        IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
        string selectedMessageString = "";

        public SingleMessageView()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (NavigationContext.QueryString.TryGetValue("NMKey", out selectedMessageString))
            {
                this.DataContext = ((ObservableCollection<TaskMeMessage>)settings["MessageList"])[int.Parse(selectedMessageString)];
            }

            if (NavigationContext.QueryString.TryGetValue("AMKey", out selectedMessageString))
            {
                this.DataContext = ((ObservableCollection<TaskMeMessage>)settings["AllMessageList"])[int.Parse(selectedMessageString)];
            }
            
            base.OnNavigatedTo(e);
        }
    }
}