module Contracts

open Telegram.Bot.Args

type Handler<'TMessage> = 'TMessage -> Instruction<'TMessage>
and Instruction<'TMessage> =
    |Continue
    |Become of (Handler<'TMessage>)
    |UnhandledWithReset
    |Unhandled
    |Reset
    |Stop
    
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

type IListenerService =
     abstract member GetListeners: string -> MessageEventArgs -> Command
