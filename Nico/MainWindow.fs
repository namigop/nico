namespace Nico

open FsXaml
open NicoExtensions
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

   
    let allSettings = TorrentClient.setupSettings pathValues.DownloadsPath port
    
        
    let torrentApp = 
        TorrentApp.create 
            port
            (fun a -> statusMessage <- String.Format("Connected to {0} peers : {1}", a.ExistingPeers + a.NewPeers, DateTime.Now))
            (fun b -> statusMessage <- String.Format("Piece hashed : {0}", DateTime.Now))
            (fun c -> statusMessage <- String.Format("Torrent state {0} -> {1} : {2}", c.OldState, c.NewState, DateTime.Now))
            (fun d -> statusMessage <- String.Format("Tracker {0} -> {1} : {2}", d.Tracker.Uri, d.Tracker.Status, DateTime.Now))

    let clientEngineItem = ClientEngineItem(torrentApp.Engine)
    let displayedTorrentManagers = ObservableCollection<TorrentManagerItem>()
        

    let refreshTimer =
        let temp = DispatcherTimer()
        temp.Interval <- TimeSpan.FromMilliseconds(500.0)
        temp.Tick |> Observable.add (fun arg ->
                        this.OnPropertyChanged("StatusMessage")
                        if (torrentApp.AllTorrentCount > 0) then             
                            this.AllTorrentsHeader <- String.Format("All ({0})", torrentApp.AllTorrentCount)              
                            this.PausedTorrentsHeader <- String.Format("Paused ({0})", torrentApp.PausedTorrentManagers |> Seq.length)
                            this.SeedingTorrentsHeader <- String.Format("Seeding ({0})", torrentApp.SeedingTorrentManagers |> Seq.length)
                            this.DownloadingTorrentsHeader <- String.Format("Active ({0})", torrentApp.ActiveTorrentManagers |> Seq.length))
        temp

    let showTorrentManagers mgrs =
        displayedTorrentManagers |> Seq.iter (fun m -> m.StopWatch())
        displayedTorrentManagers.Clear()
        mgrs 
        |> Seq.map(fun mgr -> TorrentManagerItem(mgr, pathValues, fun todo -> () ))
        |> Seq.iter(fun item ->
             displayedTorrentManagers.Add item
             item.StartWatch())


    let loadedCommand = 
        torrentApp.LoadTorrentFiles()
        torrentApp.AllTorrentManagers  |> Seq.iter (fun mgr ->  torrentApp.Start mgr)      
        showTorrentManagers torrentApp.AllTorrentManagers   
        refreshTimer.Start()

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
                    let mgr = torrentApp.AddTorrentManager target
                    torrentApp.Start mgr      
        new RelayCommand((fun c -> true), onRun)

    let selectActiveTorrentsCommand =
        let onRun (arg) = showTorrentManagers torrentApp.ActiveTorrentManagers 
        new RelayCommand((fun c -> true), onRun)

    let selectSeedingTorrentsCommand =
        let onRun (arg) = showTorrentManagers torrentApp.SeedingTorrentManagers 
        new RelayCommand((fun c -> true), onRun)
 
    let selectPausedTorrentsCommand =
        let onRun (arg) = showTorrentManagers torrentApp.PausedTorrentManagers 
        new RelayCommand((fun c -> true), onRun)

    
    let selectAllTorrentsCommand =
        let onRun (arg) = showTorrentManagers torrentApp.AllTorrentManagers 
        new RelayCommand((fun c -> true), onRun)

    let rowClickCommand =
        let onRun (arg : obj) =
            let sender = arg :?> DependencyObject
            if not (sender = null) then
                let ancestor = sender.FindAncestor<DataGridRow>()
                if not (ancestor.IsNone) then ()
        new RelayCommand((fun c -> true), onRun)


    member x.SelectAllTorrentsCommand = selectAllTorrentsCommand
    member x.SelectActiveTorrentsCommand = selectActiveTorrentsCommand
    member x.SelectSeedingTorrentsCommand = selectSeedingTorrentsCommand
    member x.SelectPausedTorrentsCommand = selectPausedTorrentsCommand

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

    member this.Close() = ()
//        let rec forceStop (tor : TorrentManager) retry =
//            if not (tor.State = TorrentState.Stopped) && retry < 10 then
//                Thread.Sleep(250)
//                forceStop tor (retry + 1)
//        clientEngineItem.ClientEngine.Dispose()
//        let fastResume = new BEncodedDictionary()
//        allTorrentManagers
//        |> Seq.map (fun t -> t.TorrentManager)
//        |> Seq.iter (fun tor ->
//               forceStop tor 0
//               fastResume.Add(tor.InfoHash.BEncodedHex(), tor.SaveFastResume().Encode()))
//        //
//        //#if !DISABLE_DHT
//        //            File.WriteAllBytes(dhtNodeFile, engine.DhtEngine.SaveNodes());
//        //#endif
//        File.WriteAllBytes(pathValues.FastResumeFile, fastResume.Encode())
//        System.Threading.Thread.Sleep(2000)