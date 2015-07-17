namespace Nico

open System
open System.Net
open System.IO
open MonoTorrent.BEncoding
open MonoTorrent.Client
open MonoTorrent.Client.Encryption
open MonoTorrent.Client.Tracker
open MonoTorrent.Common
open MonoTorrent.Dht
open MonoTorrent.Dht.Listeners
open NicoExtensions
 

module TorrentClient =
    let setupSettings downloadPath port =
        let engineSettings =
            let temp = EngineSettings(downloadPath, port)
            temp.PreferEncryption <- false
            temp.AllowedEncryption <- EncryptionTypes.All
            temp.GlobalMaxUploadSpeed <- 30 * 1024
            temp.GlobalMaxDownloadSpeed <- 100 * 1024
            temp.MaxReadRate <- 1 * 1024 * 1024
            temp
        let torrentDefaults = new TorrentSettings(4, 150, 0, 0)
        let allSettings =
            { 
              EngineSettings = engineSettings
              TorrentDefault = torrentDefaults
              Paths = Config.getPathValues() 
            }
        allSettings

    let setupClientEngine port engineSettings  =
        let engine = new ClientEngine(engineSettings)
        engine.ChangeListenEndpoint(new IPEndPoint(IPAddress.Any, port))
        let nodes = 
            try
                File.ReadAllBytes(Config.getPathValues().DhtNodeFile)
                 
            with
            | _ -> Array.zeroCreate 0
            

        let dhtListner = new DhtListener (new IPEndPoint (IPAddress.Any, port))
        let dht = new DhtEngine (dhtListner);
        engine.RegisterDht(dht);
        dhtListner.Start();
        engine.DhtEngine.Start(nodes);
        engine

    let createTorrentManager  torrentSettings (pathValues:PathValues) (torrentFile:string) =
        let  fastResume =
            try
                BEncodedValue.Decode<BEncodedDictionary>(File.ReadAllBytes(pathValues.FastResumeFile))        
            with
            | _ -> BEncodedDictionary()

        let torrent = Torrent.Load(torrentFile)
        let manager = new TorrentManager(torrent, pathValues.DownloadsPath, torrentSettings)
        let hex =  torrent.InfoHash.BEncodedHex()
        if fastResume.ContainsKey(hex) then
            let d = fastResume.[hex] :?> BEncodedDictionary
            manager.LoadFastResume(new FastResume(d));
        manager   

    let register (clientEngine:ClientEngine)  onPeersFound onPieceHashed onTorrentStateChanged onAnnounceComplete mgr =
        clientEngine.Register mgr

        mgr.PeersFound |> Observable.add onPeersFound
        mgr.PieceHashed |> Observable.add onPieceHashed
        mgr.TorrentStateChanged |> Observable.add onTorrentStateChanged
        mgr.TrackerManager |> Seq.iter (fun (tier:TrackerTier) ->
                tier.GetTrackers()
                |> Seq.iter (fun t ->  t.AnnounceComplete |> Observable.add onAnnounceComplete))
   
    let start (mgr:TorrentManager) = mgr.Start()
    let pause (mgr:TorrentManager) = mgr.Pause()
    let stop (mgr:TorrentManager) (engine:ClientEngine)  = 
        mgr.TorrentStateChanged |> Observable.add (fun args ->
                if (args.NewState = TorrentState.Stopped) then
                    engine.Unregister mgr
                    mgr.Dispose()

            )
        mgr.Stop()
    