This library provides a C# reference implementation for a Windows Update server writtent as a ASP.NET web app.

#### Requirements
Updates must be fetched from an upstream server and indexed before they can be served to Windows clients.

Use [the Server-Server Sync library](https://github.com/microsoft/update-server-server-sync) to aquire updates.

A pre-built tool to fetch and index updates is available: [UpSync](https://github.com/microsoft/update-server-server-sync/wiki/UpSync-examples)

#### The reference [UpdateServerStartup](Microsoft.UpdateServices.ClientSync.Server.UpdateServerStartup.html)
Use UpdateServerStartup to add all the required update services to your ASP.NET instance.

Prerequisites:
* updates were fetched to a file named master.zip
* content was downloaded to .\content
* the server configuration JSON is in server_configuration.json 
* the server address is http://my_update_server

```
var host = new WebHostBuilder()
    .UseUrls(@"http://my_update_server")
    .UseStartup<UpdateServerStartup>()
    .UseKestrel()
    .ConfigureKestrel((context, opts) => { })
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        var configDictionary = new Dictionary<string, string>()
        {
            { "metadata-source", @"master.zip" },
            { "server-config", @"server_configuration.json"},
            { "content-source", @".\content" },
            { "content-http-root", @"http://my_update_server" }
        };
        config.AddInMemoryCollection(configDictionary);
    })
    .Build();

host.Run();
```

If your server does not store update content, just metadata, then "content-source" and "content-http-root" settings can be null. In this case, Windows clients will download content from the official Microsoft Update content servers.