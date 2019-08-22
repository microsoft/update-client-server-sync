### Adding the update service and content service to your ASP.NET web app

#### Prerequisites
* updates were fetched to a file named master.zip
* content was downloaded to .\content
* the server configuration JSON is in server_configuration.json 
* the server address is http://my_update_server

In your ASP.NEt startup class:

```
public void ConfigureServices(IServiceCollection services)
{
    var localMetadataSource = CompressedMetadataStore.Open(sourcePath);

    var contentSource = new FileSystemContentStore(contentPath);

    var updateServiceConfiguration = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(
        File.OpenText("server_configuration.json").ReadToEnd());
    
    // Add ClientServerSyncContentController from its containing assembly
    services.TryAddSingleton&lt;ClientServerSyncContentController&gt;(
        new ClientServerSyncContentController(localMetadataSource, contentSource));

    services
        .AddMvc()
        .AddApplicationPart(
            Assembly.Load(
              "Microsoft.UpdateServices.ClientSync.Server.ClientServerSyncContentController"))
        .AddControllersAsServices();

    // Enable SoapCore; this middleware provides translation services from WCF/SOAP to Asp.net
    services.AddSoapCore();

    services.TryAddSingleton<ClientSyncWebService>(
        new Server.ClientSyncWebService(
            localMetadataSource, UpdateServiceConfiguration, "http://my_update_server"));

    services.TryAddSingleton<SimpleAuthenticationWebService>();
    services.TryAddSingleton<ReportingWebService>();
}
    
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
                controller = "ClientServerSyncContent",
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
