using System.ComponentModel;
using Microsoft.Hawaii.Rendezvous.Client;

namespace TaskMe
{
    /// <summary>
    /// Class used for data binding.
    /// </summary>
    public class NameRegistrationContext : INotifyPropertyChanged
    {
        private string name;
        private string registrationId;

        /// <summary>
        /// Initializes a new instance of the NameRegistrationContext class.
        /// </summary>
        public NameRegistrationContext()
        {
            this.Name = string.Empty;
            this.RegistrationId = string.Empty;
            this.SecretKey = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the NameRegistrationContext class.
        /// </summary>
        /// <param name="nameRegistration">Specifies a name registration object.</param>
        public NameRegistrationContext(NameRegistration nameRegistration)
        {
            this.Name = nameRegistration.Name;
            this.RegistrationId = nameRegistration.Id;
            this.SecretKey = nameRegistration.SecretKey;
        }

        /// <summary>
        /// Property Changed Event Handler.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets a name of name registration.
        /// </summary>
        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(this.name))
                {
                    return "No name";
                }

                return this.name;
            }

            set
            {
                if (this.name != value)
                {
                    this.name = value;
                    this.NotifyPropertyChanged("Name");
                }
            }
        }

        /// <summary>
        /// Gets or sets a RegistrationId of name registration.
        /// </summary>
        public string RegistrationId
        {
            get
            {
                if (
                    string.IsNullOrEmpty(this.registrationId))
                {
                    return "No registration id";
                }

                return this.registrationId;
            }

            set
            {
                if (this.registrationId != value)
                {
                    this.registrationId = value;
                    this.NotifyPropertyChanged("RegistrationId");
                }
            }
        }

        /// <summary>
        /// Gets or sets a secret key.
        /// </summary>
        public string SecretKey { get; set; }

        /// <summary>
        /// Method sets empty values for all fields.
        /// </summary>
        public void SetEmpty()
        {
            this.Name = string.Empty;
            this.RegistrationId = string.Empty;
            this.SecretKey = string.Empty;
        }

        /// <summary>
        /// Methid converts object of this class to NameRegistration object.
        /// </summary>
        /// <returns>NameRegistration object.</returns>
        public NameRegistration ToNameRegistration()
        {
            return new NameRegistration()
            {
                Name = this.Name,
                Id = this.RegistrationId,
                SecretKey = this.SecretKey
            };
        }

        /// <summary>
        /// Notification handler for the MainViewModel class.
        /// </summary>
        /// <param name="propertyName">Property name.</param>
        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}

