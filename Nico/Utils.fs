namespace Nico
open System
open System.Drawing
open System.Windows
open System.Windows.Media.Imaging
open System.Collections.Generic
open System.IO
module Utils =
  
    let private iconCache = Dictionary<string, Icon>()
    let GetIcon(fileName:string) =
        let ext = Path.GetExtension(fileName)
        let tryGet() =
            if iconCache.ContainsKey(ext) then
                iconCache.[ext]
            else
                let icon = Icon.ExtractAssociatedIcon(fileName)
                iconCache.[ext] <- icon
                icon

        let icon = tryGet()
        let rect = new Int32Rect(Width=icon.Width, Height=icon.Height)
        System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle, 
                        rect,
                        BitmapSizeOptions.FromEmptyOptions());


