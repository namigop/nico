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
open System.IO
open System.Xml.Serialization

//use concrete class types so that we can serialize/deserialize it
type TorrentFileInfo(fullpath1, priority1, progress1, sizeInMBytes1) =
    let mutable fullpath = fullpath1
    let mutable priority = priority1
    let mutable progress = progress1
    let mutable sizeInMBytes = sizeInMBytes1

    new() = TorrentFileInfo("", TorrentPriority.Normal, 0.0, 0.0)

    member this.FullPath with get() = fullpath and set v = fullpath <- v
    member this.Priority with get() = priority and set v = priority <- v
    member this.Progress with get() = progress and set v = progress <- v
    member this.SizeInMBytes with get() = sizeInMBytes and set v = sizeInMBytes <- v

