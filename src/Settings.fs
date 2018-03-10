module Settings

open System
open System.IO
open Newtonsoft.Json.Linq

let (?) (item:JObject) param =
    item.[param]

let homePath =
    if (Environment.OSVersion.Platform = PlatformID.Unix || Environment.OSVersion.Platform = PlatformID.MacOSX) then
        Environment.GetEnvironmentVariable("HOME")
    else
        Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");


let private path =
    Path.Combine(homePath, ".config", "rpibot.json")

let private settings =
    if not <| File.Exists path then
        JObject.Parse(File.ReadAllText("variables.json"))
    else
        JObject.Parse(File.ReadAllText(path))

let transmissionAddress =
    (settings?transmission).Value<string>()

let botKey =
    (settings?bot_key).Value<string>()