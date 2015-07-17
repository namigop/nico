namespace Nico

open NicoExtensions
open MonoTorrent.BEncoding
open MonoTorrent.Client
open MonoTorrent.Client.Encryption
open MonoTorrent.Client.Tracker
open MonoTorrent.Common
open MonoTorrent.Dht
open MonoTorrent.Dht.Listeners
open System
open System.Windows.Threading
open System.Collections.ObjectModel

type TorrentManagerItem( xmlDownloadInfo :TorrentDownloadInfo, manager : TorrentManager, paths:PathValues, onToggled: obj -> unit) as this =
    inherit ViewModelBase()
    let mutable progress = Convert.ToDouble(xmlDownloadInfo.Progress)
    let mutable peersHeader = ""
    let mutable downloadSpeed = ""
    let mutable downloadSizeMB = ""
    let mutable uploadSizeMB = ""
    let mutable uploadSpeed = ""
    let mutable state = xmlDownloadInfo.State.ToString()
    let mutable ratio = xmlDownloadInfo.Ratio
    let mutable selectedTabIndex = 0
    let mutable overallStatus = xmlDownloadInfo.State
   // let xmlDownloadInfo = TorrentDownloadInfo(PhysicalTorrentFile = physicalTorrentFile)

    let toggledCommand =      
        new RelayCommand((fun c -> true), onToggled)
        
    let torrentFiles =
        let items = 
            manager.Torrent.Files 
            |> Seq.sortBy (fun t -> t.FullPath)
            |> Seq.map (fun f -> TorrentFileItem(f,paths.DownloadsPath))
        new ObservableCollection<TorrentFileItem>(items)

    let updateXmlInfo() =
        xmlDownloadInfo.BytesDownloaded <- manager.Monitor.DataBytesDownloaded
        xmlDownloadInfo.BytesUploaded <- manager.Monitor.DataBytesUploaded
        xmlDownloadInfo.Progress <-Convert.ToInt32(manager.Progress)
        xmlDownloadInfo.DownloadDuration <-
            if xmlDownloadInfo.Progress < 100 then
                DateTime.Now - xmlDownloadInfo.DownloadStartDate
            else
                xmlDownloadInfo.DownloadDuration
        
        xmlDownloadInfo.State <-  this.OverallStatus
        xmlDownloadInfo.Save(paths.InternalPath)
      
    let updateDownloadStat() =
        this.Progress <- Math.Round(manager.Progress, 2)
        this.State <- manager.State.ToString()
        this.OverallStatus <-
            match manager.State with
            | TorrentState.Downloading -> OverallStatus.Downloading
            | TorrentState.Seeding -> OverallStatus.Seeding
            | TorrentState.Paused -> OverallStatus.Paused
            | _ -> OverallStatus.Others

        this.DownloadSpeed <- String.Format("{0:0.00} KB/s", Convert.ToDouble(manager.Monitor.DownloadSpeed) / 1024.0)
        this.UploadSpeed <- String.Format("{0:0.00} KB/s", Convert.ToDouble(manager.Monitor.UploadSpeed) / 1024.0)
        this.DownloadSizeMB <- String.Format("{0:0.00} MB", Convert.ToDouble(manager.Monitor.DataBytesDownloaded) / (1024.0 * 1024.0))
        this.UploadSizeMB <- String.Format(" {0:0.00} MB", Convert.ToDouble(manager.Monitor.DataBytesUploaded) / (1024.0 * 1024.0))
        this.Ratio <-
            if manager.Monitor.DataBytesDownloaded > 0L then    
                let uploaded = Convert.ToDouble(manager.Monitor.DataBytesUploaded)         
                let downloaded = Convert.ToDouble(manager.Monitor.DataBytesDownloaded)                 
                Math.Round(uploaded/downloaded, 3)
            else
                0.0

    let allPeers = new ObservableCollection<PeerId>()
    let updatePeersStat() =
        let peers = manager.GetPeers()
        this.PeersHeader <- String.Format("Peers ({0})", peers.Count)
        for p in peers do
            let getPeer(p2:PeerId) =
                allPeers |> Seq.tryFind (fun p -> p.PeerID = p2.PeerID)
            match getPeer p with
            | Some(p3) -> ()
            | None -> allPeers.Add(p)
 
   
    let timer =
        let finalInterval = TimeSpan.FromMilliseconds(750.0)
        let temp = DispatcherTimer(Tag = 0)

        temp.Interval <- TimeSpan.FromMilliseconds(100.0)
        temp.Tick |> Observable.add (fun arg ->
                        updateDownloadStat()                       
                        updatePeersStat()
                        
                        for t in torrentFiles do
                           t.UpdateProgress()

                        let counter = Convert.ToInt32(temp.Tag)
                        if (counter % 2 = 0) then
                            updateXmlInfo()
                        temp.Tag <- counter + 1
                         )
        temp

    member x.TorrentXmlInfo = xmlDownloadInfo
    member x.OverallStatus 
        with get () = overallStatus
        and set v = this.RaiseAndSetIfChanged(&overallStatus, v, "OverallStatus")
   
    member x.FilesHeader = String.Format("Files ({0})", torrentFiles.Count)
    member x.PeersHeader 
        with get () = peersStartWatchHeader
        and set v = this.RaiseAndSetIfChanged(&peersHeader, v, "PeersHeader")
   
    
    member x.AllPeers = allPeers
    member x.TorrentFiles = torrentFiles    
    member x.TorrentManager = manager
    member this.Name = manager.Torrent.Name
    // member this.Size = size
    member this.() = 
        xmlDownloadInfo.DownloadStartDate <- DateTime.Now      
        timer.Start()
    member this.StopWatch() = timer.Stop()

    member this.State
        with get () = state
        and set v = this.RaiseAndSetIfChanged(&state, v, "State")
   
    member this.SelectedTabIndex
        with get () = selectedTabIndex
        and set v = this.RaiseAndSetIfChanged(&selectedTabIndex, v, "SelectedTabIndex")

    member x.SelectFilesCommand = new RelayCommand((fun d -> true), fun _ -> this.SelectedTabIndex <- 0)
    member x.SelectPeersCommand = new RelayCommand((fun d -> true), fun _ -> this.SelectedTabIndex <- 1)

    member this.Progress
        with get () = progress
        and set v = this.RaiseAndSetIfChanged(&progress, v, "Progress")

    member this.UploadSpeed
        with get () = uploadSpeed
        and set v = this.RaiseAndSetIfChanged(&uploadSpeed, v, "UploadSpeed")

    member this.DownloadSpeed
        with get () = downloadSpeed
        and set v = this.RaiseAndSetIfChanged(&downloadSpeed, v, "DownloadSpeed")

    member this.DownloadSizeMB
        with get () = downloadSizeMB
        and set v = this.RaiseAndSetIfChanged(&downloadSizeMB, v, "DownloadSizeMB")

    member this.UploadSizeMB
        with get () = uploadSizeMB
        and set v = this.RaiseAndSetIfChanged(&uploadSizeMB, v, "UploadSizeMB")

    member this.Ratio
        with get () = ratio
        and set v = this.RaiseAndSetIfChanged(&ratio, v, "Ratio")