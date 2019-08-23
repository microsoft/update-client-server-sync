### Use UpdateServerStartup to add update services to your ASP.NET web app

#### Prerequisites
* updates were fetched to a file named master.zip. Use UpSync to fetch some updates from an upstream server ([link](https://github.com/microsoft/update-server-server-sync/wiki/UpSync-examples))
* the server configuration JSON is in server_configuration.json. Get a default configuration file from [here](https://raw.githubusercontent.com/microsoft/update-client-server-sync/master/src/downsync-tool/default-server-configuration.json)

#### Use the startup in your app

```
var host = new WebHostBuilder()
    .UseUrls("http://my_update_server")
    .UseStartup<UpdateServerStartup>()
    .UseKestrel()
    .ConfigureKestrel((context, opts) => { })
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        var configDictionary = new Dictionary<string, string>()
        {
            { "metadata-source", @"master.zip" },
            { "server-config", File.ReadAllText(@"server_configuration.json")},
            // This server does not serve update content; clients will be directed
            // to the Microsoft Update CDN
            { "content-source", null },
            // Not required when content downloads are redirected to the official CDN
            { "content-http-root", null }
        };
        config.AddInMemoryCollection(configDictionary);
    })
    .Build();

host.Run();
```
