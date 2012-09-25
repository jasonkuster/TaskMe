using System;
using System.Collections.Generic;
using Microsoft.Hawaii.Relay.Client;

namespace TaskMe
{
    /// <summary>
    /// A Container class for all pages.
    /// </summary>
    public class RelayContext
    {
        /// <summary>
        /// Initializes a new instance of the RelayContext class.
        /// </summary>
        public RelayContext()
        {
            this.Endpoint = null;
            this.Groups = new Groups();
            this.Messages = new List<Message>();
        }

        /// <summary>
        /// Gets or sets Endpoint.
        /// </summary>
        public Endpoint Endpoint { get; set; }

        /// <summary>
        /// Gets or sets available (created) groups.
        /// </summary>
        public Groups Groups { get; set; }

        /// <summary>
        /// Gets or sets the messages.
        /// </summary>
        public List<Message> Messages { get; set; }

        /// <summary>
        /// Method creates a relay context from storage.
        /// </summary>
        /// <returns>A valid RelayContext object.</returns>
        public static RelayContext CreateObject()
        {
            RelayContext dataContainer = new RelayContext();
            dataContainer.ReadFromStorage();
            return dataContainer;
        }

        /// <summary>
        /// Save the data into the storage.
        /// </summary>
        public void SaveToStorage()
        {
            try
            {
                RelayStorage.SaveEndpoint(this.Endpoint);
                RelayStorage.SaveGroups(this.Groups);
                RelayStorage.SaveMessages(this.Messages);
            }
            catch (Exception)
            {
                // Let's not to crash the client.
            }
        }

        /// <summary>
        /// Read the data from the storage.
        /// </summary>
        public void ReadFromStorage()
        {
            try
            {
                this.Endpoint = RelayStorage.ReadEndpoint();
                this.Groups = RelayStorage.ReadGroups();
                this.Messages = RelayStorage.ReadMessages();
            }
            catch (Exception)
            {
                // Let's not to crash the client.
            }
        }
    }
}
