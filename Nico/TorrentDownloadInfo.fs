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

    new (torItem:TorrentFileViewModel) = TorrentFileInfo(torItem.FileFullPath, torItem.Priority, torItem.Progress, torItem.SizeInMB)
    new() = TorrentFileInfo("", TorrentPriority.Normal, 0.0, 0.0)

    member this.FullPath with get() = fullpath and set v = fullpath <- v
    member this.Priority with get() = priority and set v = priority <- v
    member this.Progress with get() = progress and set v = progress <- v
    member this.SizeInMBytes with get() = sizeInMBytes and set v = sizeInMBytes <- v

//use concrete class types so that we can serialize/deserialize it
type TorrentDownloadInfo() =
    let mutable name = ""
    let mutable bytesDownloaded = 0L
    let mutable bytesUploaded = 0L
    let mutable downloadStartDate = DateTime.MaxValue
    let mutable downloadDuration = TimeSpan.FromMinutes(0.0)
    let mutable state = OverallStatus.Initializing
    let mutable physicalTorrentFile = ""
    let mutable progress = 0
    let mutable magnetLink = ""
    let filesInfo = ResizeArray<TorrentFileInfo>()

    static member Extension = ".tor.xml"
    member this.PhysicalTorrentFile 
        with get() = physicalTorrentFile 
        and set v = 
            physicalTorrentFile <- v
            magnetLink <- ""

    member this.Progress with get() = progress and set v = progress <- v
    
    member this.Name 
        with get() = 
            if (this.IsUsinMagnetLink) then
                let magnetParts = MagnetLinkParser.parse this.MagnetLink
                match magnetParts.dn with
                | Some(name) -> name
                | None -> magnetParts.xt |> Option.get
            else
                Path.GetFileName(this.PhysicalTorrentFile)
    
    member this.Files with get() = filesInfo

    member this.BytesDownloaded with get() = bytesDownloaded and set v = bytesDownloaded <- v

    member this.BytesUploaded with get() = bytesUploaded and set v = bytesUploaded <- v

    member this.DownloadStartDate with get() = downloadStartDate and set v = downloadStartDate <- v

    member this.DownloadDuration with get() = downloadDuration and set v = downloadDuration <- v
 
    member this.IsUsinMagnetLink = not(String.IsNullOrWhiteSpace this.MagnetLink)

    member this.MagnetLink 
        with get() = magnetLink 
        and set v = 
            magnetLink <- v
            physicalTorrentFile <- ""

    member this.State with get() = state and set v = state <- v

    member this.Ratio = Math.Round(Convert.ToDouble(this.BytesUploaded)/Convert.ToDouble(this.BytesDownloaded), 3)

    member this.IsValid = 
        if (String.IsNullOrWhiteSpace(magnetLink)) then
            File.Exists(this.PhysicalTorrentFile) && File.OpenRead(this.PhysicalTorrentFile).Length > 0L  
        else
            magnetLink.StartsWith("magnet")
    
    member this.Save(internalPath:string) =
        if not (Directory.Exists(internalPath)) then
            (Directory.CreateDirectory internalPath) |> ignore
        
        let torrentXmlInfoFileName = Path.GetFileNameWithoutExtension(this.PhysicalTorrentFile) + TorrentDownloadInfo.Extension
        let target = Path.Combine(internalPath, torrentXmlInfoFileName)
        Utils.serialize<TorrentDownloadInfo>(this)
        |> Utils.xmlPrettryPrintUTF8
        |> Utils.stringToFile target


         



