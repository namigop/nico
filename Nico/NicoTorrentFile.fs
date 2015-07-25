namespace Nico

open System
open MonoTorrent.Common
open MonoTorrent
open NicoExtensions
open System.IO

type INicoTorrentFile =
    abstract FileName : string
    abstract Priority : TorrentPriority
    abstract FullPath : string
    abstract Progress : float
    abstract SizeInMB : float<MB>

module NicoTorrentFile =
    let createFromTorFile (torFile:TorrentFile) = 
        {
            new INicoTorrentFile with
                member this.FileName = torFile.Path
                member this.Priority = torFile.Priority.Convert()
                member this.FullPath = torFile.FullPath
                member this.Progress = Math.Round(torFile.BitField.PercentComplete, 2)
                member this.SizeInMB = Math.Round(Convert.ToDouble(torFile.Length) / (1024.0 * 1024.0), 2) * 1.0<MB>
        }

    let createFromTorFileInfo(info:TorrentFileInfo) =
        {
            new INicoTorrentFile with
                member this.FileName = Path.GetFileName info.FullPath
                member this.Priority = info.Priority
                member this.FullPath = info.FullPath
                member this.Progress = info.Progress
                member this.SizeInMB = info.SizeInMBytes
        }