using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace TaskMe
{
    public class TaskMeMessage
    {
        public string type { get; set; }
        public string content { get; set; }
        public DateTime timeStamp { get; set; }
    }
}
