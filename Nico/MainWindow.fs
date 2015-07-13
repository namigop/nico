namespace Nico

open FsXaml
open InfoHashExtension
open MonoTorrent.BEncoding
open MonoTorrent.Client
open MonoTorrent.Client.Encryption
open MonoTorrent.Client.Tracker
open MonoTorrent.Common
open MonoTorrent.Dht
open MonoTorrent.Dht.Listeners
open System
open System.Collections.ObjectModel
open System.Collections.Generic
open System.Threading.Tasks
open System.Diagnostics
open System.IO
open System.Net
open System.Threading
open System.Windows
open System.Windows.Controls
open System.Windows.Media
open System.Windows.Threading
open Microsoft.Win32

type MainWindow = XAML< "MainWindow.xaml", true >

type MainWindowViewModel() as this =
    inherit ViewModelBase()
    do Config.createPaths (Config.getPathValues())
    let mutable allTorrentsHeader = "All"
    let mutable seedingTorrentsHeader = "Seeding"
    let mutable pausedTorrentsHeader = "Paused"
    let mutable downloadingTorrentsHeader = "Active"
    let mutable statusMessage = ""
    let mutable title = ""
    let pathValues = Config.getPathValues()
    let mutable port = 6746

    let rowToggle s =
        let s3 = 2
        ()

    let allSettings = TorrentClient.setupSettings pathValues.DownloadsPath port
   
    let register (clientEngine:ClientEngine) mgr =
        TorrentClient.register 
            clientEngine
            (fun a -> this.StatusMessage <- String.Format("Connected to {0} peers : {1}", a.ExistingPeers + a.NewPeers, DateTime.Now))
            (fun b -> this.StatusMessage <- String.Format("Piece hashed : {0}", DateTime.Now))
            (fun c -> this.StatusMessage <- String.Format("Torrent state {0} -> {1} : {2}", c.OldState, c.NewState, DateTime.Now))
            (fun d -> this.StatusMessage <- String.Format("Tracker {0} -> {1} : {2}", d.Tracker.Uri, d.Tracker.Status, DateTime.Now))
            mgr
        
    let clientEngineItem =
        allSettings.EngineSettings
        |> TorrentClient.setupClientEngine port
        |> fun c -> ClientEngineItem(c)

    let allTorrentManagers = ResizeArray<TorrentManagerItem>()
    let displayedTorrentManagers =
        let temp = ObservableCollection<TorrentManagerItem>()
        for t in allTorrentManagers do
            temp.Add t
        temp

    let timer =
        let temp = DispatcherTimer()
        temp.Interval <- TimeSpan.FromMilliseconds(1000.0)
        temp.Tick |> Observable.add (fun arg ->
                        if (allTorrentManagers.Count > 0) then
                            let torrentStates = allTorrentManagers |> Seq.countBy (fun t -> t.OverallStatus)
                            for (state, count) in torrentStates do
                                if state = OverallStatus.Paused then this.SeedingTorrentsHeader <- String.Format("Paused ({0})", count)
                                if state = OverallStatus.Seeding then this.SeedingTorrentsHeader <- String.Format("Seeding ({0})", count)
                                if state = OverallStatus.Downloading then this.DownloadingTorrentsHeader <- String.Format("Downloading ({0})", count)
                            this.AllTorrentsHeader <- String.Format("All ({0})", allTorrentManagers.Count))
        temp.Start()
        temp

    let start (mgr : TorrentManagerItem) =
        TorrentClient.start mgr.TorrentManager
        mgr.StartWatch()
        //Add mgr to list
        allTorrentManagers.Add(mgr)

    let loadedCommand =     
        let task = Task.Run( new Action(fun () ->
                Thread.Sleep(1000)
                let items = 
                    Directory.GetFiles(pathValues.TorrentsPath, "*.torrent", SearchOption.TopDirectoryOnly) 
                    |> Seq.map (fun torrentFile ->
                            torrentFile
                            |> TorrentClient.createTorrentManager allSettings.TorrentDefault pathValues)
                items))
        let mgr = task.re
