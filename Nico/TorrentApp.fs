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
    abstract Register : TorrentManager -> TorrentManager
    abstract AllTorrentCount : int
    abstract AllTorrentManagers : (TorrentDownloadInfo * TorrentManager) seq 
    abstract ActiveTorrentManagers :(TorrentDownloadInfo * TorrentManager) seq
    abstract SeedingTorrentManagers :(TorrentDownloadInfo * TorrentManager) seq
    abstract PausedTorrentManagers :(TorrentDownloadInfo * TorrentManager) seq
    abstract Start : TorrentManager -> unit
    abstract LoadTorrentFiles : (unit) -> unit
    abstract Engine : ClientEngine
    abstract AddTorrentManager : string -> TorrentManager
    abstract Stop : TorrentManager -> unit
    abstract Pause :TorrentManager -> unit

 module TorrentApp =
    
    let create port onPeersFound onPieceHashed onTorrentStateChanged onAnnounceComplete =
        
        let allTorrentManagers = ResizeArray<TorrentDownloadInfo*TorrentManager>()
        let pathValues = Config.getPathValues()
        let allSettings = TorrentClient.setupSettings pathValues.DownloadsPath port
   
        let getValidTorrents() =
            let files = Directory.GetFiles(pathValues.InternalPath, "*tor.xml")
            files 
            |> Seq.map (fun file -> 
                let info:TorrentDownloadInfo = file|> Utils.fileToString |> Utils.deserialize
                info)
            |> Seq.filter (fun info -> info.IsValid)
           
        let loadTorrents (paths:PathValues) torrentSettings (list: ResizeArray<TorrentDownloadInfo*TorrentManager>) =           
            Directory.GetFiles(paths.InternalPath, "*" + TorrentDownloadInfo.Extension, SearchOption.TopDirectoryOnly) 
            |> Seq.choose (fun xmlFile ->
                let info :TorrentDownloadInfo = xmlFile |> Utils.fileToString |> Utils.deserialize 
                if info.IsValid then Some(info) else None
                )
            |> Seq.map (fun torrentInfo -> 
                let torrentFile = torrentInfo.PhysicalTorrentFile
                let mgr = TorrentClient.createTorrentManager torrentSettings paths torrentFile
                torrentInfo, mgr)
            |> Seq.iter (fun i -> list.Add i)
        
        let engine = TorrentClient.setupClientEngine port allSettings.EngineSettings      

        {
            new ITorrentApp with
                member x.Engine = engine
                member x.AllTorrentCount = allTorrentManagers.Count
                member x.AddTorrentManager torrentFile = 
                    let mgr = TorrentClient.createTorrentManager allSettings.TorrentDefault pathValues torrentFile
                    let xmlDownloadInfo = TorrentDownloadInfo(PhysicalTorrentFile = torrentFile)
                    allTorrentManagers.Add (xmlDownloadInfo,mgr)
                    mgr
                member x.Register mgr = 
                    TorrentClient.register engine onPeersFound onPieceHashed onTorrentStateChanged onAnnounceComplete mgr
                    mgr
                member x.AllTorrentManagers = seq { for a in allTorrentManagers do yield a }
                member x.Start mgr =  TorrentClient.start mgr
                   
                     
                member x.SeedingTorrentManagers = 
                    x.AllTorrentManagers 
                    |> Seq.choose (fun (xmlInfo,t) -> 
                        match t.State with 
                        | TorrentState.Seeding -> Some (xmlInfo ,t)
                        | _ -> None) 
                member x.PausedTorrentManagers = 
                    x.AllTorrentManagers 
                    |> Seq.choose (fun (xmlInfo,t)  -> 
                        match t.State with 
                        | TorrentState.Paused -> Some (xmlInfo,t)
                        | _ -> None) 
                member x.ActiveTorrentManagers = 
                    x.AllTorrentManagers 
                    |> Seq.choose (fun (xmlInfo,t)  -> 
                        match t.State with 
                        | TorrentState.Downloading -> Some (xmlInfo,t)
                        | _ -> None) 
                member x.LoadTorrentFiles()  =
                    loadTorrents pathValues allSettings.TorrentDefault allTorrentManagers
                    allTorrentManagers 
                    |> Seq.iter (fun (info,mgr) ->
                        (x.Register mgr) |> ignore
                        match info.State with
                        | OverallStatus.Downloading | OverallStatus.Seeding -> mgr.Start()
                        | OverallStatus.Paused -> mgr.Pause()
                        | _ -> () )
                    
                member x.Stop mgr =  TorrentClient.stop mgr x.Engine
                member x.Pause mgr =  TorrentClient.pause  mgr   

        }


     