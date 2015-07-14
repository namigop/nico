namespace Nico

open System
open System.Net
open System.IO
open MonoTorrent.BEncoding
open MonoTorrent.Client
open MonoTorrent.Client.Encryption
open MonoTorrent.Client.Tracker
open MonoTorrent.Common
open MonoTorrent.Dht
open MonoTorrent.Dht.Listeners
open NicoExtensions

type PathValues =
    {
        BasePath : string
        TorrentsPath: string
        DownloadsPath:string
        FastResumeFile:string
        DhtNodeFile:string
    }

module Config =

    let private basePath = fun () -> Environment.CurrentDirectory
    let private torrentsPath = Path.Combine(basePath(), "Torrents")

    let rec getPathValues() =
        {
            BasePath = basePath()
            TorrentsPath = torrentsPath
            DownloadsPath = Path.Combine(basePath(), "Downloads")
            FastResumeFile = Path.Combine(torrentsPath, "fastresume.data")
            DhtNodeFile = Path.Combine(basePath(), "DhtNodes")
        }
    
    let createPaths (paths:PathValues) =
        let create path = 
            if not (Directory.Exists(path)) then
                Directory.CreateDirectory(path) |> ignore
        create paths.DownloadsPath
        create paths.TorrentsPath

        
type AllSettings =
    { 
      EngineSettings : EngineSettings
      TorrentDefault : TorrentSettings
      Paths : PathValues 
    }

 