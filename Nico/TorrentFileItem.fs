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
open System.IO
open System.Collections.ObjectModel
open System.Diagnostics

type TorrentFileItem(torFile : TorrentFile, downloadPath) =
    inherit ViewModelBase()
    let mutable progress = 0.0
    let mutable fileName = torFile.Path
    let mutable priority = torFile.Priority
    let image = Utils.GetIcon torFile.FullPath
      
    let priorities =
        let temp = new ObservableCollection<Priority>()
        temp.Add(Priority.Highest)
        temp.Add(Priority.High)
        temp.Add(Priority.Normal)
        temp.Add(Priority.Low)
        temp.Add(Priority.Lowest)
        temp.Add(Priority.DoNotDownload)
        temp

    let openInExplorerCommand =
        let onRun (arg) =
            let downloadedAt = Path.GetDirectoryName(torFile.FullPath)
            Process.Start("explorer.exe", downloadedAt) |> ignore
        new RelayCommand((fun c -> true), onRun)

    member this.OpenInExplorerCommand = openInExplorerCommand
    member this.Image = image
    member this.UpdateProgress() =
        this.Progress <- Math.Round(torFile.BitField.PercentComplete,2)
    member this.Progress
        with get () = progress
        and set v = this.RaiseAndSetIfChanged(&progress, v, "Progress")

    member this.FileName = fileName
    member this.FileFullPath = torFile.FullPath

   
    member this.Priorities = priorities
    member this.Priority
        with get () = priority
        and set v = 
            this.RaiseAndSetIfChanged(&priority, v, "Priority")
            torFile.Priority <- v

    
    member this.SizeInMB = Math.Round(Convert.ToDouble(torFile.Length)/(1024.0 * 1024.0), 2)