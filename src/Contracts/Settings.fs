namespace Contracts

open System
open System.IO
open Newtonsoft.Json.Linq
open Newtonsoft.Json.Serialization
open System
open Newtonsoft.Json

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

    [<Literal>]
    let UserSettingsPath = ".config/rpibot.json"
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
    
    let homePath =
        if (Environment.OSVersion.Platform = PlatformID.Unix || Environment.OSVersion.Platform = PlatformID.MacOSX) then
            Environment.GetEnvironmentVariable(HomePathNix)
        else
            Environment.ExpandEnvironmentVariables(HomePathWindows);
    
    let private path =
        Path.Combine(homePath, UserSettingsPath)
    
    let settings =
        let getFile fPath =
            if File.Exists fPath then
                Some(JObject.Parse(File.ReadAllText(fPath)).ToObject<JConfig>())
            else
                None
        let userSettings = getFile path            
        let appSettings = getFile AppSettingsFile
        seq {
            yield userSettings
            yield appSettings
        } 
    
    let (?) (this : #seq<JConfig option>) prop: 'Result option =
      let pick toption = 
        try
            let t = toption |> Option.get
            let v = t.GetType().GetProperty(prop).GetValue(t, null)
            if v |> isNull |> not then 
                Some(v :?> 'Result)
            else None
        with _ -> printfn "JConfig param %s missing" prop 
                  None
      this |> Seq.tryPick pick
    
    let transmissionAddress =
        stringValue settings?transmission
    
    let botKey =
        stringValue settings?botKey
    
    let ftpPath =
        stringValue settings?ftpPath
    
    let isWhiteListEnabled =
        boolValue settings?isWhiteListEnabled
    
    let whiteListArray:string[] = 
        arrayValue settings?whiteList
    
    let inWhitelist user =
        if isWhiteListEnabled |> not then
            true
        else
            let innerArray = whiteListArray
            innerArray |> Seq.exists (fun x -> user = x)
    
    let webInterfacePort:int =
        settings?listeningPort |> Option.get
    
    let ReadConfiguration() =
        settings |> Seq.tryPick id
