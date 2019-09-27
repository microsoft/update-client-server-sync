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
        /// <summary>
        /// Handle software sync request from a client
        /// </summary>
        /// <param name="parameters">Sync parameters</param>
        /// <returns></returns>
        private Task<SyncInfo> DoSoftwareUpdateSync(SyncUpdateParameters parameters)
        {
            MetadataSourceLock.EnterReadLock();

            if (MetadataSource == null)
            {
                throw new FaultException();
            }

            // Get list of installed non leaf updates; these are prerequisites that the client has installed.
            // This list is used to check what updates are applicable to the client
            // We will not send updates that already appear on this list
            var installedNonLeafUpdatesGuids = GetInstalledNotLeafGuidsFromSyncParameters(parameters);

            // Other known updates to the client; we will not send any updates that are on this list
            var otherCachedUpdatesGuids = GetOtherCachedUpdateGuidsFromSyncParameters(parameters);

            // Initialize the response
            var response = new SyncInfo()
            {
                NewCookie = new Cookie() { Expiration = DateTime.Now.AddDays(5), EncryptedData = new byte[12] },
                DriverSyncNotNeeded = "false"
            };

            // Add root updates first; if any new root updates were added, return the response to the client immediatelly
            AddMissingRootUpdatesToSyncUpdatesResponse(installedNonLeafUpdatesGuids, otherCachedUpdatesGuids, response, out bool rootUpdatesAdded);
            if (!rootUpdatesAdded)
            {
                // No root updates were added; add non-leaf updates now
                AddMissingNonLeafUpdatesToSyncUpdatesResponse(installedNonLeafUpdatesGuids, otherCachedUpdatesGuids, response, out bool nonLeafUpdatesAdded);
                if (!nonLeafUpdatesAdded)
                {
                    // No leaf updates were added; add leaf bundle updates now
                    AddMissingBundleUpdatesToSyncUpdatesResponse(installedNonLeafUpdatesGuids, otherCachedUpdatesGuids, response, out bool bundleUpdatesAdded);
                    if (!bundleUpdatesAdded)
                    {
                        // No bundles were added; finally add leaf software updates
                        AddMissingSoftwareUpdatesToSyncUpdatesResponse(installedNonLeafUpdatesGuids, otherCachedUpdatesGuids, response, out bool softwareUpdatesAdded);
                    }
                }
            }

            MetadataSourceLock.ExitReadLock();

            return Task.FromResult(response);
        }

        /// <summary>
        /// For a client request, gathers applicable root updates (detectoids, categories, etc.) that the client does not have yet
        /// </summary>
        /// <param name="installedNonLeaf">List of non leaf updates installed on the client</param>
        /// <param name="otherCached">List of other updates known to the client</param>
        /// <param name="response">The response  to append new updates to</param>
        /// <param name="updatesAdded">On return: true of updates were added to the response, false otherwise</param>
        private void AddMissingRootUpdatesToSyncUpdatesResponse(List<Guid> installedNonLeaf, List<Guid> otherCached, SyncInfo response, out bool updatesAdded)
        {
            var missingRootUpdates = RootUpdates
                .Except(installedNonLeaf)                               // Do not resend installed updates
                .Except(otherCached)                                    // Do not resend other client known updates
                .Where(guid => IdToFullIdentityMap.ContainsKey(guid))   
                .Select(guid => IdToFullIdentityMap[guid])              // Map the GUID to a fully qualified identity
                .Select(id => MetadataSource.CategoriesIndex[id])       // Get the update (category) by identity
                .Where(u => !u.IsSuperseded)                            // Remove superseded updates
                .Take(MaxUpdatesInResponse + 1)                         // Only take the maximum number of updates allowed + 1 (to see if we truncated)
                .ToList();

            if (missingRootUpdates.Count > 0)
            {
                response.NewUpdates = CreateUpdateInfoListFromNonLeafUpdates(missingRootUpdates).ToArray();
                response.Truncated = true;
                updatesAdded = true;
            }
            else
            {
                updatesAdded = false;
            }
        }

        /// <summary>
        /// For a client request, gathers applicable software updates that are not leafs in the prerequisite tree; 
        /// </summary>
        /// <param name="installedNonLeaf">List of non leaf updates installed on the client</param>
        /// <param name="otherCached">List of other updates known to the client</param>
        /// <param name="response">The response  to append new updates to</param>
        /// <param name="updatesAdded">On return: true of updates were added to the response, false otherwise</param>
        private void AddMissingNonLeafUpdatesToSyncUpdatesResponse(List<Guid> installedNonLeaf, List<Guid> otherCached, SyncInfo response, out bool updatesAdded)
        {
            var missingNonLeafs = NonLeafUpdates
                    .Except(installedNonLeaf)                   // Do not resend installed updates
                    .Except(otherCached)                        // Do not resend other client known updates
                    .Where(guid => IdToFullIdentityMap.ContainsKey(guid))
                    .Select(guid => IdToFullIdentityMap[guid])  // Map the GUID to a fully qualified identity
                    // Non leaf updates can be either a category or regular update
                    .Select(id => MetadataSource.CategoriesIndex.ContainsKey(id) ? MetadataSource.CategoriesIndex[id] : MetadataSource.UpdatesIndex[id])
                    .Where(u => !u.IsSuperseded && u.IsApplicable(installedNonLeaf))    // Eliminate superseded and not applicable updates
                    .Take(MaxUpdatesInResponse + 1)             // Only take the maximum number of updates allowed + 1 (to see if we truncated)
                    .ToList();

            if (missingNonLeafs.Count > 0)
            {
                response.NewUpdates = CreateUpdateInfoListFromNonLeafUpdates(missingNonLeafs).ToArray();
                response.Truncated = true;
                updatesAdded = true;
            }
            else
            {
                updatesAdded = false;
            }
        }

        /// <summary>
        /// For a client request, gathers applicable leaf bundle updates that the client does not have yet
        /// </summary>
        /// <param name="installedNonLeaf">List of non leaf updates installed on the client</param>
        /// <param name="otherCached">List of other updates known to the client</param>
        /// <param name="response">The response  to append new updates to</param>
        /// <param name="updatesAdded">On return: true of updates were added to the response, false otherwise</param>
        private void AddMissingBundleUpdatesToSyncUpdatesResponse(List<Guid> installedNonLeaf, List<Guid> otherCached, SyncInfo response, out bool updatesAdded)
        {
            var allMissingBundles = SoftwareLeafUpdateGuids
                .Except(installedNonLeaf)                               // Do not resend installed updates
                .Except(otherCached)                                    // Do not resend other client known updates
                .Where(guid => IdToFullIdentityMap.ContainsKey(guid))
                .Select(guid => IdToFullIdentityMap[guid])              // Map the GUID to a fully qualified identity
                .Select(id => MetadataSource.UpdatesIndex[id])          // Select the software update by identity
                .OfType<SoftwareUpdate>()
                .Where(u => !u.IsSuperseded && u.IsApplicable(installedNonLeaf) && u.IsBundle);  // Remove superseded, not applicable and not bundles

            var unapprovedMissingBundles = allMissingBundles.Where(u => !ApprovedSoftwareUpdates.Contains(u.Identity));
            if (unapprovedMissingBundles.Count() > 0)
            {
                OnUnApprovedSoftwareUpdatesRequested?.Invoke(unapprovedMissingBundles);
            }

            var approvedMissingBundles = allMissingBundles
                .Where(u => ApprovedSoftwareUpdates.Contains(u.Identity))
                .Take(MaxUpdatesInResponse + 1)   // Only take the maximum number of updates allowed + 1 (to see if we truncated)
                .ToList();

            if (approvedMissingBundles.Count > 0)
            {
                response.NewUpdates = CreateUpdateInfoListFromSoftwareUpdate(approvedMissingBundles).ToArray();
                response.Truncated = true;
                updatesAdded = true;
            }
            else
            {
                updatesAdded = false;
            }
        }

        /// <summary>
        /// For a client sync request, gathers applicable software updates that the client does not have yet
        /// </summary>
        /// <param name="installedNonLeaf">List of non leaf updates installed on the client</param>
        /// <param name="otherCached">List of other updates known to the client</param>
        /// <param name="response">The response  to append new updates to</param>
        /// <param name="updatesAdded">On return: true of updates were added to the response, false otherwise</param>
        private void AddMissingSoftwareUpdatesToSyncUpdatesResponse(List<Guid> installedNonLeaf, List<Guid> otherCached, SyncInfo response, out bool updatesAdded)
        {
            var allMissingApplicableUpdates = SoftwareLeafUpdateGuids
                .Except(installedNonLeaf)                               // Do not resend installed updates
                .Except(otherCached)                                    // Do not resend other client known updates
                .Select(guid => IdToFullIdentityMap[guid])              // Map the GUID to a fully qualified identity
                .Select(id => MetadataSource.UpdatesIndex[id])          // Select the software update by identity
                .OfType<SoftwareUpdate>()
                .Where(u => !u.IsSuperseded && u.IsApplicable(installedNonLeaf) && !u.IsBundle); // Remove superseded, not applicable and bundles

            var unapprovedMissingUpdates = allMissingApplicableUpdates
                .Where(u => !ApprovedSoftwareUpdates.Contains(u.Identity) && (!u.IsBundled || !u.BundleParent.Any(i => ApprovedSoftwareUpdates.Contains(i))));

            if (unapprovedMissingUpdates.Count() > 0)
            {
                OnUnApprovedSoftwareUpdatesRequested?.Invoke(unapprovedMissingUpdates);
            }

            var missingApplicableUpdates = allMissingApplicableUpdates
                .Where(u => ApprovedSoftwareUpdates.Contains(u.Identity) || (u.IsBundled && u.BundleParent.Any(i => ApprovedSoftwareUpdates.Contains(i))))    // The update is approved or it's part of a bundle that is approved
                .Take(MaxUpdatesInResponse + 1)                         // Only take the maximum number of updates allowed + 1 (to see if we truncated)
                .ToList();

            response.Truncated = missingApplicableUpdates.Count > MaxUpdatesInResponse;

            if (missingApplicableUpdates.Count > 0)
            {
                response.NewUpdates = CreateUpdateInfoListFromSoftwareUpdate(missingApplicableUpdates).ToArray();
                updatesAdded = true;
            }
            else
            {
                updatesAdded = false;
            }
        }

        /// <summary>
        /// Creates a list of updates to be sent to the client, based on the specified list of software updates.
        /// The update information sent to the client contains a deployment field and a core XML fragment extracted
        /// from the full metadata of the update
        /// </summary>
        /// <param name="softwareUpdates">List of software updates to send to the client</param>
        /// <returns>List of updates that can be appended to a SyncUpdates SOAP response to a client</returns>
        private List<UpdateInfo> CreateUpdateInfoListFromSoftwareUpdate(List<SoftwareUpdate> softwareUpdates)
        {
            var returnListLength = Math.Min(MaxUpdatesInResponse, softwareUpdates.Count);
            var returnList = new List<UpdateInfo>(returnListLength);

            for (int i = 0; i < returnListLength; i++)
            {
                // Get the update index; it will be sent to the client
                var revision = IdToRevisionMap[softwareUpdates[i].Identity.ID];

                // Generate the core XML fragment
                var identity = softwareUpdates[i].Identity;
                var coreXml = GetCoreFragment(identity);

                // Add the update information to the return array
                returnList.Add(new UpdateInfo()
                {
                    Deployment = new Deployment()
                    {
                        // Action is Install for bundles of updates that are not part of a bundle
                        // Action is Bundle for updates that are part of a bundle
                        Action = (softwareUpdates[i].IsBundle || !softwareUpdates[i].IsBundled) ? DeploymentAction.Install : DeploymentAction.Bundle,
                        ID = softwareUpdates[i].IsBundle ? 20000 : (softwareUpdates[i].IsBundled ? 20001 : 20002),
                        AutoDownload = "0",
                        AutoSelect = "0",
                        SupersedenceBehavior = "0",
                        IsAssigned = true,
                        LastChangeTime = "2019-08-06"
                    },
                    IsLeaf = true,
                    ID = revision,
                    IsShared = false,
                    Verification = null,
                    Xml = coreXml
                });
            }

            return returnList;
        }

        /// <summary>
        /// Creates a list of updates to be sent to the client, based on the specified list of category updates.
        /// The update information sent to the client contains a deployment field and a core XML fragment extracted
        /// from the full metadata of the update
        /// </summary>
        /// <param name="nonLeafUpdates">List of non-software updates to send to the client. These are usually detectoids, categories and classifications</param>
        /// <returns>List of updates that can be appended to a SyncUpdates SOAP response to a client</returns>
        private List<UpdateInfo> CreateUpdateInfoListFromNonLeafUpdates(List<Update> nonLeafUpdates)
        {
            var returnListLength = Math.Min(MaxUpdatesInResponse, nonLeafUpdates.Count);
            var returnList = new List<UpdateInfo>(returnListLength);

            for (int i = 0; i < returnListLength; i++)
            {
                var revision = IdToRevisionMap[nonLeafUpdates[i].Identity.ID];

                var identity = nonLeafUpdates[i].Identity;

                // Generate the core XML fragment
                var coreXml = GetCoreFragment(identity);

                // Add the update information to the return array
                returnList.Add(new UpdateInfo()
                {
                    Deployment = new Deployment()
                    {
                        Action = DeploymentAction.Evaluate,
                        ID = 15000,
                        AutoDownload = "0",
                        AutoSelect = "0",
                        SupersedenceBehavior = "0",
                        IsAssigned = true,
                        LastChangeTime = "2019-08-06"
                    },
                    IsLeaf = false,
                    ID = revision,
                    IsShared = false,
                    Verification = null,
                    Xml = coreXml
                });
            }

            return returnList;
        }
    }
}
