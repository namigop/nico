namespace Nico

open System
open System.Text
open System.Web

type MagnetLinkParts =
    {
        dn : string option//DisplayName
        xl : string option//eXact Length
        xt : string option//eXact Topic
        acs : string option//AcceptableSource
        kt : string option//Keyword Topic
        mt : string option//Manifest topic
        tr : string option//Address Tracker
    }

module MagnetLinkParser =

   // "magnet:?xt=urn:btih:a770e547943c695bc1ed945c5194f3128d01bcec&dn=Trevor+Noah+-+African+American+%282013%29%5BDVDRIP%5D%5BBREEZE%5D&tr=udp%3A%2F%2Ftracker.openbittorrent.com%3A80&tr=udp%3A%2F%2Fopen.demonii.com%3A1337&tr=udp%3A%2F%2Ftracker.coppersurfer.tk%3A6969&tr=udp%3A%2F%2Fexodus.desync.com%3A6969"
    let private magnetStart = "magnet:?"

    let parse (magnetUrl:string) =
        if magnetUrl.StartsWith(magnetStart) |> not then
            failwith("magnet link URLs should start with " + magnetStart)

        let magnetParts = 
            magnetUrl.Replace(magnetStart, "")
            |> fun str -> str.Split("&".ToCharArray())
            |> Seq.map (fun line -> 
                    Console.WriteLine line
                    let parts = line.Split("=".ToCharArray(), 2)
                    parts.[0], Some(parts.[1])
                )
        let decode p =
            match p with
            | Some(v) -> Some(HttpUtility.UrlDecode v)
            | None -> None
        let get (check: bool) v = if check then decode v else None
            
        {
            dn = magnetParts  |> Seq.tryPick(fun  (k, v)-> get (k = "dn") v) 
            xl = magnetParts  |> Seq.tryPick(fun  (k, v)-> get (k = "xl") v)
            xt = magnetParts  |> Seq.tryPick(fun  (k, v)-> get (k = "xt") v)
            acs = magnetParts |> Seq.tryPick(fun  (k, v)-> get (k = "as") v)
            kt = magnetParts  |> Seq.tryPick(fun  (k, v)-> get (k = "kt") v)
            mt = magnetParts  |> Seq.tryPick(fun  (k, v)-> get (k = "mt") v)
            tr = magnetParts  |> Seq.tryPick(fun  (k, v)-> get (k = "tr") v)
        }
        
            

