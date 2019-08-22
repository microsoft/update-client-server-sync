// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CommandLine;
using System;

namespace Microsoft.UpdateServices.Tools.UpdateServer
{
    class Program
    {
        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<RunUpdateServerOptions>(args)
                .WithParsed<RunUpdateServerOptions>(opts => UpdateServer.Run(opts))
                .WithNotParsed(failed => Console.WriteLine("Error"));
        }
    }
}
