### Use UpdateServerStartup to add update services to your ASP.NET web app

#### Prerequisites
* updates were fetched to a file named master.zip
* content was downloaded to .\content
* the server configuration JSON is in server_configuration.json 
* the server address is http://my_update_server

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
            { "server-config", @"server_configuration.json"},
            { "content-source", @".\content" },
            { "content-http-root", @"http://my_update_server" }
        };
        config.AddInMemoryCollection(configDictionary);
    })
    .Build();

host.Run();
```
