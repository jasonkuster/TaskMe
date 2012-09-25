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

namespace TaskMe
{
    public partial class MessageView : PhoneApplicationPage
    {
        IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

        public MessageView()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (((ObservableCollection<TaskMeMessage>)settings["MessageList"]).Count != 0)
            {
                NoNewMessagesTextBlock.Visibility = System.Windows.Visibility.Collapsed;
                NewMessagesListBox.ItemsSource = ((ObservableCollection<TaskMeMessage>)settings["MessageList"]);
            }
            else
                NoNewMessagesTextBlock.Visibility = System.Windows.Visibility.Visible;

            if (((ObservableCollection<TaskMeMessage>)settings["AllMessageList"]).Count != 0)
            {
                NoMessagesTextBlock.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
                NoMessagesTextBlock.Visibility = System.Windows.Visibility.Visible;

            this.Dispatcher.BeginInvoke(
                    delegate
                    {
                        int insAt = 0;
                        foreach (TaskMeMessage message in ((ObservableCollection<TaskMeMessage>)settings["MessageList"]))
                        {
                            ((ObservableCollection<TaskMeMessage>)settings["AllMessageList"]).Insert(insAt++, message);
                        }
                        AllMessagesListBox.ItemsSource = null;
                        AllMessagesListBox.ItemsSource = ((ObservableCollection<TaskMeMessage>)settings["AllMessageList"]);
                    });

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (e.Uri.OriginalString.Contains("MainPage.xaml"))
            {
                ((ObservableCollection<TaskMeMessage>)settings["MessageList"]).Clear();
            }
            base.OnNavigatedFrom(e);
        }

        TaskMeMessage _selectedMessage = null;
        private void AllMessagesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AllMessagesListBox.SelectedIndex == -1) //-1 is the default position
                return;
            _selectedMessage = (TaskMeMessage)AllMessagesListBox.SelectedItem;
            NavigationService.Navigate(new Uri("/SingleMessageView.xaml?AMKey=" + ((ObservableCollection<TaskMeMessage>)settings["AllMessageList"]).IndexOf(_selectedMessage), UriKind.RelativeOrAbsolute)); //Navigate to TaskView

            NewMessagesListBox.SelectedIndex = -1;
        }

        private void NewMessagesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NewMessagesListBox.SelectedIndex == -1) //-1 is the default position
                return;
            _selectedMessage = (TaskMeMessage)NewMessagesListBox.SelectedItem;
            NavigationService.Navigate(new Uri("/SingleMessageView.xaml?NMKey=" + ((ObservableCollection<TaskMeMessage>)settings["MessageList"]).IndexOf(_selectedMessage), UriKind.RelativeOrAbsolute)); //Navigate to TaskView

            NewMessagesListBox.SelectedIndex = -1;
        }
    }
}