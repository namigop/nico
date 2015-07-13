module InfoHashExtension 
    
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

    type MonoTorrent.InfoHash with
        member this.BEncodedHex() = BEncodedString(this.ToHex())

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
 
           

//        public static T FindAncestor<T>(this DependencyObject element)
//    where T : DependencyObject
//{
//    // Try get a parent and check for type.
//    var parent = VisualTreeHelper.GetParent(element);
//    if (parent is T)
//    {
//        return (T)parent;
//    }
//    return FindAncestor<T>(parent);
//}
