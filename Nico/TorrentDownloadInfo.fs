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
    let mutable name = ""

    let filesInfo = ResizeArray<TorrentFileInfo>()

    static member Extension = ".tor.xml"

    member this.PhysicalTorrentFile 
        with get() = physicalTorrentFile 
        and set v = 
            physicalTorrentFile <- v
            if not (String.IsNullOrWhiteSpace v) then
                magnetLink <- ""

    member this.Progress with get() = progress and set v = progress <- v
    
    member this.Name 
        with get() = name
        and set v = name <- v

//        with get() = 
//            if (this.IsUsinMagnetLink) then
//                let magnetParts = MagnetLinkParser.parse this.MagnetLink
//                match magnetParts.dn with
//                | Some(name) -> name
//                | None -> magnetParts.xt |> Option.get
//            else
//                Path.GetFileName(this.PhysicalTorrentFile)
    
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
            if this.IsUsinMagnetLink then
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
        
        let name = Path.GetFileNameWithoutExtension(this.Name) 
            
        let torrentXmlInfoFileName = name + TorrentDownloadInfo.Extension
        let target = Path.Combine(internalPath, torrentXmlInfoFileName)
        Utils.serialize<TorrentDownloadInfo>(this)
        |> Utils.xmlPrettryPrintUTF8
        |> Utils.stringToFile target


         



