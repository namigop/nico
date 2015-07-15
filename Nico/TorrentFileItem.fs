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

type TorrentFileItem(torFile : TorrentFile, downloadPath) =
    inherit ViewModelBase()
    let mutable progress = 0.0
    let mutable fileName = torFile.Path
    let mutable priority = torFile.Priority
    let image = Utils.GetIcon torFile.FullPath
      
    member this.Image = image
    member this.UpdateProgress() =
        this.Progress <- Math.Round(torFile.BitField.PercentComplete,2)
    member this.Progress
        with get () = progress
        and set v = this.RaiseAndSetIfChanged(&progress, v, "Progress")

    member this.FileName = fileName

    member this.Priority
        with get () = priority
        and set v = this.RaiseAndSetIfChanged(&priority, v, "Priority")

    
    member this.SizeInMB = Math.Round(Convert.ToDouble(torFile.Length)/(1024.0 * 1024.0), 2)