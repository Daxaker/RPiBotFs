module Stateful

open Akka.FSharp

type Handler<'TMessage> = 'TMessage -> Instruction<'TMessage>
and Instruction<'TMessage> =
    |Continue
    |Become of (Handler<'TMessage>)
    |Unhandled
    |Stop
    
let statefulActorOf handler (mailbox: Actor<_>) =
    let rec run runHandler = actor {
        let! message = mailbox.Receive()
        
        let next, handled =
            match (runHandler message) with
            |Continue  -> Some(runHandler), true
            |Become (handler') -> Some(handler'), true
            |Unhandled -> Some(runHandler), false
            |Stop -> None, true
        if not <| handled then
            mailbox.Unhandled(message)
        
        match next with
        |None -> 
            mailbox.Context.Stop(mailbox.Self)
            return()
        |Some (handler') ->
            return! run handler'
    }
    
    run handler 
    
   

     