module Settings

open System
open System.IO
open Newtonsoft.Json.Linq

[<Literal>]
let DefaultPort = "9090"
[<Literal>]
let UserSettingsPath = ".config/rpibot.json"
[<Literal>]
let HomePathNix = "HOME"
[<Literal>]
let HomePathWindows = "%HOMEDRIVE%%HOMEPATH%"
[<Literal>]
let AppSettingsFile = "variables.json"
[<Literal>]
let TransmissionAddress = "transmission"
[<Literal>]
let BotKey = "bot_key"
[<Literal>]
let FtpPath = "ftp_path"
[<Literal>]
let IsWhiteListEnabled = "white_list_enabled"
[<Literal>]
let WhiteList = "white_list"
[<Literal>]
let ListeningPort = "listening_port"

let getValue<'a> token (def:'a) = 
    match token with
    |Some (j:JToken) -> j.Value<'a>()
    |_ -> def

let getParam param (item:JObject option) =
    match item with
    |Some itm ->
        let value = itm.[param]
        if  value |> isNull then
            None
        else
            Some(value)
    |_ -> None

let stringValue st =
    getValue st String.Empty

let boolValue bt =
    getValue bt false

let homePath =
    if (Environment.OSVersion.Platform = PlatformID.Unix || Environment.OSVersion.Platform = PlatformID.MacOSX) then
        Environment.GetEnvironmentVariable(HomePathNix)
    else
        Environment.ExpandEnvironmentVariables(HomePathWindows);

let private path =
    Path.Combine(homePath, UserSettingsPath)

let settings =
    let userSettings =
        if File.Exists path then
            Some(JObject.Parse(File.ReadAllText(path)))
        else
            None
    let appSettings =
        JObject.Parse(File.ReadAllText(AppSettingsFile))
    seq {
        yield userSettings
        yield Some(appSettings)
    } 

let private tryGetValue param = 
    settings |> Seq.tryPick (param |> getParam)

let transmissionAddress =
    stringValue <| tryGetValue TransmissionAddress

let botKey =
    stringValue <| tryGetValue BotKey

let ftpPath =
    stringValue <| tryGetValue FtpPath

let isWhiteListEnabled =
    tryGetValue IsWhiteListEnabled

let whiteList = 
    tryGetValue WhiteList

let inWhitelist user =
    if isWhiteListEnabled |> boolValue |> not then
        true
    else
        let innerArray = 
            let value = whiteList
            match value with
            |Some v -> Some(v.Value<JArray>().Values<string>())
            |_ -> None
        innerArray.IsSome && innerArray |> Option.get |> Seq.exists (fun x -> user = x)

let listeningPort =
    let value = stringValue <| tryGetValue ListeningPort
    if String.IsNullOrWhiteSpace value then
        DefaultPort
    else
        value

let ReadConfiguration() =
    settings |> Seq.tryPick id
