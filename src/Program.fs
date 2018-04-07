namespace RPiBotFs

open Microsoft.AspNetCore.Hosting
open System.IO
open Contracts
open Microsoft.AspNetCore

module Program =
    let exitCode = 0
    
    [<EntryPoint>]
    let main _ =
        let host =
            WebHost
                .CreateDefaultBuilder() 
                .UseKestrel()
                .UseStartup<Startup>()
                .UseUrls(sprintf "http://+:%d" webInterfacePort)
                .Build()
        host.Run();
        exitCode
