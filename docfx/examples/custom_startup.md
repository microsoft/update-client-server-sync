### Adding the update service and content service to your ASP.NET web app

#### Prerequisites
* update metadata was fetched to a file named master.zip. Use UpSync to fetch updates from an upstream server ([link](https://github.com/microsoft/update-server-server-sync/wiki/UpSync-examples))
* the server configuration JSON is in server_configuration.json. Get a default configuration file from [here](https://raw.githubusercontent.com/microsoft/update-client-server-sync/master/src/downsync-tool/default-server-configuration.json)
* update content was downloaded to .\content. Use UpSync to fetch update content.
* the server address is http://<my_update_server>:<port> or http://<ip>:<port>

#### Add update services to your startup

```
// Startup's ConfigureServices
public void ConfigureServices(IServiceCollection services)
{
    var localMetadataSource = CompressedMetadataStore.Open(sourcePath);

    var contentSource = new FileSystemContentStore(contentPath);

    var updateServiceConfiguration = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(
        File.ReadAllText("server_configuration.json"));
    
    // Add ClientSyncContentController from its containing assembly
    services.TryAddSingleton<ClientSyncContentController>(
        new ClientSyncContentController(localMetadataSource, contentSource));

    services
        .AddMvc()
        .AddApplicationPart(
            Assembly.Load(
              "Microsoft.UpdateServices.ClientSync.Server.ClientSyncContentController"))
        .AddControllersAsServices();

    // Enable SoapCore; this middleware provides translation services from WCF/SOAP to Asp.net
    services.AddSoapCore();

    services.TryAddSingleton<ClientSyncWebService>(
        new Server.ClientSyncWebService(
            localMetadataSource, UpdateServiceConfiguration, "http://my_update_server"));

    services.TryAddSingleton<SimpleAuthenticationWebService>();
    services.TryAddSingleton<ReportingWebService>();
}

// Startup's configure
public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
{
    // Add the content controller to MVC route
    app.UseMvc(routes =>
    {
        routes.MapRoute(
            name: "getContent",
            template: "Content/{directory}/{name}",
            defaults: new
            {
                controller = "ClientSyncContent",
                action = "GetUpdateContent"
            });
    });
    
    // Wire the SOAP services
    app.UseSoapEndpoint<ClientSyncWebService>(
        "/ClientWebService/client.asmx",
        new BasicHttpBinding(),
        SoapSerializer.XmlSerializer);

    app.UseSoapEndpoint<SimpleAuthenticationWebService>(
        "/SimpleAuthWebService/SimpleAuth.asmx",
        new BasicHttpBinding(),
        SoapSerializer.XmlSerializer);

    app.UseSoapEndpoint<ReportingWebService>(
        "/ReportingWebService/WebService.asmx",
        new BasicHttpBinding(),
        SoapSerializer.XmlSerializer);
}
```
