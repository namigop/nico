namespace Nico

open FsXaml
open NicoExtensions
open MonoTorrent
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
open Nico.Cs

type MainWindow = XAML< "MainWindow.xaml", true >

type MainViewDisplay =
| All = 0
| Active =1
| Seeding = 2
| Paused =3

type MainWindowViewModel() as this =
    inherit ViewModelBase()
    //do Config.createPaths (Config.getPathValues())
    let mutable allTorrentsHeader = "All"
    let mutable seedingTorrentsHeader = "Seeding"
    let mutable pausedTorrentsHeader = "Paused"
    let mutable downloadingTorrentsHeader = "Active"
    let mutable statusMessage = ""
    let mutable title = ""
    let pathValues = Config.getPathValues(Config.defaultDownloadPath)
    let mutable port = 6746
    let mutable selectedTorrentManager = Unchecked.defaultof<TorrentManagerViewModel>
    let mutable mainViewDisplay = MainViewDisplay.All

    let allSettings downloadPath = TorrentClient.setupSettings downloadPath port

    let torrentApp =
        TorrentApp.create
            port
            (fun a -> statusMessage <- String.Format("Connected to {0} peers : {1}", a.ExistingPeers + a.NewPeers, DateTime.Now))
            (fun b -> statusMessage <- String.Format("Piece hashed : {0}", DateTime.Now))
            (fun c -> statusMessage <- String.Format("Torrent state {0} -> {1} : {2}", c.OldState, c.NewState, DateTime.Now))
            (fun d -> statusMessage <- String.Format("Tracker {0} -> {1} : {2}", d.Tracker.Uri, d.Tracker.Status, DateTime.Now))


    do  ClipboardNotification.ClipboardUpdate 
        |> Observable.add (fun arg ->
            let text = Clipboard.GetText()
            if not (text  = null) && text.StartsWith("magnet:?") || (text.StartsWith("http://") && text.EndsWith(".torrent")) then
                let svc = UrlMonitorService.create text Config.defaultDownloadPath
                let targetUrl = svc.GetLink()
                if svc.IsValidLink then
                    let vm =
                        if svc.IsMagnetLink then
                            torrentApp.AddTorrentManagerFromMagnet  (Config.getPathValues svc.DownloadPath) targetUrl
                        else
                            let pathValues = Config.getPathValues svc.DownloadPath
                            let file = Path.Combine(pathValues.InternalPath, Guid.NewGuid().ToString() + ".torrent" )
                            (Utils.downloadTorrent targetUrl file).Wait()
                            torrentApp.AddTorrentManager  pathValues file

                    vm
                    |> torrentApp.Register
                    |> torrentApp.Start
            )
    let clientEngineItem = ClientEngineItem(torrentApp.Engine)
    let displayedTorrentManagers = ObservableCollection<TorrentManagerViewModel>()

    let showTorrentManagers mgrs =
        displayedTorrentManagers |> Seq.iter (fun m -> m.StopWatch())
        displayedTorrentManagers.Clear()
        mgrs     
        |> Seq.iter(fun item ->
            displayedTorrentManagers.Add item
            if (item.OverallStatus = OverallStatus.Downloading || item.OverallStatus = OverallStatus.Seeding) then
               item.StartWatch()
            else 
                ()) //TODO
                //item.ShowFiles())

    let refreshTimer =
        let temp = DispatcherTimer()
        temp.Interval <- TimeSpan.FromMilliseconds(500.0)
        temp.Tick |> Observable.add (fun arg ->
                        this.OnPropertyChanged("StatusMessage")
                        let tryUpdateDisplay()  =
                            let updateUI mgrs = 
                                if not((mgrs |> Seq.length) = displayedTorrentManagers.Count) then
                                    showTorrentManagers mgrs
                            match mainViewDisplay with
                            | MainViewDisplay.Active -> updateUI torrentApp.ActiveTorrentManagers
                            | MainViewDisplay.Seeding -> updateUI torrentApp.SeedingTorrentManagers
                            | MainViewDisplay.Paused -> updateUI torrentApp.PausedTorrentManagers
                            | _ -> updateUI torrentApp.AllTorrentManagers

                        if (torrentApp.AllTorrentCount > 0) then
                            this.AllTorrentsHeader <- String.Format("All ({0})", torrentApp.AllTorrentCount)
                            this.PausedTorrentsHeader <- String.Format("Paused ({0})", torrentApp.PausedTorrentManagers |> Seq.length)
                            this.SeedingTorrentsHeader <- String.Format("Seeding ({0})", torrentApp.SeedingTorrentManagers |> Seq.length)
                            this.DownloadingTorrentsHeader <- String.Format("Active ({0})", torrentApp.ActiveTorrentManagers |> Seq.length)
                            tryUpdateDisplay())
        temp

    let loadedCommand =
        torrentApp.LoadTorrentFiles()       
        showTorrentManagers torrentApp.AllTorrentManagers
        refreshTimer.Start() 
        this.OnPropertyChanged("SelectedTorrentManager")

    let addMagnetLinkCommand =
        let onRun (arg) = ()
