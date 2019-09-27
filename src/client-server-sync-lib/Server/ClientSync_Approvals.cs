// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UpdateServices.Storage;
using System.Linq;
using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.WebServices.ClientSync;
using System.IO;
using Microsoft.UpdateServices.ClientSync.DataModel;
using System.Threading;
using System.ServiceModel;

namespace Microsoft.UpdateServices.ClientSync.Server
{

    public partial class ClientSyncWebService
    {
        private HashSet<Identity> ApprovedSoftwareUpdates;
        private HashSet<Identity> ApprovedDriverUpdates;

        /// <summary>
        /// Delegate method called to report updates applicable to a client but which are not approved and thus not offered
        /// </summary>
        /// <param name="requestedUnapprovedUpdates"></param>
        public delegate void UnApprovedUpdatesRequestedDelegate(IEnumerable<Update> requestedUnapprovedUpdates);

        /// <summary>
        /// Event raised when software updates are applicable to a client but are not approved for distribution
        /// </summary>
        public event UnApprovedUpdatesRequestedDelegate OnUnApprovedSoftwareUpdatesRequested;

        /// <summary>
        /// Event raised when driver updates are applicable to a client but are not approved for distribution
        /// </summary>
        public event UnApprovedUpdatesRequestedDelegate OnUnApprovedDriverUpdatesRequested;

        /// <summary>
        /// Adds an update identity to the list of approved software updates.
        /// Approved updates are made available to clients connecting to this service.
        /// </summary>
        /// <param name="approvedUpdate">Approved update</param>
        public void AddApprovedSoftwareUpdate(Identity approvedUpdate)
        {
            ApprovedSoftwareUpdates.Add(approvedUpdate);
        }

        /// <summary>
        /// Adds a list of update identities to the list of approved software updates.
        /// Approved updates are made available to clients connecting to this service.
        /// </summary>
        /// <param name="approvedUpdates">List of approved updates</param>
        public void AddApprovedSoftwareUpdates(IEnumerable<Identity> approvedUpdates)
        {
            foreach (var approvedUpdate in approvedUpdates)
            {
                ApprovedSoftwareUpdates.Add(approvedUpdate);
            }
        }

        /// <summary>
        /// Adds an update identities to the list of approved driver updates.
        /// Approved updates are made available to clients connecting to this service.
        /// </summary>
        /// <param name="approvedUpdate">Approved driver update</param>
        public void AddApprovedDriverUpdate(Identity approvedUpdate)
        {
            ApprovedDriverUpdates.Add(approvedUpdate);
        }

        /// <summary>
        /// Adds a list of update identities to the list of approved driver updates.
        /// Approved updates are made available to clients connecting to this service.
        /// </summary>
        /// <param name="approvedUpdates"></param>
        public void AddApprovedDriverUpdates(IEnumerable<Identity> approvedUpdates)
        {
            foreach (var approvedUpdate in approvedUpdates)
            {
                ApprovedDriverUpdates.Add(approvedUpdate);
            }
        }

        /// <summary>
        /// Removes an approved software update from the list of approved software updates.
        /// The software update will not be given to connecting clients anymore.
        /// </summary>
        /// <param name="updateIdentity">Identity of update to un-approve</param>
        public void RemoveApprovedSoftwareUpdate(Identity updateIdentity)
        {
            ApprovedSoftwareUpdates.Remove(updateIdentity);
        }

        /// <summary>
        /// Removes an approved software update from the list of approved software updates.
        /// The software update will not be given to connecting clients anymore.
        /// </summary>
        /// <param name="updateIdentity">Identity of update to un-approve</param>
        public void RemoveApprovedDriverUpdate(Identity updateIdentity)
        {
            ApprovedDriverUpdates.Remove(updateIdentity);
        }

        /// <summary>
        /// Clears the list of approved driver updates.
        /// Un-approved updates are not made available to connecting clients.
        /// </summary>
        public void ClearApprovedDriverUpdates()
        {
            ApprovedDriverUpdates.Clear();
        }

        /// <summary>
        /// Clears the list of approved software updates.
        /// Un-approved updates are not made available to connecting clients.
        /// </summary>
        public void ClearApprovedSoftwareUpdates()
        {
            ApprovedSoftwareUpdates.Clear();
        }
    }
}
