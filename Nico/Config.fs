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
        InternalPath :string
        BasePath : string
        TorrentsPath: string
        DownloadsPath:string
        FastResumeFile:string
        DhtNodeFile:string
    }

module Config =
 
    let private internalPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Nico-11-16-2009")
    let private torrentsPath = Path.Combine(internalPath, "Torrents")
    let defaultDownloadPath = Path.Combine(internalPath, "Downloads")

    let rec getPathValues(downloadPath) =
        {
            InternalPath = internalPath
            BasePath = internalPath
            TorrentsPath = torrentsPath
            DownloadsPath = downloadPath
            FastResumeFile = Path.Combine(torrentsPath, "fastresume.data")
            DhtNodeFile = Path.Combine(internalPath, "DhtNodes")
        }
    
    let createPaths (paths:PathValues) =
        let create path = 
            if not (Directory.Exists(path)) then
                Directory.CreateDirectory(path) |> ignore
        create paths.DownloadsPath
        create paths.TorrentsPath
        create paths.InternalPath

        
type AllSettings =
    { 
      EngineSettings : EngineSettings
      TorrentDefault : TorrentSettings
      Paths : PathValues 
    }

 