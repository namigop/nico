namespace Nico

open System
open System.Windows
open System.ComponentModel
open System.Collections.ObjectModel
open System.Windows.Input
open System.Diagnostics
open System.Windows.Threading
 
open System.IO
 
open System.Xml
 
 
open System.Resources
open System.Threading.Tasks
open System.Reflection
open OxyPlot
open OxyPlot.Annotations
open OxyPlot.Axes
open OxyPlot.Series

type ISpeedPlot =
    abstract AddDownloadPerSecPoint : DatePoint -> unit
    abstract AddUploadPerSecPoint : DatePoint -> unit   
    abstract Reset : unit -> unit 
    abstract DownloadModel : PlotModel 
    abstract UploadModel : PlotModel 

module SpeedPlot =

    let create() =
        let downloadSpeedPerSecPoints = ResizeArray<DatePoint>()
        let downloadSpeedPerSecSeries =
            let temp =
                LineSeries(MarkerType = MarkerType.None, DataFieldX = "X", DataFieldY = "Y", ItemsSource = downloadSpeedPerSecPoints)
            temp.StrokeThickness <- 1.10
            temp.Title <- "Download KB/sec"
            temp

        let uploadSpeedPerSecPoints = ResizeArray<DatePoint>()
        let uploadSpeedPerSecSeries =
            let temp =
                LineSeries(MarkerType = MarkerType.None, DataFieldX = "X", DataFieldY = "Y", ItemsSource = uploadSpeedPerSecPoints)
            temp.StrokeThickness <- 1.10
            temp.Title <- "Upload KB/sec"
            temp
     
        ///Oxyplot model of the showing the HttpWebRequests performance counters
        let createPlotModel title series=
            let temp = PlotModel()
            temp.Title <- title
            temp.TitleFontSize <- 14.0
            temp.IsLegendVisible <- true
            let yAxis =
                let tempY =
                    LinearAxis
                        (Title = "KB/sec", MajorGridlineStyle = LineStyle.Dash, MinorGridlineStyle = LineStyle.Dot,
                         Position = AxisPosition.Left, IsAxisVisible = true, MaximumPadding = 0.1, MinimumPadding = 0.1)
                tempY.Minimum <- -1.0
                tempY

            let xAxis =
                let tempX =
                    DateTimeAxis
                        (Title = "Time", MajorGridlineStyle = LineStyle.Dash, MinorGridlineStyle = LineStyle.Dot, 
                         Position = AxisPosition.Bottom, StringFormat = "mm:ss", IsZoomEnabled = true, MaximumPadding = 0.1, MinimumPadding = 0.1)
                tempX

            temp.Axes.Add(yAxis)
            temp.Axes.Add(xAxis)
            temp.Series.Add(series)     
            temp

        let uploadPlotModel = createPlotModel "Upload Speed" uploadSpeedPerSecSeries
        let downloadPlotModel = createPlotModel "Upload Speed" downloadSpeedPerSecSeries

        let plotWindow = TimeSpan.FromMinutes 30.0

        let rec tryRemoveExtra (window:TimeSpan) (points:ResizeArray<DatePoint>) =
            let first = Seq.head points
            let last = Seq.last points
            if (last.X - first.X) > window then
                (points.Remove first) |> ignore
                tryRemoveExtra window points

        { 
            new ISpeedPlot with
                member x.AddDownloadPerSecPoint(p:DatePoint) =                    
                    downloadSpeedPerSecPoints.Add p
                    tryRemoveExtra plotWindow downloadSpeedPerSecPoints
                    downloadPlotModel.InvalidatePlot(true)
    
                member x.AddUploadPerSecPoint(p:DatePoint) =      
                    uploadSpeedPerSecPoints.Add p
                    tryRemoveExtra plotWindow uploadSpeedPerSecPoints
                    uploadPlotModel.InvalidatePlot(true)

                member x.Reset() = 
                    downloadSpeedPerSecPoints.Clear()
                    uploadSpeedPerSecPoints.Clear()
                    downloadPlotModel.InvalidatePlot(true);
                    uploadPlotModel.InvalidatePlot(true);

                member x.DownloadModel = downloadPlotModel 
                member x.UploadModel = uploadPlotModel 
        }
   
   