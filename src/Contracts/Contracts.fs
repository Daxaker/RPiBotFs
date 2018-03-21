module Contracts

open Telegram.Bot.Args
open Akka.FSharp

type Handler<'TMessage> = 'TMessage -> Instruction<'TMessage>
and Instruction<'TMessage> =
    |Continue
    |Become of (Handler<'TMessage>)
    |UnhandledWithBecome of (Handler<'TMessage>)
    |Unhandled
    |Reset
    |Stop
    
let statefulActorOf handler (mailbox: Actor<_>) =
    let rec run runHandler defaultHandler = actor {
        let! message = mailbox.Receive()
        
        let next, handled =
            match (runHandler message) with
            |Continue  -> Some(runHandler), true
            |Become (handler') -> Some(handler'), true
            |Unhandled -> Some(runHandler), false
            |UnhandledWithBecome (handler') -> Some(handler'), false 
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

type Command =
    |NonSecureRequest of Async<unit>
    |SingleCommand of Async<unit>
    |ChangeState of (Command -> Instruction<Command>)
    |RawData of MessageEventArgs
    |SetDefaultHandler
    |Skip  

type Request = {
    UserChatId: int64 
    Command:Command
}