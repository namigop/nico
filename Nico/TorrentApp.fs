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
 
 type ITorrentApp =
    abstract Register : TorrentManager -> unit
    abstract AllTorrentCount : int
    abstract AllTorrentManagers :TorrentManager seq
    abstract ActiveTorrentManagers :TorrentManager seq
    abstract SeedingTorrentManagers :TorrentManager seq
    abstract PausedTorrentManagers :TorrentManager seq
    abstract Start : TorrentManager -> unit
    abstract LoadTorrentFiles : (unit) -> unit
    abstract Engine : ClientEngine
    abstract AddTorrentManager : string -> TorrentManager
    abstract Stop : TorrentManager -> unit

 module TorrentApp =
    
    let create port onPeersFound onPieceHashed onTorrentStateChanged onAnnounceComplete =
        
        let allTorrentManagers = ResizeArray<TorrentManager>()
        let pathValues = Config.getPathValues()
        let allSettings = TorrentClient.setupSettings pathValues.DownloadsPath port
   
        let getValidTorrents() =
           
        let loadTorrents (paths:PathValues) torrentSettings (list: ResizeArray<TorrentManager>) =           
            Directory.GetFiles(paths.TorrentsPath, "*.torrent", SearchOption.TopDirectoryOnly) 
            |> Seq.map (fun torrentFile -> TorrentClient.createTorrentManager torrentSettings paths torrentFile)
            |> Seq.iter (fun i -> list.Add i)
        
        let engine = TorrentClient.setupClientEngine port allSettings.EngineSettings      

        {
            new ITorrentApp with
                member x.Engine = engine
                member x.AllTorrentCount = allTorrentManagers.Count
                member x.AddTorrentManager torrentFile = 
                    let mgr = TorrentClient.createTorrentManager allSettings.TorrentDefault pathValues torrentFile
                    allTorrentManagers.Add mgr
                    mgr
                member x.Register mgr = TorrentClient.register engine onPeersFound onPieceHashed onTorrentStateChanged onAnnounceComplete mgr
                member x.AllTorrentManagers = seq { for a in allTorrentManagers do yield a }
                member x.Start mgr =  TorrentClient.start mgr
                   
                     
                member x.SeedingTorrentManagers = x.AllTorrentManagers |> Seq.filter (fun t -> t.State = TorrentState.Seeding)
                member x.PausedTorrentManagers = x.AllTorrentManagers |> Seq.filter (fun t -> t.State = TorrentState.Paused)
                member x.ActiveTorrentManagers = x.AllTorrentManagers |> Seq.filter (fun t -> t.State = TorrentState.Downloading)
                member x.LoadTorrentFiles()  =
                    loadTorrents pathValues allSettings.TorrentDefault allTorrentManagers
                    allTorrentManagers |> Seq.iter (fun mgr -> x.Register mgr )
                    
                member x.Stop mgr =  TorrentClient.stop mgr x.Engine

        }


     