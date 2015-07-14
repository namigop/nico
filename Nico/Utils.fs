namespace Nico
open System
open System.Drawing
open System.Windows
open System.Windows.Media.Imaging
open System.Collections.Generic
open System.IO
module Utils =
  
    let private iconCache = Dictionary<string, Icon>()
    let GetIcon(fileName2:string) =
        let ext = Path.GetExtension(fileName2)
        let getFile target =
            if File.Exists(target) then
                target, true
            else
                let ext = Path.GetExtension(target)
                let tempFile = Path.GetTempFileName()
                let newFile =  tempFile + ext
                File.Move(tempFile, newFile)
                newFile, false
       
        let tryGet() =
            if iconCache.ContainsKey(ext) then
                iconCache.[ext]
            else     
                let fileName, isExisting = getFile fileName2
                let icon = Icon.ExtractAssociatedIcon(fileName)
                iconCache.[ext] <- icon
                if not isExisting then
                    File.Delete fileName
                icon

        let icon = tryGet()
        let rect = new Int32Rect(Width=icon.Width, Height=icon.Height)
        System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle, 
                        rect,
                        BitmapSizeOptions.FromEmptyOptions());


