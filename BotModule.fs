module BotModule

open System
open System.Collections.Generic
open System.Linq
open System.IO
open Telegram.Bot
open Telegram.Bot.Args
open Telegram.Bot.Types
open Telegram.Bot.Types.ReplyMarkups
open TransmissionModule
open Settings

let private client =
    lazy(new TelegramBotClient(botKey))

type State =
    |Empty
    |WaitForTorrent

let changeState =
    let map = Dictionary<int64, _>()
    fun chatId state->
        match map.TryGetValue chatId with
        |true, value -> 
            map.[chatId] <- state
            value
        |_ -> 
            map.[chatId] <- state
            Empty

let (|Prefix|_|) (p:string) (s:string) =
    match s with 
    |null -> None
    |_ when s.StartsWith(p) -> Some <| s.Substring p.Length
    |_ -> None 
let (|InvariantEqual|_|) (str:string) arg = 
  if String.Compare(str, arg, StringComparison.OrdinalIgnoreCase) = 0
    then Some() else None
let getCallbackButton (torrentInfo: Transmission.API.RPC.Entity.TorrentInfo) =
    [|InlineKeyboardButton.WithCallbackData(torrentInfo.Name, torrentInfo.ID.ToString())|] :> IEnumerable<InlineKeyboardButton>
let sendMessageMarkup markup (chatId:int64) message  =
    client.Value.SendTextMessageAsync(Types.ChatId chatId, message, Types.Enums.ParseMode.Default,false, false, 0, markup) |> Async.AwaitTask |> Async.Ignore
let sendChatMessageMarkup markup (arg: MessageEventArgs) =
    sendMessageMarkup markup arg.Message.Chat.Id
let sendChatMessage arg =
    sendChatMessageMarkup null arg
//TODO: Exception handling
let parseCommand (command:string) (args:MessageEventArgs) =
    match command.ToLower() with
        |"torrents" -> 
            async {
                try
                    let! torrents = GetTorrentListAsync()
                    let callbackResults = 
                        torrents.Torrents |> Array.map getCallbackButton
                    let markup = callbackResults |> InlineKeyboardMarkup
                    do! sendChatMessageMarkup markup args "Torrents"
                with error -> do!  sendChatMessage args error.Message
            } |> Async.Start
        |"active" ->
            async{
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
            } |> Async.Start
        |"addtorrent" ->
            changeState args.Message.Chat.Id WaitForTorrent |> ignore
        |_ -> ()   
let OnMessageReceived (args:MessageEventArgs) =
    match changeState args.Message.Chat.Id Empty with
        |Empty -> 
            match args.Message.Text with
            |Prefix "/" rest ->
                args |> parseCommand rest
            |_ -> ()
                    
        |WaitForTorrent -> 
            match args.Message.Type with
            |Enums.MessageType.Document -> 
                async {
                    let filePath =
                        Path.Combine(Path.GetTempPath(), args.Message.Document.FileName)
                    use stream = File.Create(filePath)
                    do! client.Value.GetInfoAndDownloadFileAsync(args.Message.Document.FileId, stream) |> Async.AwaitTask |> Async.Ignore
                    stream.Dispose() |> ignore
                    do! AddTorrentFileAsync filePath
                    changeState args.Message.Chat.Id Empty |> ignore
                    do! sendChatMessage args "File added"
                } |> Async.Start
            |Enums.MessageType.Text -> 
                async { 
                    do! args.Message.Text |> AddTorrentMagnetAsync
                    changeState args.Message.Chat.Id Empty |> ignore
                    do! sendChatMessage args "File added"
                } |> Async.Start
            |_ -> ()

        
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