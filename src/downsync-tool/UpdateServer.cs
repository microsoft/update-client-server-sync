// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.UpdateServices.ClientSync.Server;
using Microsoft.UpdateServices.Storage;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.UpdateServices.Tools.UpdateServer
{
    /// <summary>
    /// Runs a service that provides updates to Windows PCs
    /// Requires a local repository. All or a subset of updates from the local repository can be served.
    /// </summary>
    class UpdateServer
    {
        public static void Run(RunUpdateServerOptions options)
        {
            // Check that the metadata source file exists
            if (!File.Exists(options.MetadataSource))
            {
                ConsoleOutput.WriteRed($"There is no metadata source at {options.MetadataSource}");
                return;
            }

            if (!File.Exists(options.ConfigFile))
            {
                ConsoleOutput.WriteRed($"There is no configuration file at path {options.ConfigFile}");
                return;
            }

            if (!string.IsNullOrEmpty(options.ContentPath))
            {
                if (!Directory.Exists(options.ContentPath))
                {
                    ConsoleOutput.WriteRed($"There is no content directory at path {options.ContentPath}");
                    return;
                }

                if (string.IsNullOrEmpty(options.Endpoint))
                {
                    ConsoleOutput.WriteRed($"Endpoint is required when serving content");
                    return;
                }
            }

            var configurationJson = File.ReadAllText(options.ConfigFile);

            var host = new WebHostBuilder()
                .UseUrls($"http://{options.Endpoint}:{options.Port}")
                .UseStartup<UpdateServerStartup>()
                .UseKestrel()
                .ConfigureKestrel((context, opts) => { })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddEventSourceLogger();
                })
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var configDictionary = new Dictionary<string, string>()
                    {
                        { "metadata-source", options.MetadataSource },
                        { "server-config", configurationJson},
                        { "content-source", options.ContentPath },
                        { "content-http-root", string.IsNullOrEmpty(options.Endpoint) ? null : $"http://{options.Endpoint}:{options.Port}" }
                    };

                    config.AddInMemoryCollection(configDictionary);
                })
                .Build();

            host.Run();
        }
    }
}
