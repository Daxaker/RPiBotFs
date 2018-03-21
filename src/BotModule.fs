module BotModule

open System
open System.Linq
open System.IO
open System.Reflection
open Akka.FSharp
open Telegram.Bot
open Telegram.Bot.Args
open Telegram.Bot.Types
open Telegram.Bot.Types.ReplyMarkups
open Settings
open Contracts
open Autofac
open System.Diagnostics


let listeners() = 
    let extensionsDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"extensions")
    let extensions = Directory.GetFiles(extensionsDir, "*.RPIExtension.dll")
    let builder = new ContainerBuilder()
    let map x =
        let addon = Assembly.LoadFrom(x)
        do builder.RegisterAssemblyTypes(addon).As<IListenerService>() |> ignore
        
    do extensions |> Seq.iter map
    let container = builder.Build()
    let lt = container.BeginLifetimeScope()
    let ex = lt.Resolve<seq<IListenerService>>()
    List.ofSeq ex
    
let system  = 
    System.create "mySystem" <| Configuration.defaultConfig()

let statefulActorOf handler (mailbox: Actor<_>) =
    let rec run runHandler defaultHandler = actor {
        let! message = mailbox.Receive()
        
        let next, handled =
            match (runHandler message) with
            |Continue  -> Some(runHandler), true
            |Become (handler') -> Some(handler'), true
            |Unhandled -> Some(runHandler), false
            |UnhandledWithReset -> Some(defaultHandler), false 
            |Reset -> Some(defaultHandler), true
            |Stop -> None, true
            
        if not <| handled then
            mailbox.Unhandled(message)
        
        match next with
        |None -> 
            mailbox.Context.Stop(mailbox.Self)
            return()
        |Some (handler') ->
            return! run handler' defaultHandler
    }
    
    run handler handler
    
let waitingCommandState = function
    |SingleCommand(fAsync) | NonSecureRequest(fAsync) ->
        fAsync |> Async.Start
        Continue
    |ChangeState(newState) -> 
        Become(newState)
    |_ -> Unhandled

let actorRef = 
    spawn system "myActor" <| statefulActorOf waitingCommandState    
  
let whoAmI (args:MessageEventArgs) = async {
    let chatId = args.Message.Chat.Id
    do! sendChatMessage args <| sprintf "Chat id is %d" chatId
}

let nullCheck value =
    match box value with
    |null -> String.Empty
    |_ -> value

let getCommands cmdStr args =
    listeners() |> List.map (fun (x:IListenerService) -> args |> x.GetListeners cmdStr)

let extraCmd args = function
    |"/reset" -> [SetDefaultHandler]
    |"/whoami" -> [NonSecureRequest(whoAmI args)]
    |_ -> []

let parseCommand (command:string) (args:MessageEventArgs) =
    let chatId = args.Message.Chat.Id
    let createRequest (cmds:#seq<Command>) =
        cmds |> Seq.map (fun cmd -> {UserChatId = chatId; Command=cmd})
    let cmd = command.ToLower()
    (cmd |> extraCmd args) @ getCommands cmd args |> createRequest 

let isAuthorized = function
    |{UserChatId = _; Command = NonSecureRequest _} -> true
    |{UserChatId = chatId; Command = _} -> inWhitelist <| string chatId

let sendCommand = function
    |{UserChatId=_; Command = SetDefaultHandler} -> actorRef <! Reset
    |request -> if isAuthorized request then actorRef <! request.Command

let sendCommands (cmds:#seq<Request>) =
    cmds |> Seq.iter sendCommand
                            
        
let OnMessageReceived (args:MessageEventArgs) =
    args |> parseCommand (args.Message.Text |> nullCheck) |> sendCommands

        
let OnMessageEventHandler =
    EventHandler<MessageEventArgs>(fun _ -> OnMessageReceived)

//let OnCallbackQuery (args:CallbackQueryEventArgs) =
//    let res =
//        async{
//            let! torrents = args.CallbackQuery.Data |> int |> GetTorrentAsync
//            client.Value.SendTextMessageAsync(Types.ChatId args.CallbackQuery.Message.Chat.Id, torrents.Torrents.Single().Name) |> ignore
//        }
//    Async.Start res

//let OnCallbackQueryEventHandler =
//    EventHandler<CallbackQueryEventArgs>(fun _ -> OnCallbackQuery)

let Start() =
    if not client.Value.IsReceiving then
        client.Value.OnMessage.AddHandler OnMessageEventHandler
//        client.Value.OnCallbackQuery.AddHandler OnCallbackQueryEventHandler
        client.Value.StartReceiving()

let Stop() =
    if client.Value.IsReceiving then
        client.Value.StopReceiving()
        client.Value.OnMessage.RemoveHandler OnMessageEventHandler
//        client.Value.OnCallbackQuery.RemoveHandler OnCallbackQueryEventHandler

let ShowConfig() = ReadConfiguration() |> Option.get
    
let TestMe() =
    client.Value.TestApiAsync()