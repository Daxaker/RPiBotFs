namespace RPiBotFs

open Microsoft.AspNetCore.Hosting
open System.IO

module Program =
    let exitCode = 0
    
    [<EntryPoint>]
    let main _ =
        let host =
            WebHostBuilder() 
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseKestrel()
                .UseStartup<Startup>()
                .Build()
        host.Run();
        exitCode
