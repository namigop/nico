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
open System.Collections.Generic
open System.Collections.ObjectModel
open OxyPlot
open OxyPlot.Annotations
open OxyPlot.Axes
open OxyPlot.Series
open System.IO
open System.Diagnostics

type TorrentManagerViewModel( xmlDownloadInfo :TorrentDownloadInfo, manager : TorrentManager, paths:PathValues) as this =
    inherit ViewModelBase()
    let mutable progress = Convert.ToDouble(xmlDownloadInfo.Progress)
    let mutable peersHeader = "Peers"
    let mutable downloadSpeed = ""
    let mutable downloadSizeMB = ""
    let mutable uploadSizeMB = ""
    let mutable uploadSpeed = ""
    let mutable state = xmlDownloadInfo.State.ToString()
    let mutable ratio = xmlDownloadInfo.Ratio
    let mutable selectedTabIndex = 0
    let mutable overallStatus = xmlDownloadInfo.State
    let speedPlot = SpeedPlot.create()
     
    let torrentFiles =    new ObservableCollection<TorrentFileViewModel>()
     
    let getTorrentFiles() = 
        if xmlDownloadInfo.Files.Count > 0 then
            let items = xmlDownloadInfo.Files |> Seq.map(fun file -> TorrentFileViewModel(NicoTorrentFile.createFromTorFileInfo file, paths.DownloadsPath))
            if not (torrentFiles.Count = Seq.length items)  then
                torrentFiles.Clear()
                items |> Seq.iter(fun t -> torrentFiles.Add t)
        
    let updateFiles() =
        if (Utils.isNotNull manager.Torrent) then
            if not(String.IsNullOrWhiteSpace(xmlDownloadInfo.PhysicalTorrentFile)) && not (File.Exists xmlDownloadInfo.PhysicalTorrentFile) then
                ()foomagnet
            xmlDownloadInfo.PhysicalTorrentFile <- Path.Combine(manager.Torrent.TorrentPath, manager.Torrent.Name)
            if not(manager.Torrent.Files.Length = torrentFiles.Count) then
                let items = 
                    manager.Torrent.Files 
                    |> Seq.sortBy (fun t -> t.FullPath)
                    |> Seq.map (fun f -> NicoTorrentFile.createFromTorFile f)
                    |> Seq.map (fun fileInfo -> TorrentFileInfo(fileInfo.FullPath, fileInfo.Priority, fileInfo.Progress, fileInfo.SizeInMB))
        
                if (Seq.length items) > 0 then
                    xmlDownloadInfo.Files.Clear()
                    xmlDownloadInfo.Files.AddRange items
            else
                //Update the progress
                let keyPairs = 
                    manager.Torrent.Files 
                    |> Seq.fold (fun (acc:Dictionary<string, float>) i -> 
                        let path:string = i.Path
                        acc.[path] <- 0.0
                        acc ) (Dictionary<string, float>())
                for f in xmlDownloadInfo.Files do
                    f.Progress <- keyPairs.[Path.GetFileName(f.FullPath)]


    let updateXmlInfo() =
        xmlDownloadInfo.BytesDownloaded <- 
            if xmlDownloadInfo.BytesDownloaded > manager.Monitor.DataBytesDownloaded then
                xmlDownloadInfo.BytesDownloaded 
            else
                manager.Monitor.DataBytesDownloaded

        xmlDownloadInfo.BytesUploaded <-  xmlDownloadInfo.BytesUploaded + manager.Monitor.DataBytesUploaded
        xmlDownloadInfo.Progress <-Convert.ToInt32(manager.Progress)
        xmlDownloadInfo.DownloadDuration <-
            if xmlDownloadInfo.Progress < 100 then
                DateTime.Now - xmlDownloadInfo.DownloadStartDate
            else
                xmlDownloadInfo.DownloadDuration
        
        updateFiles()
        xmlDownloadInfo.State <-  this.OverallStatus
        xmlDownloadInfo.Save(paths.InternalPath)
      
    let updateDownloadStat() =
        this.Progress <-
            let curProgress = Math.Round(manager.Progress, 2)
            if curProgress < this.Progress then this.Progress else curProgress

        this.State <- manager.State.ToString()
        this.OverallStatus <-
            match manager.State with
            | TorrentState.Downloading -> OverallStatus.Downloading
            | TorrentState.Seeding -> OverallStatus.Seeding
            | TorrentState.Paused -> OverallStatus.Paused
            | TorrentState.Stopped -> OverallStatus.Stopped
            | TorrentState.Error -> OverallStatus.Error
            | _ ->
                if (xmlDownloadInfo.Progress = 100) then
                    OverallStatus.Completed
                else
                    xmlDownloadInfo.State
        let downloadSpeedInKB = Convert.ToDouble(manager.Monitor.DownloadSpeed) / 1024.0;
        let uploadSpeedInKB = Convert.ToDouble(manager.Monitor.UploadSpeed) / 1024.0;
     
        this.DownloadSpeed <- String.Format("{0:0.00} KB/s", downloadSpeedInKB)
        this.UploadSpeed <- String.Format("{0:0.00} KB/s",  uploadSpeedInKB)
        this.DownloadSizeMB <- String.Format("{0:0.00} MB", Convert.ToDouble(manager.Monitor.DataBytesDownloaded) / (1024.0 * 1024.0))
        this.UploadSizeMB <- String.Format(" {0:0.00} MB", Convert.ToDouble(manager.Monitor.DataBytesUploaded) / (1024.0 * 1024.0))
        this.Ratio <-
            if  xmlDownloadInfo.BytesDownloaded > 0L && xmlDownloadInfo.BytesUploaded > 0L then    
                let uploaded = xmlDownloadInfo.BytesUploaded.ToDouble()        
                let downloaded =  xmlDownloadInfo.BytesDownloaded.ToDouble()                
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
        temp.Tick 
            |> Observable.add (fun arg ->
                updateDownloadStat()                       
                updatePeersStat()                     
                for t in torrentFiles do
                    t.UpdateProgress()

                let counter = Convert.ToInt32(temp.Tag)
                if (counter % 2 = 0) then
                    let downloadSpeedInKB = Convert.ToDouble(manager.Monitor.DownloadSpeed) / 1024.0;
                    let uploadSpeedInKB = Convert.ToDouble(manager.Monitor.UploadSpeed) / 1024.0;
                    speedPlot.AddDownloadPerSecPoint(DatePoint(Y=downloadSpeedInKB))
                    speedPlot.AddUploadPerSecPoint(DatePoint(Y=uploadSpeedInKB))
                    getTorrentFiles()
                    updateXmlInfo()

                temp.Tag <- counter + 1)
        temp
        
    let openInExplorerCommand =
        let onRun (arg) =
            torrentFiles 
            |> Seq.head
            |> fun t -> 
                let downloadedAt = Path.GetDirectoryName(t.FileFullPath)
                Process.Start("explorer.exe", downloadedAt) |> ignore
        new RelayCommand((fun c -> true), onRun)

    member this.OpenInExplorerCommand = openInExplorerCommand
    member x.TorrentXmlInfo = xmlDownloadInfo
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
    member this.Name =  xmlDownloadInfo.Name
    member this.StartWatch() = 
        if (xmlDownloadInfo.DownloadStartDate > DateTime.Now) then
            xmlDownloadInfo.DownloadStartDate <- DateTime.Now      
        updateFiles()
        timer.Start()
    member this.StopWatch() = timer.Stop()

    member this.State
        with get () = state
        and set v = this.RaiseAndSetIfChanged(&state, v, "State")
   
    member this.SelectedTabIndex
        with get () = selectedTabIndex
        and set v = this.RaiseAndSetIfChanged(&selectedTabIndex, v, "SelectedTabIndex")

    member x.SelectDetailsCommand = new RelayCommand((fun d -> true), fun _ -> this.SelectedTabIndex <- 2)
    member x.SelectFilesCommand = new RelayCommand((fun d -> true), fun _ -> this.SelectedTabIndex <- 0)
    member x.SelectPeersCommand = new RelayCommand((fun d -> true), fun _ -> this.SelectedTabIndex <- 1)
    member x.DownloadPlotModel = speedPlot.DownloadModel
    member x.UploadPlotModel = speedPlot.UploadModel
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