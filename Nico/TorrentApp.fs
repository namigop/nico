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
    abstract Register : TorrentManagerViewModel -> TorrentManagerViewModel
    abstract AllTorrentCount : int
    abstract AllTorrentManagers : (TorrentManagerViewModel) seq 
    abstract ActiveTorrentManagers :(TorrentManagerViewModel) seq
    abstract SeedingTorrentManagers :(TorrentManagerViewModel) seq
    abstract PausedTorrentManagers :(TorrentManagerViewModel) seq
    abstract Start : TorrentManagerViewModel  -> unit
    abstract LoadTorrentFiles : (unit) -> unit
    abstract Engine : ClientEngine
    abstract AddTorrentManager : string -> TorrentManagerViewModel
    abstract AddTorrentManagerFromMagnet : string -> TorrentManagerViewModel
    abstract Stop : TorrentManagerViewModel -> unit
    abstract Pause :TorrentManagerViewModel -> unit

 module TorrentApp =
    open MonoTorrent
    
    let create port onPeersFound onPieceHashed onTorrentStateChanged onAnnounceComplete =
        
        let allTorrentManagers = ResizeArray<TorrentManagerViewModel>()
        let pathValues = Config.getPathValues()
        let allSettings = TorrentClient.setupSettings pathValues.DownloadsPath port
   
        let getValidTorrents() =
            let files = Directory.GetFiles(pathValues.InternalPath, "*tor.xml")
            files 
            |> Seq.map (fun file -> 
                let info:TorrentDownloadInfo = file|> Utils.fileToString |> Utils.deserialize
                info)
            |> Seq.filter (fun info -> info.IsValid)
           
        let loadTorrents (paths:PathValues) torrentSettings (list: ResizeArray<TorrentManagerViewModel>) =           
            Directory.GetFiles(paths.InternalPath, "*" + TorrentDownloadInfo.Extension, SearchOption.TopDirectoryOnly) 
            |> Seq.choose (fun xmlFile ->
                let info :TorrentDownloadInfo = xmlFile |> Utils.fileToString |> Utils.deserialize 
                if info.IsValid then Some(info) else None
                )
            |> Seq.map (fun torrentInfo -> 
                let torrentMgr =
                    if torrentInfo.IsUsinMagnetLink then
                        let magnet = torrentInfo.MagnetLink
                        TorrentClient.createTorrentManagerFromMagnet torrentSettings paths (MagnetLink(magnet))
                    else
                        let torrentFile = torrentInfo.PhysicalTorrentFile
                        TorrentClient.createTorrentManager torrentSettings paths torrentFile
                let mgrItem = TorrentManagerViewModel(torrentInfo, torrentMgr, pathValues)
                mgrItem)
            |> Seq.iter (fun i -> list.Add i)

        let engine = TorrentClient.setupClientEngine port allSettings.EngineSettings      

        {
            new ITorrentApp with
                member x.Engine = engine
                member x.AllTorrentCount = allTorrentManagers.Count
                member x.AddTorrentManager torrentFile = 
                    let mgr = TorrentClient.createTorrentManager allSettings.TorrentDefault pathValues torrentFile
                    let xmlDownloadInfo = TorrentDownloadInfo(PhysicalTorrentFile = torrentFile)
                    let mgrItem = TorrentManagerViewModel(xmlDownloadInfo, mgr, pathValues)
                    allTorrentManagers.Add (mgrItem)
                    mgrItem
                member x.AddTorrentManagerFromMagnet magnetLinkUrl =
                    let magnetLink = new MagnetLink(magnetLinkUrl)
                    let mgr = TorrentClient.createTorrentManagerFromMagnet allSettings.TorrentDefault pathValues magnetLink
                    let xmlDownloadInfo = TorrentDownloadInfo(MagnetLink = magnetLinkUrl)
                    let mgrItem = TorrentManagerViewModel(xmlDownloadInfo, mgr, pathValues)
                    allTorrentManagers.Add (mgrItem)
                    mgrItem
                member x.Register mgr = 
                    TorrentClient.register engine onPeersFound onPieceHashed onTorrentStateChanged onAnnounceComplete mgr.TorrentManager
                    mgr
                member x.AllTorrentManagers = seq { for a in allTorrentManagers do yield a }
                member x.Start mgr =  
                    TorrentClient.start mgr.TorrentManager
                    mgr.TorrentXmlInfo.Save(pathValues.InternalPath)
                    mgr.StartWatch()
                     
                member x.SeedingTorrentManagers = 
                    x.AllTorrentManagers 
                    |> Seq.choose (fun (t) -> 
                        match t.TorrentManager.State with 
                        | TorrentState.Seeding -> Some (t)
                        | _ -> None) 
                member x.PausedTorrentManagers = 
                    x.AllTorrentManagers 
                    |> Seq.choose (fun (t) -> 
                        match t.TorrentManager.State with 
                        | TorrentState.Paused -> Some (t)
                        | _ -> None) 
                member x.ActiveTorrentManagers = 
                    x.AllTorrentManagers 
                    |> Seq.choose (fun (t) -> 
                        match t.TorrentManager.State with 
                        | TorrentState.Downloading -> Some (t)
                        | _ -> None) 
                member x.LoadTorrentFiles()  =
                    loadTorrents pathValues allSettings.TorrentDefault allTorrentManagers
                    allTorrentManagers 
                    |> Seq.iter (fun (mgr) ->
                        (x.Register mgr) |> ignore
                        match mgr.TorrentXmlInfo.State with
                        | OverallStatus.Downloading | OverallStatus.Seeding -> x.Start mgr
                        | OverallStatus.Paused -> x.Pause mgr
                        | _ -> () )
                    
                member x.Stop mgr =  TorrentClient.stop mgr.TorrentManager x.Engine
                member x.Pause mgr =  TorrentClient.pause  mgr.TorrentManager   

        }


     