module BotModule

open System
open System.Linq
open System.IO
open Akka.FSharp
open Telegram.Bot
open Telegram.Bot.Args
open Telegram.Bot.Types
open Telegram.Bot.Types.ReplyMarkups
open TransmissionModule
open Settings
open Stateful
open TelegramBotClientModule

type Command =
    |NonSecureRequest of Async<unit>
    |SingleCommand of Async<unit>
    |ChangeState of (Command -> Instruction<Command>)
    |RawData of MessageEventArgs
    |Skip  

type Request = {
    UserChatId: int64 
    Command:Command
}


let torrentsList args = async {
    try
        let! torrents = GetTorrentListAsync()
        let callbackResults = 
            torrents.Torrents |> Array.map getCallbackButton
        let markup = callbackResults |> InlineKeyboardMarkup
        do! sendChatMessageMarkup markup args "Torrents"
    with error -> do!  sendChatMessage args error.Message
}
     
let activeTorrents args = async {
    try
        let! torrents = GetTorrentListAsync()
        let result = torrents.Torrents |> Array.filter (fun t -> t.ETA <> -1)
        if result |> Array.isEmpty then
            do! sendChatMessage args "Active downloads not found" 
        else
            let str (r:Transmission.API.RPC.Entity.TorrentInfo) =
                r.Name + Environment.NewLine + if r.ETA > 0 then 
                                                string( TimeSpan.FromSeconds(float r.ETA))  
                                               else "Unknown"
            let tasks = result |> Array.map  (fun r -> sendChatMessage args (r |> str))
            do! tasks |> Async.Parallel |> Async.Ignore
    with error -> do! sendChatMessage args error.Message
}
let whoAmI (args:MessageEventArgs) = async {
    let chatId = args.Message.Chat.Id
    do! sendChatMessage args <| sprintf "Chat id is %d" chatId
}     
let waitingCommandState = function
    |SingleCommand(fAsync) | NonSecureRequest(fAsync) ->
        fAsync |> Async.Start
        Continue
    |ChangeState(newState) -> 
        Become(newState)
    |_ -> Unhandled
    
let waitTorrentState cmd = 
    match cmd with
    |RawData(args) -> 
        async {
           match args.Message.Type with
           |Enums.MessageType.Document -> 
                let filePath =
                    Path.Combine(Path.GetTempPath(), args.Message.Document.FileName)
                use stream = File.Create(filePath)
                do! client.Value.GetInfoAndDownloadFileAsync(args.Message.Document.FileId, stream) |> Async.AwaitTask |> Async.Ignore
                stream.Dispose() |> ignore
                do! AddTorrentFileAsync filePath
                do! sendChatMessage args "File added"
            |Enums.MessageType.Text -> 
                do! args.Message.Text |> AddTorrentMagnetAsync
                do! sendChatMessage args "File added"
            |_ -> ()
        } |> Async.Start
        Become(waitingCommandState)
    |_ -> UnhandledWithBecome(waitingCommandState)

let system  = 
    System.create "mySystem" <| Configuration.defaultConfig()

let actorRef = 
    spawn system "myActor" <| statefulActorOf waitingCommandState

let nullCheck value =
    match box value with
    |null -> String.Empty
    |_ -> value
    
let parseCommand (command:string) (args:MessageEventArgs) =
    let chatId = args.Message.Chat.Id
    let createRequest cmd =
        {UserChatId = chatId; Command=cmd}
    match command.ToLower() with
        |"/whoami" -> NonSecureRequest(whoAmI args) |> createRequest
        |"/torrents" -> SingleCommand(torrentsList args) |> createRequest
        |"/active" -> SingleCommand(activeTorrents args)|> createRequest 
        |"/addtorrent" -> ChangeState(waitTorrentState)|> createRequest 
        |_ -> RawData(args) |> createRequest   

let isAuthorized = function
    |{UserChatId = _; Command = NonSecureRequest _} -> true
    |{UserChatId = chatId; Command = _} -> inWhitelist <| string chatId

let sendCommand (cmd:Request) =
    if isAuthorized cmd then
        actorRef <! cmd.Command
        
let OnMessageReceived (args:MessageEventArgs) =
    args |> parseCommand (args.Message.Text |> nullCheck) |> sendCommand

        
let OnMessageEventHandler =
    EventHandler<MessageEventArgs>(fun _ -> OnMessageReceived)

let OnCallbackQuery (args:CallbackQueryEventArgs) =
    let res =
        async{
            let! torrents = args.CallbackQuery.Data |> int |> GetTorrentAsync
            client.Value.SendTextMessageAsync(Types.ChatId args.CallbackQuery.Message.Chat.Id, torrents.Torrents.Single().Name) |> ignore
        }
    Async.Start res

let OnCallbackQueryEventHandler =
    EventHandler<CallbackQueryEventArgs>(fun _ -> OnCallbackQuery)

let Start() =
    if not client.Value.IsReceiving then
        client.Value.OnMessage.AddHandler OnMessageEventHandler
        client.Value.OnCallbackQuery.AddHandler OnCallbackQueryEventHandler
        client.Value.StartReceiving()

let Stop() =
    if client.Value.IsReceiving then
        client.Value.StopReceiving()
        client.Value.OnMessage.RemoveHandler OnMessageEventHandler
        client.Value.OnCallbackQuery.RemoveHandler OnCallbackQueryEventHandler

let ShowConfig() = 
    match ReadConfiguration() with
    |Some c -> c.Root.ToString()
    |_ -> failwith "No config file"
let TestMe() =
    client.Value.TestApiAsync()