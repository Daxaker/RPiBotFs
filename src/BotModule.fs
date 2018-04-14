module BotModule

open System
open System.IO
open System.Reflection
open Akka.FSharp
open Telegram.Bot.Args
open Contracts
open Autofac

let syncObject = obj;

let listeners() = 
    try
        let extensionsDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"extensions")
        let dirs = extensionsDir |> Directory.GetDirectories
        
        let builder = new ContainerBuilder()
        let getExtension dir =
            let extensions = Directory.GetFiles(dir, "*.RPIExtension.dll")
            let map x =
                let addon = Assembly.LoadFrom(x)
                do builder.RegisterAssemblyTypes(addon).As<IListenerService>() |> ignore
                
            do extensions |> Seq.iter map
            let container = builder.Build()
            let lt = container.BeginLifetimeScope()
            lt.Resolve<seq<IListenerService>>()
        dirs |> Seq.collect getExtension |> List.ofSeq
    with exn -> printf "FAIL: %A" exn; List.empty
    
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
    |{UserChatId = chatId; Command = _} -> chatId |> string |> inWhitelist  

let sendCommand = function
    |{UserChatId=_; Command = SetDefaultHandler} -> actorRef <! Reset
    |request -> if isAuthorized request then actorRef <! request.Command

let sendCommands (cmds:#seq<Request>) =
    cmds |> Seq.iter sendCommand
                            
        
let OnMessageReceived (args:MessageEventArgs) =
    args |> parseCommand (args.Message.Text |> nullCheck) |> sendCommands

        
let OnMessageEventHandler =
    EventHandler<MessageEventArgs>(fun _ -> OnMessageReceived)

let Start() =
    let receiving() =
        if not client.Value.IsReceiving then
            client.Value.OnMessage.AddHandler OnMessageEventHandler
            client.Value.StartReceiving()
    lock syncObject receiving
                            

let Stop() =
    let stop() =
        if client.Value.IsReceiving then
            client.Value.StopReceiving()
            client.Value.OnMessage.RemoveHandler OnMessageEventHandler
    lock syncObject stop

let ShowConfig() = readConfiguration()
    
let TestMe() =
    client.Value.TestApiAsync()