namespace Nico

open System
open FsXaml

type AddMagnetLinkWindowViewModel() =
    inherit ViewModelBase()

    let mutable magnetUrl = "magnet:?xt=urn:btih:a770e547943c695bc1ed945c5194f3128d01bcec&dn=Trevor+Noah+-+African+American+%282013%29%5BDVDRIP%5D%5BBREEZE%5D&tr=udp%3A%2F%2Ftracker.openbittorrent.com%3A80&tr=udp%3A%2F%2Fopen.demonii.com%3A1337&tr=udp%3A%2F%2Ftracker.coppersurfer.tk%3A6969&tr=udp%3A%2F%2Fexodus.desync.com%3A6969"

     
    member this.MagnetUrl
        with get () = magnetUrl
        and set v = this.RaiseAndSetIfChanged(&magnetUrl, v, "MagnetUrl")
 
    member this.OkCommand
        with get() =
            new RelayCommand((fun _ -> true), fun arg -> ())
        
type AddMagnetLinkWindow = XAML<"AddMagnetLinkWindow.xaml", true >

 type IAddMagnetLinkService =
    abstract GetLink : unit -> string

module MagnetLinkService =

    let create paths =
        {
            new IAddMagnetLinkService with
                member x.GetLink() =
                    let window = AddMagnetLinkWindow()
                    let res = window.Root.ShowDialog()
                    if res.Value then
                        let vm : AddMagnetLinkWindowViewModel = Utils.cast window.Root.DataContext
                        vm.MagnetUrl
                    else
                        let vm : AddMagnetLinkWindowViewModel = Utils.cast window.Root.DataContext
                        vm.MagnetUrl
        
        }