# Windows Update Services ClientServer Sync Protocol

Provides a sample C# implementation (.NET Core) for the [Microsoft Update Client-Server protocol](https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-wusp/b8a2ad1d-11c4-4b64-a2cc-12771fcb079b), server side. This server implementation can be run on-premises or as a ASP.NET Web App in the cloud.

Use this library to build solutions for deploying Microsoft updates to Windows PCs.

This library assumes update metadata and content was acquired and indexed using the [Server-Server Sync library](https://github.com/microsoft/update-server-server-sync).

## Reference the library in your project
In your .NET Core project, add a reference to the **[UpdateServices.ClientServerSync NuGet package](https://www.nuget.org/packages/UpdateServices.ClientServerSync)**.

Alternatively, you can compile the code yourself. Visual Studio 2017 with .Net Core development tools is required to build the solution provided at build\client-server-sync.sln

## Using the library
Please refer to the API documentation for help on using the library.

Using the reference UpdateServerStartup to start a ASP.NET web app

Creating a custom ASP.NET web app

## Use the downsync utility
The downsync utility is provided as a sample for using the library. Downsync can be used to deploy updates to Windows PC.

You can build downsync in Visual Studio; it builds from the same solution as the library. Or download and unzip downsync from https://github.com/microsoft/update-client-server-sync/releases

See [Using the DownSync utility](https://github.com/microsoft/update-client-server-sync/wiki/Using-the-DownSync-utility)

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
