namespace RPiBotFs

open Microsoft.AspNetCore.Mvc
open BotModule

[<Route("/")>]
type ApiController () =
    inherit Controller()

     [<HttpGet("start")>]
    member __.Start() =
        BotModule.Start()

    [<HttpGet("stop")>]
    member __.Stop() =
        BotModule.Stop()

    [<HttpGet("showconfig")>]
    member __.ShowConfig() = 
        ObjectResult(ShowConfig())
