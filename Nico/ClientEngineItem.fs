namespace Nico

open System
open System.Windows.Threading
open NicoExtensions
open MonoTorrent.BEncoding
open MonoTorrent.Client
open MonoTorrent.Client.Encryption
open MonoTorrent.Client.Tracker
open MonoTorrent.Common
open MonoTorrent.Dht
open MonoTorrent.Dht.Listeners

type ClientEngineItem(engine:ClientEngine) as this =
    inherit ViewModelBase()
    let mutable totalDownloadSpeed = 0.0
    let mutable totalUploadSpeed = 0.0
    let mutable readRate = 0.0
    let mutable writeRate = 0.0
    let mutable totalRead = 0.0
    let mutable totalWritten = 0.0
    let mutable openConnections = 0.0

    let mutable fileName = 0
    let timer =
        let temp = DispatcherTimer()
        temp.Interval <- TimeSpan.FromMilliseconds(1000.0)
        temp.Tick |> Observable.add(fun arg -> 
            this.TotalDownloadSpeed <- Convert.ToDouble(engine.TotalDownloadSpeed) / 1024.0
            this.TotalUploadSpeed <- Convert.ToDouble(engine.TotalUploadSpeed) / 1024.0
            this.ReadRate <- Convert.ToDouble(engine.DiskManager.ReadRate) / 1024.0
            this.WriteRate <- Convert.ToDouble(engine.DiskManager.WriteRate) / 1024.0
            this.TotalRead <- Convert.ToDouble(engine.DiskManager.TotalRead) / 1024.0
            this.TotalWritten <- Convert.ToDouble(engine.DiskManager.TotalWritten) / 1024.0
            this.OpenConnections <- Convert.ToDouble(engine.ConnectionManager.OpenConnections) / 1024.0
            )
        temp

    member this.ClientEngine = engine
    member this.StartWatch() = timer.Start()
    member this.StopWatch() = timer.Stop()
    
    member this.TotalDownloadSpeed
        with get () = totalDownloadSpeed
        and set v = this.RaiseAndSetIfChanged(&totalDownloadSpeed, v, "TotalDownloadSpeed")
    member this.TotalUploadSpeed
        with get () = totalUploadSpeed
        and set v = this.RaiseAndSetIfChanged(&totalUploadSpeed, v, "TotalUploadSpeed")
    member this.ReadRate
        with get () = readRate
        and set v = this.RaiseAndSetIfChanged(&readRate, v, "ReadRate")
    member this.WriteRate
        with get () = writeRate
        and set v = this.RaiseAndSetIfChanged(&writeRate, v, "WriteRate")
    member this.TotalRead
        with get () = totalRead
        and set v = this.RaiseAndSetIfChanged(&totalRead, v, "TotalRead")
    member this.TotalWritten
        with get () = totalWritten
        and set v = this.RaiseAndSetIfChanged(&totalWritten, v, "TotalWritten")
    member this.OpenConnections
        with get () = openConnections
        and set v = this.RaiseAndSetIfChanged(&openConnections, v, "OpenConnections")