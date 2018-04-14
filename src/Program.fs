namespace RPiBotFs

open Argu
open Microsoft.AspNetCore.Hosting
open Contracts
open Microsoft.AspNetCore

type CLIArgs =
    |[<AltCommandLine("-c")>]ConfigFile of path:string
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | ConfigFile(_) -> """path to config file"""

module Program =
    let exitCode = 0
    
    [<EntryPoint>]
    let main args =
        let parser = ArgumentParser.Create<CLIArgs>()
        let cliArgs = parser.Parse(args)
        let configFile = cliArgs.TryGetResult ConfigFile
        let cfgFile = defaultArg configFile ""
        do printfn "External config %s" cfgFile
        cfgFile |> SetPath |> ignore
        let host =
            WebHost
                .CreateDefaultBuilder() 
                .UseStartup<Startup>()
                .UseUrls(webInterfacePort() |> sprintf "http://+:%d" )
                .Build()
        host.Run();
        exitCode
