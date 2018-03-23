namespace TransmissionService

open System
open System.IO
open Telegram.Bot.Args
open Telegram.Bot.Types
open Telegram.Bot.Types.ReplyMarkups
open TransmissionModule
open Contracts

[<AutoOpen>]
module TransmissionService =

    let torrentsList args = async {
        try
            let! torrents = GetTorrentListAsync()
            let callbackResults = 
                torrents.Torrents |> Array.map (fun t -> getCallbackButton t.Name <| string t.ID)
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
            Reset
        |_ -> UnhandledWithReset
    
    let addTorrent args cmd = 
        async {
            do! sendChatMessage args "Waiting for torrent"
        } |> Async.Start
        cmd
    
type TransmissionListnerService() =
    interface IListenerService with
        member this.GetListeners command args =
            match command with
            |"/torrents" -> SingleCommand(torrentsList args)
            |"/active" -> SingleCommand(activeTorrents args)
            |"/addtorrent" -> ChangeState(waitTorrentState) |> addTorrent args
            |_ -> RawData(args)  
            
