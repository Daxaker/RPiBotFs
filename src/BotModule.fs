module BotModule

open System
open System.Collections.Generic
open System.Collections.Concurrent
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

let private client =
    lazy(new TelegramBotClient(botKey))

[<AutoOpen>]
module MessageHelper =
    let getCallbackButton (torrentInfo: Transmission.API.RPC.Entity.TorrentInfo) =
        [|InlineKeyboardButton.WithCallbackData(torrentInfo.Name, torrentInfo.ID.ToString())|] :> IEnumerable<InlineKeyboardButton>
    
    let sendMessageMarkup markup (chatId:int64) message  =
        client.Value.SendTextMessageAsync(Types.ChatId chatId, message, Types.Enums.ParseMode.Default,false, false, 0, markup) |> Async.AwaitTask |> Async.Ignore
        
    let sendChatMessageMarkup markup (arg: MessageEventArgs) =
        sendMessageMarkup markup arg.Message.Chat.Id

    let sendChatMessage arg =
        sendChatMessageMarkup null arg
        

type Command =
    |SingleCommand of Async<unit>
    |ChangeState of (Command -> Instruction<Command>)
    |RawData of MessageEventArgs
    |Skip  

let torrentsList args =
     async {
        try
            let! torrents = GetTorrentListAsync()
            let callbackResults = 
                torrents.Torrents |> Array.map getCallbackButton
            let markup = callbackResults |> InlineKeyboardMarkup
            do! sendChatMessageMarkup markup args "Torrents"
        with error -> do!  sendChatMessage args error.Message
     }
let activeTorrents args =
    async {
        try
            let! torrents = GetTorrentListAsync()
            let result = torrents.Torrents |> Array.filter (fun t -> t.ETA > 0)
            if result |> Array.isEmpty then
                do! "Active downloads not found" |> sendChatMessage args
            else
                let str (r:Transmission.API.RPC.Entity.TorrentInfo) =
                    r.Name + Environment.NewLine + string(TimeSpan.FromSeconds(float r.ETA))
                let tasks = result |> Array.map  (fun r -> sendChatMessage args (r |> str))
                do! tasks |> Async.Parallel |> Async.Ignore
        with error -> do! sendChatMessage args error.Message
    }
let waitingCommand = function
    |SingleCommand(fAsync) ->
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
    |_ -> ()
    Become(waitingCommand)

let system  = 
    System.create "mySystem" <| Configuration.defaultConfig()

let actorRef = 
    spawn system "myActor" <| statefulActorOf waitingCommand

let nullCheck value =
    match box value with
    |null -> String.Empty
    |_ -> value
let parseCommand (command:string) (args:MessageEventArgs) =
    match command.ToLower() with
        |"/torrents" -> SingleCommand(torrentsList args)
        |"/active" -> SingleCommand(activeTorrents args)
        |"/addtorrent" -> ChangeState(waitTorrentState)
        |_ -> RawData(args)   

let sendCommand cmd =
    actorRef <! cmd
        
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

let TestMe() =
    client.Value.TestApiAsync()