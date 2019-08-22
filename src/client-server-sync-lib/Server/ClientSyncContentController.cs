// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using Microsoft.UpdateServices.Metadata.Content;
using Microsoft.UpdateServices.Storage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.UpdateServices.ClientSync.Server
{
    /// <summary>
    /// MVC controller for handling update content requests from Windows clients
    /// </summary>
    /// <example>
    /// <para>Attach this service to your ASP.NET service:</para>
    /// <code>
    /// public void ConfigureServices(IServiceCollection services)
    /// {
    ///    var localMetadataSource = CompressedMetadataStore.Open(sourcePath);
    ///    var contentSource = new FileSystemContentStore(contentPath);
    ///    services.TryAddSingleton&lt;ClientSyncContentController&gt;(
    ///        new ClientSyncContentController(localMetadataSource, contentSource));
    ///    //
    ///    // Add ClientSyncContentController from its containing assembly
    ///    services
    ///        .AddMvc()
    ///        .AddApplicationPart(
    ///            Assembly.Load(
    ///                "Microsoft.UpdateServices.ClientSync.Server.ClientSyncContentController"))
    ///        .AddControllersAsServices();
    ///    ...
    /// }
    /// // Configure routes for the content controller
    /// public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
    /// {
    ///     // Add the content controller to MVC route
    ///     app.UseMvc(routes =&gt;
    ///     {
    ///         routes.MapRoute(
    ///             name: "getContent",
    ///             template: "Content/{directory}/{name}",
    ///             defaults: new 
    ///             {
    ///                 controller = "ClientSyncContent",
    ///                 action = "GetUpdateContent"
    ///             });
    ///     });
    /// ...
    /// }
    /// </code>
    /// </example>
    public class ClientSyncContentController : Controller
    {
        IUpdateContentSource ContentSource;
        Dictionary<string, UpdateFile> UpdateFiles;

        /// <summary>
        /// Create a content controller from the specified metadata and update content sources.
        /// </summary>
        /// <param name="metadataSource">The source of update metadata. Used to build the list of known files to serve.</param>
        /// <param name="contentSource">The source of content. Used to read update content and send it to clients.</param>
        public ClientSyncContentController(IMetadataSource metadataSource, IUpdateContentSource contentSource)
        {
            ContentSource = contentSource;

            var updatesWithFiles = metadataSource.GetUpdates().Where(u => u.HasFiles).ToList();

            UpdateFiles = updatesWithFiles.SelectMany(u => u.Files).GroupBy(f => f.Digests[0].DigestBase64).Select(g => g.First()).ToDictionary(
                f => {
                    // TODO: fix; hack; this is an internal implementation detail; must be exposed from server-server-sync library
                    byte[] hashBytes = Convert.FromBase64String(f.Digests[0].DigestBase64);
                    var cachedContentDirectoryName = string.Format("{0:X}", hashBytes.Last());

                    return $"{cachedContentDirectoryName.ToLower()}/{f.Digests[0].HexString.ToLower()}";
                });
        }

        /// <summary>
        /// Handle HTTP GET requests on the Content/(Directory)/(FileHash) URLs
        /// </summary>
        /// <param name="directory">The directory name for an update file</param>
        /// <param name="name">The file name for an update file</param>
        /// <returns>File content on success, other error codes otherwise</returns>
        [HttpGet("Content/{directory}/{name}", Name = "GetUpdateContent")]
        public IActionResult GetUpdateContent(string directory, string name)
        {
            var lookupKey = $"{directory.ToLower()}/{name.ToLower()}";

            if (UpdateFiles.TryGetValue(lookupKey, out UpdateFile file) &&
                 ContentSource.Contains(file))
            {
                var request = HttpContext.Request;

                var fileResult = new FileStreamResult(ContentSource.Get(file), "application/octet-stream");
                fileResult.FileDownloadName = name;
                fileResult.EnableRangeProcessing = true;
                return fileResult;
            }
            else
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Handle HTTP HEAD requests on the Content/(Directory)/(FileName) URLs
        /// </summary>
        /// <param name="directory">The directory name for an update file</param>
        /// <param name="name">The file name for an update file</param>
        /// <returns>File header on success, other error codes otherwise</returns>
        [HttpHead("Content/{directory}/{name}", Name = "GetUpdateContentHead")]
        public void GetUpdateContentHead(string directory, string name)
        {
            HttpContext.Response.Body = null;

            var lookupKey = $"{directory.ToLower()}/{name.ToLower()}";

            if (UpdateFiles.TryGetValue(lookupKey, out UpdateFile file) &&
                ContentSource.Contains(file))
            {
                var okResult = new OkResult();

                using (var contentStream = ContentSource.Get(file))
                {
                    HttpContext.Response.ContentLength = contentStream.Length;
                }

                HttpContext.Response.Body = null;
                HttpContext.Response.StatusCode = 200;
            }
            else
            {
                HttpContext.Response.StatusCode = 404;
            }
        }
    }
}
