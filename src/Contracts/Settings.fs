namespace Contracts

open System
open System.IO
open Newtonsoft.Json.Linq
open Newtonsoft.Json.Serialization
open System
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
    let SetUserSettingsPath =
        let mutable memory = null 
        let res path =
            if isNull memory then 
                memory <- path
            memory
        res
    [<Literal>]
    let HomePathNix = "HOME"
    [<Literal>]
    let HomePathWindows = "%HOMEDRIVE%%HOMEPATH%"
    [<Literal>]
    let AppSettingsFile = "variables.json"
    
    let getValue<'a> token (def:'a) = 
        match token with
        |Some (s) -> s 
        |None -> def
    
    let stringValue st =
        getValue st String.Empty
    
    let boolValue bt =
        getValue bt false
    
    let arrayValue arr =
        getValue arr [||]
    
    let settings = lazy(
        let getFile fPath =
            if String.IsNullOrEmpty(fPath) |> not && File.Exists fPath then
                Some(JObject.Parse(File.ReadAllText(fPath)).ToObject<JConfig>())
            else
                None
        let userSettings = "" |> SetUserSettingsPath |> getFile           
        let appSettings = getFile AppSettingsFile
        seq {
            yield userSettings
            yield appSettings
        })
    
    let (?) (this : Lazy<#seq<JConfig option>>) prop: 'Result option =
      let pick toption = 
        try
           if toption |> Option.isNone then
                None
           else 
                let t = toption |> Option.get
                let v = t.GetType().GetProperty(prop).GetValue(t, null)
                if v |> isNull |> not then 
                    Some(v :?> 'Result)
                else None
        with _ -> printfn "JConfig param %s missing" prop 
                  None
      this.Value |> Seq.tryPick pick
    
    let transmissionAddress =
        lazy(stringValue settings?transmission)
    
    let botKey =
        lazy(stringValue settings?botKey)
    
    let ftpPath =
        lazy(stringValue settings?ftpPath)
    
    let isWhiteListEnabled =
        lazy(boolValue settings?isWhiteListEnabled)
    
    let whiteListArray:Lazy<string[]> = 
        lazy(arrayValue settings?whiteList)
    
    let inWhitelist user =
        if isWhiteListEnabled.Value |> not then
            true
        else
            let innerArray = whiteListArray
            innerArray.Value |> Seq.exists (fun x -> user = x)
    
    let webInterfacePort():int  =
        settings?listeningPort |> Option.get
    
    let ReadConfiguration() =
        settings.Value |> Seq.tryPick id
