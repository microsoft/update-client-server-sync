// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CommandLine;
using System.Collections.Generic;

namespace Microsoft.UpdateServices.Tools.UpdateServer
{
    [Verb("run-server", HelpText = "Serve updates to Windows PCs")]
    public class RunUpdateServerOptions
    {
        [Option("metadata-source", Required = true, HelpText = "Source of update metadata")]
        public string MetadataSource { get; set; }

        [Option("content-source", Required = false, HelpText = "Source of update content")]
        public string ContentPath { get; set; }

        [Option("port", Required = false, Default = 32150, HelpText = "The port to bind the server to.")]
        public int Port { get; set; }

        [Option("endpoint", Required = false, HelpText = "The endpoint to bind the server to.")]
        public string Endpoint { get; set; }

        [Option("server-config", Required = false, Default = "default-server-configuration.json", HelpText = "Server configuration file")]
        public string ConfigFile { get; set; }
    }
}
