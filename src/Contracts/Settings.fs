namespace Contracts

open System
open System.IO
open Newtonsoft.Json.Linq
open Newtonsoft.Json

[<CLIMutable>]
type JConfig = {
    [<JsonProperty(PropertyName = "transmission")>]
    transmission:string
    [<JsonProperty(PropertyName = "bot_key")>]
    botKey:string
    [<JsonProperty(PropertyName = "ftp_path")>]
    ftpPath:string
    [<JsonProperty(PropertyName = "white_list_enabled")>]
    isWhiteListEnabled:bool
    [<JsonProperty(PropertyName = "white_list")>]
    whiteList:string[]
    [<JsonProperty(PropertyName = "listening_port")>]
    listeningPort:Nullable<int>
}

[<AutoOpen>]
module Settings =
    let mutable private path = "variables.json"
    let SetPath p =
        path <- p
    
    let settings : Lazy<JConfig> = lazy
        (
            if String.IsNullOrEmpty(path) |> not && File.Exists path then
                JObject.Parse(File.ReadAllText(path)).ToObject<JConfig>()
            else
                failwith "No settings file"
        )
    
    let transmissionAddress =
        lazy(settings.Value.transmission)
    
    let botKey =
        lazy(settings.Value.botKey)
    
    let ftpPath =
        lazy(settings.Value.ftpPath)
    
    let isWhiteListEnabled =
        lazy(settings.Value.isWhiteListEnabled)
    
    let whiteListArray:Lazy<string[]> = 
        lazy(settings.Value.whiteList)
    
    let inWhitelist user =
        if isWhiteListEnabled.Value |> not then
            true
        else
            let innerArray = whiteListArray
            innerArray.Value |> Seq.exists (fun x -> user = x)
    
    let webInterfacePort():int  =
        (settings.Value.listeningPort |> Option.ofNullable, 9090) ||> defaultArg
    
    let readConfiguration() =
        settings.Value
