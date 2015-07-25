module NicoExtensions 
    
    open MonoTorrent
    open MonoTorrent.BEncoding
    open MonoTorrent.Client
    open MonoTorrent.Client.Encryption
    open MonoTorrent.Client.Tracker
    open MonoTorrent.Common
    open MonoTorrent.Dht
    open MonoTorrent.Dht.Listeners
    open System
    open System.Windows
    open System.Windows.Media
    open Nico

    type MonoTorrent.InfoHash with
        member this.BEncodedHex() = BEncodedString(this.ToHex())

    type System.Int32 with
        member this.ToDouble() = Convert.ToDouble(this)
 
    type System.Int64 with
        member this.ToDouble() = Convert.ToDouble(this)

    type Priority with
        member this.Convert() =
            match this with
            | Priority.High -> TorrentPriority.High
            | Priority.Highest -> TorrentPriority.Highest
            | Priority.Low -> TorrentPriority.Low
            | Priority.Lowest -> TorrentPriority.Lowest
            | _ -> TorrentPriority.Normal
            
    type DependencyObject with
        member this.FindAncestor<'T when 'T :> DependencyObject>() = 
            let rec findAncestor(elem:DependencyObject) =
                // Try get a parent and check for type.
                let parent = VisualTreeHelper.GetParent(elem)

                if parent = null then
                   None
                elif (typeof<'T> = parent.GetType()) then           
                    Some(parent :?> 'T)
                else
                    findAncestor(parent)
            
            findAncestor(this)
 
            