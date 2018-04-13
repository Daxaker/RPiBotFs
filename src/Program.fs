namespace RPiBotFs

open Argu
open Microsoft.AspNetCore.Hosting
open System.IO
open Contracts
open Microsoft.AspNetCore

type CLIArgs =
    |[<AltCommandLine("-c")>]ConfigFile of path:string
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | ConfigFile(path) -> """path to config file"""

module Program =
    open Argu

    let exitCode = 0
    
    [<EntryPoint>]
    let main args =
        let parser = ArgumentParser.Create<CLIArgs>()
        let cliArgs = parser.Parse(args)
        let configFile = cliArgs.TryGetResult ConfigFile
        defaultArg configFile "" |> SetUserSettingsPath |> ignore
        let host =
            WebHost
                .CreateDefaultBuilder() 
                .UseStartup<Startup>()
                .UseUrls(webInterfacePort() |> sprintf "http://+:%d" )
                .Build()
        host.Run();
        exitCode
