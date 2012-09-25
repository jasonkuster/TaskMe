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
using System.Collections.ObjectModel;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace TaskMe
{
    public class MyProject
    {
        public string name { get; set; }
        public string projKey { get; set; }
        public string description { get; set; }
        public DateTime dueDate { get; set; }
        public bool isComplete { get; set; }
        public string status { get; set; }
        public Person creator { get; set; }
        public string recipients { get; set; }
        public ObservableCollection<MyTask> tasks { get; set; }
        public ObservableCollection<MyTask> doneTasks { get; set; }

        public void addRecipient(string name)
        {
            bool isPresent = false;
            if (string.IsNullOrEmpty(recipients))
            {
                recipients = name;
                return;
            }
            foreach (string find in recipients.Split(','))
            {
                if (find == name)
                {
                    isPresent = true;
                    break;
                }
            }
            if (!isPresent)
            {
                recipients += ("," + name);
            }
        }

        public void removeRecipient(string name)
        {
            if (!string.IsNullOrEmpty(recipients))
            {
                string[] recipArray = recipients.Split(',');
                List<string> removeFrom = new List<string>();
                foreach (string s in recipArray)
                    removeFrom.Add(s);
                removeFrom.Remove(name);
                recipients = string.Join(",", removeFrom);
            }
        }

        public byte[] Serialize()
        {
            return Encoding.Unicode.GetBytes("Project" + '\0' + name + '\0' + projKey + '\0' + description + '\0'
                + dueDate.ToString() + '\0' + isComplete.ToString() + '\0' + status + '\0' + creator.ToString() + '\0' + recipients);
            //using (MemoryStream m = new MemoryStream())
            //{
            //    using (BinaryWriter writer = new BinaryWriter(m))
            //    {
            //        writer.Write("Project");
            //        writer.Write('\0');
            //        writer.Write(name);
            //        writer.Write('\0');
            //        writer.Write(projKey);
            //        writer.Write('\0');
            //        writer.Write(description);
            //        writer.Write('\0');
            //        writer.Write(dueDate.ToString());
            //        writer.Write('\0');
            //        writer.Write(isComplete.ToString());
            //        writer.Write('\0');
            //        writer.Write(status);
            //        writer.Write('\0');
            //        writer.Write(creator.ToString());
            //        writer.Write('\0');
            //        writer.Write(recipients);
            //    }
            //    return m.ToArray();
            //}
        }

        public int CompareTo(object obj)
        {
            MyProject task = obj as MyProject;
            if (task == null)
            {
                throw new ArgumentException("Object is not MyProject");
            }
            if (this.dueDate.Equals(task.dueDate))
                return this.name.CompareTo(task.name);
            return this.dueDate.CompareTo(task.dueDate);
        }
    }
}
