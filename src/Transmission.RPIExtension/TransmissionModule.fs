module TransmissionModule
open System
open System.IO
open Transmission.API.RPC
open Transmission.API.RPC.Entity
open Contracts

let private client =
    lazy (new Client(transmissionAddress))
let GetTorrentListAsync() =
    client.Value.TorrentGetAsync(TorrentFields.ALL_FIELDS) |> Async.AwaitTask
        
let GetTorrentAsync (id) =
    client.Value.TorrentGetAsync ([|TorrentFields.NAME|], [|id|]) |> Async.AwaitTask
       
let GetTorrentsList() =
    client.Value.TorrentGet(TorrentFields.ALL_FIELDS)

let AddTorrentFileAsync filePath =
    async {
        if not(File.Exists(filePath)) then 
            failwith "File not exists"
        else
            use stream = File.OpenRead(filePath)
            let fileBytes:byte[] = Array.zeroCreate (int stream.Length)
            do stream.Read(fileBytes, 0, Convert.ToInt32(stream.Length)) |> ignore
            let encodedData = Convert.ToBase64String(fileBytes)
            let t = NewTorrent(Metainfo = encodedData, Paused = false)
            do! client.Value.TorrentAddAsync(t) |> Async.AwaitTask |> Async.Ignore
    }
let AddTorrentMagnetAsync magnet =
    async{
        let t = NewTorrent(Filename = magnet, Paused = false)
        do! client.Value.TorrentAddAsync(t) |> Async.AwaitTask |> Async.Ignore
    }
