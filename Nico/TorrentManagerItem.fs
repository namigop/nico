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

type OverallStatus =
    | Paused = 0
    | Downloading = 1
    | Seeding =2
    | Others = 3

type TorrentManagerItem(manager : TorrentManager, paths:PathValues, onToggled: obj -> unit) as this =
    inherit ViewModelBase()
    let mutable progress = 0.0
    let mutable peersHeader = ""
    let mutable downloadSpeed = ""
    let mutable downloadSizeMB = ""
    let mutable uploadSizeMB = ""
    let mutable uploadSpeed = ""
    let mutable state = ""
    let mutable ratio = 0.0
    let mutable selectedTabIndex = 0
    let mutable overallStatus = OverallStatus.Others

    let toggledCommand =      
        new RelayCommand((fun c -> true), onToggled)
        
    let torrentFiles =
        let items = 
            manager.Torrent.Files 
            |> Seq.sortBy (fun t -> t.FullPath)
            |> Seq.map (fun f -> TorrentFileItem(f,paths.DownloadsPath))
        new ObservableCollection<TorrentFileItem>(items)

    let allPeers = new ObservableCollection<PeerId>()
    let timer =
        let finalInterval = TimeSpan.FromMilliseconds(500.0)
        let temp = DispatcherTimer()
        temp.Interval <- TimeSpan.FromMilliseconds(100.0)
        temp.Tick |> Observable.add (fun arg ->
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
                        for t in torrentFiles do
                           t.UpdateProgress()

                        let peers = manager.GetPeers()
                        this.PeersHeader <- String.Format("Peers ({0})", peers.Count)
                        for p in peers do
                            let getPeer(p2:PeerId) =
                                allPeers |> Seq.tryFind (fun p -> p.PeerID = p2.PeerID)
                            match getPeer p with
                            | Some(p3) -> ()
                            | None -> allPeers.Add(p)

                        //rampup the initial refresh
                        if temp.Interval < finalInterval then
                             temp.Interval <- temp.Interval.Add(TimeSpan.FromMilliseconds(50.0))
                             temp.Stop()
                             temp.Start()
                        else
                            temp.Interval <- finalInterval
                            temp.Stop()
                            temp.Start()

                         )
        temp

    member x.OverallStatus 
        with get () = overallStatus
        and set v = this.RaiseAndSetIfChanged(&overallStatus, v, "OverallStatus")
   
    member x.FilesHeader = String.Format("Files ({0})", torrentFiles.Count)
    member x.PeersHeader 
        with get () = peersHeader
        and set v = this.RaiseAndSetIfChanged(&peersHeader, v, "PeersHeader")
   
    
    member x.AllPeers = allPeers
    member x.TorrentFiles = torrentFiles    
    member x.TorrentManager = manager
    member this.Name = manager.Torrent.Name
    // member this.Size = size
    member this.StartWatch() = timer.Start()
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