// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using System.ServiceModel;
using SoapCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.UpdateServices.WebServices.ClientSync;
using System.Reflection;

namespace Microsoft.UpdateServices.ClientSync.Server
{
    /// <summary>
    /// Startup class for a ASP.NET Core web service that implements the Client-Server sync protocol.
    /// <para>A web service started with UpdateServerStartup acts as an update server to Windows PCs.</para>
    /// </summary>
    public class UpdateServerStartup
    {
        IMetadataSource MetadataSource;

        Config UpdateServiceConfiguration;

        IUpdateContentSource ContentSource = null;

        string ContentRoot;

        /// <summary>
        /// Initialize a new instance of UpdateServerStartup.
        /// </summary>
        /// <param name="config">
        /// <para>ASP.NET configuration.</para>
        /// <list type="bullet">
        /// <item><description>Required: string entry "metadata-source" with the path to file containing updates metadata.</description></item>
        /// <item><description>Required: string entry "server-config" containing the server configuration in JSON format. A reference server configuration file is provided.</description></item>
        /// <item><description>Optional: string entry "content-source" containing the path where update content is stored.</description></item>
        /// <item><description>Optional: string entry "content-http-root" with the root URL where update content is serverd from (e.g. http://my-update-server.com). Usually this set to the address/name of the server. Required if "content-source" is specified.</description></item>
        /// </list>
        /// </param>
        public UpdateServerStartup(IConfiguration config)
        {
            // Get the repository path from the configuration
            var metadataSourceFile = config.GetValue<string>("metadata-source");
            MetadataSource = CompressedMetadataStore.Open(metadataSourceFile);
            if (MetadataSource == null)
            {
                throw new System.Exception($"Cannot open updates metadata source from path {metadataSourceFile}");
            }

            UpdateServiceConfiguration = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(config.GetValue<string>("server-config"));

            // A file that contains mapping of update identity to a 32 bit, locally assigned revision ID.
            var contentPath = config.GetValue<string>("content-source");
            if (!string.IsNullOrEmpty(contentPath))
            {
                ContentSource = new FileSystemContentStore(contentPath);
                if (ContentSource == null)
                {
                    throw new System.Exception($"Cannot open updates content source from path {contentPath}");
                }
            }

            ContentRoot = config.GetValue<string>("content-http-root");
        }

        /// <summary>
        /// Called by ASP.NET to configure services
        /// </summary>
        /// <param name="services">Service collection.
        /// <para>The client-server sync and simple authentication services are added to this list.</para>
        /// </param>
        public void ConfigureServices(IServiceCollection services)
        {
            // Enable SoapCore; this middleware provides translation services from WCF/SOAP to Asp.net
            services.AddSoapCore();

            // Enable the upstream WCF services
            var clientSyncService = new Server.ClientSyncWebService(UpdateServiceConfiguration, ContentSource == null ? null : ContentRoot);
            clientSyncService.SetMetadataSource(MetadataSource);
            services.TryAddSingleton<ClientSyncWebService>(clientSyncService);
            services.TryAddSingleton<SimpleAuthenticationWebService>();
            services.TryAddSingleton<ReportingWebService>();

            // Enable the content controller if serving content
            if (ContentSource != null)
            {
                services.TryAddSingleton<ClientSyncContentController>(new ClientSyncContentController(MetadataSource, ContentSource));
            }

            // Add ContentController from this assembly
            services.AddMvc().AddApplicationPart(Assembly.GetExecutingAssembly()).AddControllersAsServices();
        }

        /// <summary>
        /// Called by ASP.NET to configure a web app's application pipeline
        /// </summary>
        /// <param name="app">Applicatin to configure.
        /// <para>A SOAP endpoint is configured for this app.</para>
        /// </param>
        /// <param name="env">Hosting environment.</param>
        /// <param name="loggerFactory">Logging factory.</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (ContentSource != null)
            {
                app.UseMvc(routes =>
                {
                    routes.MapRoute(
                        name: "getContent",
                        template: "Content/{directory}/{name}", defaults: new { controller = "ClientServerSyncContent", action = "GetUpdateContent" });
                });
            }

            // Wire the upstream WCF services
            app.UseSoapEndpoint<ClientSyncWebService>("/ClientWebService/client.asmx", new BasicHttpBinding(), SoapSerializer.XmlSerializer);
            app.UseSoapEndpoint<SimpleAuthenticationWebService>("/SimpleAuthWebService/SimpleAuth.asmx", new BasicHttpBinding(), SoapSerializer.XmlSerializer);
            app.UseSoapEndpoint<ReportingWebService>("/ReportingWebService/WebService.asmx", new BasicHttpBinding(), SoapSerializer.XmlSerializer);
        }
    }
}
