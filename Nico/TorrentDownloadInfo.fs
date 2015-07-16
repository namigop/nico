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
    let mutable bytesDownloaded = 0.0
    let mutable bytesUploaded = 0.0
    let mutable downloadStartDate = DateTime.MinValue
    let mutable downloadDuration = TimeSpan.FromMinutes(0.0)
    let mutable status = OverallStatus.Others
    let mutable torrentFile = ""
    
    member this.TorrentFile with get() = torrentFile and set v = torrentFile <- v
    
    member this.Name with get() = name and set v = name <- v
    
    member this.BytesDownloaded with get() = bytesDownloaded and set v = bytesDownloaded <- v

    member this.BytesUploaded with get() = bytesUploaded and set v = bytesUploaded <- v

    member this.DownloadStartDate with get() = downloadStartDate and set v = downloadStartDate <- v

    member this.DownloadDuration with get() = downloadDuration and set v = downloadDuration <- v

    member this.Status with get() = status and set v = status <- v

    member this.Ration = Math.Round(this.BytesUploaded/this.BytesDownloaded, 3)

    member this.IsValid = File.Exists(this.TorrentFile) && File.OpenRead(this.TorrentFile).Length > 0L  
    
    member this.Save(internalPath:string) =
        if not (Directory.Exists(internalPath)) then
            (Directory.CreateDirectory internalPath) |> ignore
        
        let torrentXmlInfoFileName = Path.GetFileNameWithoutExtension(this.TorrentFile) + ".xml"
        let target = Path.Combine(internalPath, torrentXmlInfoFileName)
        let ser = XmlSerializer(typeof<TorrentDownloadInfo>)
        use ms = new  MemoryStream()
        ser.Serialize(ms, this)
        use reader =new StreamReader(ms)
        ms.Seek(0L, SeekOrigin.Begin) |> ignore
        reader.ReadToEnd()
        |> fun content -> File.WriteAllText(target, content)



         



