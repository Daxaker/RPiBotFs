namespace Contracts 

open System.Collections.Generic
open Telegram.Bot
open Telegram.Bot.Args
open Telegram.Bot.Types
open Telegram.Bot.Types.ReplyMarkups

[<AutoOpen>]
module TelegramModule =

    let client =
        lazy(new TelegramBotClient(botKey.Value))
    
    let getCallbackButton caption response =
        [|InlineKeyboardButton.WithCallbackData(caption, response)|] :> IEnumerable<InlineKeyboardButton>
    
    let sendMessageMarkup markup (chatId:int64) message =
        client.Value.SendTextMessageAsync(Types.ChatId chatId, message, Types.Enums.ParseMode.Default,false, false, 0, markup) |> Async.AwaitTask |> Async.Ignore
        
    let sendChatMessageMarkup markup (arg: MessageEventArgs) =
        sendMessageMarkup markup arg.Message.Chat.Id
    
    let sendChatMessage arg =
        sendChatMessageMarkup null arg
        