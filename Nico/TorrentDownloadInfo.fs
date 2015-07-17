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
    let mutable downloadStartDate = DateTime.MinValue
    let mutable downloadDuration = TimeSpan.FromMinutes(0.0)
    let mutable state = OverallStatus.Others
    let mutable physicalTorrentFile = ""
    let mutable progress = 0

    static member Extension = ".tor.xml"
    member this.PhysicalTorrentFile with get() = physicalTorrentFile and set v = physicalTorrentFile <- v
    member this.Progress with get() = progress and set v = progress <- v
    
    member this.Name with get() = Path.GetFileName(this.PhysicalTorrentFile)
    
    member this.BytesDownloaded with get() = bytesDownloaded and set v = bytesDownloaded <- v

    member this.BytesUploaded with get() = bytesUploaded and set v = bytesUploaded <- v

    member this.DownloadStartDate with get() = downloadStartDate and set v = downloadStartDate <- v

    member this.DownloadDuration with get() = downloadDuration and set v = downloadDuration <- v

    member this.State with get() = state and set v = state <- v

    member this.Ratio = Math.Round(Convert.ToDouble(this.BytesUploaded)/Convert.ToDouble(this.BytesDownloaded), 3)

    member this.IsValid = File.Exists(this.PhysicalTorrentFile) && File.OpenRead(this.PhysicalTorrentFile).Length > 0L  
    
    member this.Save(internalPath:string) =
        if not (Directory.Exists(internalPath)) then
            (Directory.CreateDirectory internalPath) |> ignore
        
        let torrentXmlInfoFileName = Path.GetFileNameWithoutExtension(this.PhysicalTorrentFile) + TorrentDownloadInfo.Extension
        let target = Path.Combine(internalPath, torrentXmlInfoFileName)
        Utils.serialize<TorrentDownloadInfo>(this)
        |> Utils.stringToFile target


         



