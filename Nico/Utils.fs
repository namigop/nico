namespace Nico
open System
open System.Drawing
open System.Windows
open System.Windows.Media.Imaging
open System.Collections.Generic
open System.IO
open System.Xml
open System.DirectoryServices.AccountManagement

open System.Security.Cryptography
open Microsoft.VisualBasic.FileIO


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


    
    let fileDeleteForced fileFullPath = 
        let info = new FileInfo(fileFullPath);
        if (info.Exists) then
            if (info.IsReadOnly) then
                info.IsReadOnly <- false
            info.Delete()

    let rec fileDeleteAll (dir:string)  (recursv:bool) =
            if (Directory.Exists(dir)) then
                let files = Directory.GetFiles(dir)
                for file in files do
                    fileDeleteForced file
                if (recursv) then //clean up the sub directories
                    let subDirs = Directory.GetDirectories(dir)                  
                    for subDir in subDirs do
                        fileDeleteAll subDir recursv

    let cast o = (box o) :?> 'a

    let fileDeleteToRecycleBin file =
        if (File.Exists(file)) then
            FileSystem.DeleteFile(file, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing)

    let directoryDeleteToRecycleBin(dir:string) = 
        if (Directory.Exists(dir)) then
            FileSystem.DeleteDirectory(dir, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing)

    let directoryDelete p =
        if (Directory.Exists(p)) then
            Directory.Delete(p)

    let directoryDelete2 p r =
        if (Directory.Exists(p)) then
            Directory.Delete(p,r)
        
    let rec fileDeleteAllToRecycleBin (dir:string) (recursv:bool) = 
        if (Directory.Exists(dir)) then
            let files = Directory.GetFiles(dir)
            for file in files do
                fileDeleteToRecycleBin(file)
            if (recursv) then //clean up the sub directories
                let subDirs = Directory.GetDirectories(dir)
                for  subDir in subDirs do
                    directoryDeleteToRecycleBin(subDir)
                    //fileDeleteAllToRecycleBin(subDir, recursive);
    let tryGetXml2 content =
        if not (String.IsNullOrEmpty (content) ) then
            let doc = new XmlDocument(PreserveWhitespace = true )               
            try
                doc.LoadXml(content)
                Some(doc)
            with
            | _ -> None
        else
            None
        