//            let svc = MagnetLinkService.create ""
//            svc.GetLink()
//            |> torrentApp.AddTorrentManagerFromMagnet             
//            |> torrentApp.Register
//            |> torrentApp.Start
             //showTorrentManagers torrentApp.ActiveTorrentManagers
        new RelayCommand((fun c -> true), onRun)

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
                    target 
                    |> torrentApp.AddTorrentManager pathValues
                    |> torrentApp.Register
                    |> torrentApp.Start 

                showTorrentManagers torrentApp.ActiveTorrentManagers
        new RelayCommand((fun c -> true), onRun)

    let removeTorrentCommand =
        let onRun (arg) =
            //show a folder browser dialog
            let dg = OpenFileDialog(Filter="Torrent Files (*.torrent)|*.torrent", Multiselect = false)
            if dg.ShowDialog().Value then
                let torrent = dg.FileName 
                let fileName = Path.GetFileName torrent
                let target = Path.Combine(pathValues.TorrentsPath, fileName)
                if not (File.Exists target) then
                    File.Copy(torrent, target)
                    target
                    |> torrentApp.AddTorrentManager pathValues
                    |> torrentApp.Register
                    |> torrentApp.Start 
                showTorrentManagers torrentApp.ActiveTorrentManagers
        new RelayCommand((fun c -> Utils.isNotNull(selectedTorrentManager)), onRun)

    let selectActiveTorrentsCommand =
        let onRun (arg) = 
            showTorrentManagers torrentApp.ActiveTorrentManagers
            mainViewDisplay <- MainViewDisplay.Active
        new RelayCommand((fun c -> true), onRun)

    let selectSeedingTorrentsCommand =
        let onRun (arg) = 
            showTorrentManagers torrentApp.SeedingTorrentManagers
            mainViewDisplay <- MainViewDisplay.Seeding
        new RelayCommand((fun c -> true), onRun)

    let selectPausedTorrentsCommand =
        let onRun (arg) = 
            showTorrentManagers torrentApp.PausedTorrentManagers
            mainViewDisplay <- MainViewDisplay.Paused
        new RelayCommand((fun c -> true), onRun)

    let selectAllTorrentsCommand =
        let onRun (arg) = 
            showTorrentManagers torrentApp.AllTorrentManagers
            mainViewDisplay <- MainViewDisplay.All
        new RelayCommand((fun c -> true), onRun)

    let rowClickCommand =
        let onRun (arg : obj) =
            let sender = arg :?> DependencyObject
            if not (sender = null) then
                let ancestor = sender.FindAncestor<DataGridRow>()
                if not (ancestor.IsNone) then ()
        new RelayCommand((fun c -> true), onRun)

    let pauseTorrentCommand =
        let onRun (arg) =
            if Utils.isNotNull(selectedTorrentManager) then
                torrentApp.Pause selectedTorrentManager               
        new RelayCommand((fun c -> Utils.isNotNull(selectedTorrentManager)), onRun)

    let startTorrentCommand =
        let onRun (arg) =
            if Utils.isNotNull(selectedTorrentManager) then
                torrentApp.Start selectedTorrentManager
                selectedTorrentManager.StartWatch()
        new RelayCommand((fun c -> Utils.isNotNull(selectedTorrentManager)), onRun)
    
    let moveTorrent (mgr:TorrentManagerViewModel) (getTargetPos : int -> int) (afterMove: TorrentManagerViewModel -> unit) =
        if Utils.isNotNull(mgr) then
            let curPos = displayedTorrentManagers.IndexOf(mgr)
            let targetPos =  getTargetPos curPos
            if displayedTorrentManagers.Remove(mgr) then
                displayedTorrentManagers.Insert(targetPos, mgr)       
                afterMove mgr

    let moveUpTorrentCommand =
        let onRun (arg) = 
            moveTorrent 
                selectedTorrentManager
                (fun  curPos ->  
                    let temp = curPos - 1
                    if temp < 0 then 0 else temp)
                (fun mgrItem -> mgrItem.TorrentManager.Torrent.Files.[0].Priority <- Priority.Normal) //TODO
        new RelayCommand((fun c -> true), onRun)
    
    let moveDownTorrentCommand =
        let onRun (arg) =
            moveTorrent 
                selectedTorrentManager
                (fun curPos ->  
                    let temp = curPos + 1
                    if temp < displayedTorrentManagers.Count then temp else curPos)
                (fun mgrItem -> mgrItem.TorrentManager.Torrent.Files.[0].Priority <- Priority.Normal) //TODO            
        new RelayCommand((fun c -> true), onRun)

    let stopTorrentCommand =
        let onRun (arg) =
           torrentApp.Stop selectedTorrentManager         
        new RelayCommand((fun c -> Utils.isNotNull(selectedTorrentManager)), onRun)
        
 
    member x.AddMagnetLinkCommand = addMagnetLinkCommand
    member x.MoveUpTorrentCommand = moveUpTorrentCommand
    member x.MoveDownTorrentCommand = moveDownTorrentCommand
    member x.PauseTorrentCommand = pauseTorrentCommand
    member x.StartTorrentCommand = startTorrentCommand
    member x.StopTorrentCommand = stopTorrentCommand
    member x.SelectAllTorrentsCommand = selectAllTorrentsCommand
    member x.SelectActiveTorrentsCommand = selectActiveTorrentsCommand
    member x.SelectSeedingTorrentsCommand = selectSeedingTorrentsCommand
    member x.SelectPausedTorrentsCommand = selectPausedTorrentsCommand
    member this.LoadedCommand = loadedCommand
    member this.AddTorrentCommand = addTorrentCommand
    member this.RowClickCommand = rowClickCommand

    member this.SelectedTorrentManager
        with get () = selectedTorrentManager
        and set v = 
            this.RaiseAndSetIfChanged(&selectedTorrentManager, v, "SelectedTorrentManager")
            this.PauseTorrentCommand.RaiseCanExecuteChanged()
            this.StartTorrentCommand.RaiseCanExecuteChanged()
            this.StopTorrentCommand.RaiseCanExecuteChanged()
  
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