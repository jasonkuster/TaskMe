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
using System.IO;
using System.Text;

namespace TaskMe
{
    public class MyTask : IComparable
    {
        public string projKey { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string taskKey { get; set; }
        public DateTime dueDate { get; set; }
        public DateTime remindDate { get; set; }
        public bool isComplete { get; set; }
        public string status { get; set; }
        public string project { get; set; }
        public Person assignedTo { get; set; }

        public byte[] Serialize()
        {
            if (!isComplete)
                return Encoding.Unicode.GetBytes("Task" + '\0' + projKey + '\0' + name + '\0' + description + '\0' + taskKey + '\0' 
                    + dueDate.ToString() + '\0' + remindDate.ToString() + '\0' + isComplete.ToString() + '\0' + status + '\0' + project + '\0' + assignedTo.ToString());
            else
                return Encoding.Unicode.GetBytes("DoneTask" + '\0' + projKey + '\0' + name + '\0' + description + '\0' + taskKey + '\0'
                    + dueDate.ToString() + '\0' + remindDate.ToString() + '\0' + isComplete.ToString() + '\0' + status + '\0' + project + '\0' + assignedTo.ToString());
            //using (MemoryStream m = new MemoryStream())
            //{
            //    using (BinaryWriter writer = new BinaryWriter(m))
            //    {
            //        writer.Write(projKey);
            //        writer.Write('\0');
            //        writer.Write(name);
            //        writer.Write('\0');
            //        writer.Write(description);
            //        writer.Write('\0');
            //        writer.Write(taskKey);
            //        writer.Write('\0');
            //        writer.Write(dueDate.ToString());
            //        writer.Write('\0');
            //        writer.Write(remindDate.ToString());
            //        writer.Write('\0');
            //        writer.Write(isComplete.ToString());
            //        writer.Write('\0');
            //        writer.Write(status);
            //        writer.Write('\0');
            //        writer.Write(project);
            //        writer.Write('\0');
            //        writer.Write(assignedTo.ToString());
            //    }
            //    return m.ToArray();
            //}
        }

        public int CompareTo(object obj)
        {
            MyTask task = obj as MyTask;
            if (task == null)
            {
                throw new ArgumentException("Object is not MyTask");
            }
            if (this.dueDate.Equals(task.dueDate))
                return this.name.CompareTo(task.name);
            return this.dueDate.CompareTo(task.dueDate);
        }
    }
}
