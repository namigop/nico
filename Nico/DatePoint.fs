namespace Nico

open System

//Point used by Charts with DateTime as X axis
type DatePoint() =
    let mutable dt = DateTime.Now
    let mutable value : double = 0.0

    member x.X
        with get () = dt
        and set (v) = dt <- v

    member x.Y
        with get () = value
        and set v = value <- v
 

