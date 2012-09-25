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
    public class Person
    {
        public string name { get; set; }
        public string username { get; set; }
        public string hawaiiID { get; set; }
        public bool reqSent { get; set; }
        public bool accepted { get; set; }

        public override string ToString()
        {
            return (name + '\0' + username + '\0' + hawaiiID + '\0' + reqSent.ToString() + '\0' + accepted.ToString());
        }
    }
}