//        let items = Directory.GetFiles(pathValues.TorrentsPath, "*.torrent", SearchOption.TopDirectoryOnly) 
//                    |> Seq.map (fun torrentFile ->
//                                torrentFile
//                                |> TorrentClient.createTorrentManager allSettings.TorrentDefault pathValues
//                                |> register clientEngineItem.ClientEngine       
//                                |> fun mgr -> TorrentManagerItem(mgr, pathValues, rowToggle))
//                    |> Seq.iter (fun i -> allTorrentManagers.Add i)
//         
//        allTorrentManagers |> Seq.iter start

    let addTorrentCommand =
        let onRun (arg) =
            //show a folder browser dialog
            let dg = OpenFileDialog(Filter="Torrent Files (*.torrent)|*.torrent", Multiselect = false)
            
            if dg.ShowDialog().Value then
                let torrent = dg.FileName
                let fileName = Path.GetFileName torrent
                let target = Path.Combine(pathValues.TorrentsPath, fileName)
                if not (File.Exists target) then
                    File.Copy(torrent, target)
                    target |> TorrentClient.createTorrentManager allSettings.TorrentDefault pathValues
                           |> register clientEngineItem.ClientEngine       
                           |> fun mgr -> 
                                let item = TorrentManagerItem(mgr, pathValues, rowToggle)
                                allTorrentManagers.Add item
                                start item

                
        new RelayCommand((fun c -> true), onRun)

    let rowClickCommand =
        let onRun (arg : obj) =
            let sender = arg :?> DependencyObject
            if not (sender = null) then
                let ancestor = sender.FindAncestor<DataGridRow>()
                if not (ancestor.IsNone) then ()
        new RelayCommand((fun c -> true), onRun)


    member this.LoadedCommand = loadedCommand
    member this.AddTorrentCommand = addTorrentCommand
    member this.RowClickCommand = rowClickCommand

    member this.Title
        with get () = title
        and set v = this.RaiseAndSetIfChanged(&title, v, "Title")

    member this.TorrentManagers = displayedTorrentManagers

    member this.StatusMessage
        with get () = statusMessage
        and set v = this.RaiseAndSetIfChanged(&statusMessage, v, "StatusMessage")

    member this.DownloadingTorrentsHeader
        with get () = downloadingTorrentsHeader
        and set v = this.RaiseAndSetIfChanged(&downloadingTorrentsHeader, v, "DownloadingTorrentsHeader")

    member this.AllTorrentsHeader
        with get () = allTorrentsHeader
        and set v = this.RaiseAndSetIfChanged(&allTorrentsHeader, v, "AllTorrentsHeader")

    member this.SeedingTorrentsHeader
        with get () = seedingTorrentsHeader
        and set v = this.RaiseAndSetIfChanged(&seedingTorrentsHeader, v, "SeedingTorrentsHeader")

    member this.PausedTorrentsHeader
        with get () = pausedTorrentsHeader
        and set v = this.RaiseAndSetIfChanged(&pausedTorrentsHeader, v, "PausedTorrentsHeader")

    member this.Close() =
        let rec forceStop (tor : TorrentManager) retry =
            if not (tor.State = TorrentState.Stopped) && retry < 10 then
                Thread.Sleep(250)
                forceStop tor (retry + 1)
        clientEngineItem.ClientEngine.Dispose()
        let fastResume = new BEncodedDictionary()
        allTorrentManagers
        |> Seq.map (fun t -> t.TorrentManager)
        |> Seq.iter (fun tor ->
               forceStop tor 0
               fastResume.Add(tor.InfoHash.BEncodedHex(), tor.SaveFastResume().Encode()))
        //
        //#if !DISABLE_DHT
        //            File.WriteAllBytes(dhtNodeFile, engine.DhtEngine.SaveNodes());
        //#endif
        File.WriteAllBytes(pathValues.FastResumeFile, fastResume.Encode())
        System.Threading.Thread.Sleep(2000)