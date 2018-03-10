namespace RPiBotFs

open Microsoft.AspNetCore.Builder
open Nancy.Owin

type Startup() =
    member __.Configure(app: IApplicationBuilder) =
        app.UseOwin(fun x -> x.UseNancy() |> ignore) |> ignore