namespace Nico

open InfoHashExtension
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

    let image =
        if File.Exists(torFile.FullPath) then
            Utils.GetIcon torFile.FullPath
        else
            null
    
    member this.Image = image
    member this.UpdateProgress() =
        this.Progress <- Math.Round(torFile.BitField.PercentComplete,2)
    member this.Progress
        with get () = progress
        and set v = this.RaiseAndSetIfChanged(&progress, v, "Progress")

    member this.FileName = fileName

    
    member this.SizeInMB = Math.Round(Convert.ToDouble(torFile.Length)/(1024.0 * 1024.0), 2)