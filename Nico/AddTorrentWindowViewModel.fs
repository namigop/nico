namespace Nico

open System
open FsXaml
open System.Windows
open System.Web
open System.Net
open System.IO

type AddTorrentWindowViewModel() =
    inherit ViewModelBase()

    let mutable url = ""
    let mutable downloadPath = ""
    let mutable closeFunction = fun () -> ()
    let mutable dialogResult = Nullable<bool>(false)

    member this.Url
        with get () = url
        and set v = this.RaiseAndSetIfChanged(&url, v, "Url")
 
    member this.DownloadPath
        with get () = downloadPath
        and set v = this.RaiseAndSetIfChanged(&downloadPath, v, "DownloadPath")
 
    member this.OkCommand
        with get() =
            let onRun arg =
                dialogResult <- Nullable<bool>(true)
                closeFunction()
            new RelayCommand((fun _ -> true), onRun)

    member this.CancelCommand
        with get() =
            let onRun arg =
                dialogResult <- Nullable<bool>(false)
                closeFunction()
            new RelayCommand((fun _ -> true), onRun)

    member this.SelectFolderCommand
        with get() =
            new RelayCommand((fun _ -> true), fun arg -> ())

    member this.DialogResult 
        with get() = dialogResult

    member this.SetCloseWindow(close:unit -> unit) = closeFunction <- close
        
type AddTorrentWindowV = XAML<"AddTorrentWindow.xaml", true >

type IUrlMonitorService =
    abstract GetLink : unit -> string
    abstract IsMagnetLink : bool
    abstract IsValidLink : bool
    abstract Url : string
    abstract DownloadPath : string

module UrlMonitorService =

    let create url2 (downloadsPath) =
        let url = ref ""
        let downloadPath = ref ""
      
        {
            new IUrlMonitorService with
                member x.GetLink() =
                    let window = AddTorrentWindowV()
                    let vm : AddTorrentWindowViewModel = Utils.cast window.Root.DataContext
                    vm.SetCloseWindow(fun () -> window.Root.Close())
                    vm.DownloadPath <- downloadsPath
                    vm.Url <- url2
                    window.Root.ShowDialog() |> ignore
                    if vm.DialogResult.Value then
                        let vm : AddTorrentWindowViewModel = Utils.cast window.Root.DataContext
                        url := vm.Url
                        downloadPath := vm.DownloadPath
                      
                    !url
                member x.Url = !url
                member x.DownloadPath = !downloadPath
                member x.IsMagnetLink = (!url).StartsWith("magnet:?")
                member x.IsValidLink = x.IsMagnetLink || x.Url.StartsWith("http://") && x.Url.EndsWith(".torrent")   
                    
        
        }