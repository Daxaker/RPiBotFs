namespace RPiBotFs

open Microsoft.AspNetCore.Hosting
open System.IO
open Settings

module Program =
    let exitCode = 0
    
    [<EntryPoint>]
    let main _ =
        let host =
            WebHostBuilder() 
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseKestrel()
                .UseStartup<Startup>()
                .UseUrls(sprintf "http://+:%d" webInterfacePort)
                .Build()
        host.Run();
        exitCode
