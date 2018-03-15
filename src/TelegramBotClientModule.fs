module TelegramBotClientModule

open System.Collections.Generic
open Telegram.Bot
open Telegram.Bot.Args
open Telegram.Bot.Types
open Telegram.Bot.Types.ReplyMarkups
open Settings

let client =
    lazy(new TelegramBotClient(botKey))

let getCallbackButton (torrentInfo: Transmission.API.RPC.Entity.TorrentInfo) =
    [|InlineKeyboardButton.WithCallbackData(torrentInfo.Name, torrentInfo.ID.ToString())|] :> IEnumerable<InlineKeyboardButton>

let sendMessageMarkup markup (chatId:int64) message =
    client.Value.SendTextMessageAsync(Types.ChatId chatId, message, Types.Enums.ParseMode.Default,false, false, 0, markup) |> Async.AwaitTask |> Async.Ignore
    
let sendChatMessageMarkup markup (arg: MessageEventArgs) =
    sendMessageMarkup markup arg.Message.Chat.Id

let sendChatMessage arg =
    sendChatMessageMarkup null arg
        