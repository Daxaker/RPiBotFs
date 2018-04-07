namespace RPiBotFs

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection

type Startup private () =
    new (configuration: IConfiguration) as self =
        Startup() then
        self.Configuration <- configuration

    // This method gets called by the runtime. Use this method to add services to the container.
    member __.ConfigureServices(services: IServiceCollection) =
        // Add framework services.
        services.AddMvc() |> ignore

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    member __.Configure(app: IApplicationBuilder, env: IHostingEnvironment) =
        match env.IsDevelopment() with
        |true -> app.UseDeveloperExceptionPage() |> ignore
        |_ -> app.UseExceptionHandler("/Error") |> ignore
        app.UseMvc(fun routes ->
                       routes.MapRoute("default", "{controller}/{action=Index}/{id?}") |> ignore
            ) |> ignore

    member val Configuration : IConfiguration = null with get, set