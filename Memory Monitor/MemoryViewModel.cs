using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using Widgets.Common;
using System.Management;

namespace Memory_Monitor
{
    public partial class MemoryViewModel: INotifyPropertyChanged,IDisposable
    {
        private readonly Schedule schedule = new();
        private string scheduleID = "";
        private readonly CancellationTokenSource cancellationTokenSource = new();

        public struct SettingsStruct
        {
            public float TimeLine { get; set; }
            public string GraphicColor { get; set; }
        }

        public static SettingsStruct Default => new()
        {
            TimeLine = 200,
            GraphicColor = "#FF2E6E02",
        };

        public required SettingsStruct Settings = Default;

        private PerformanceCounter? memoryCounter;
        private AreaSeries? AreaSeries;
        private int timeCounter;
        private double totalMemory;

        private PlotModel? _plotModel;
        public PlotModel? MemoryPlotModel 
        {
            get { return _plotModel; }
            set { 
                _plotModel = value;
                OnPropertyChanged(nameof(MemoryPlotModel));
            }
        }

        private string _usageText = "0";
        public string UsageText
        {
            get { return _usageText; } 
            set { 
                _usageText = value;
                OnPropertyChanged(nameof(UsageText));
            }
        }

        public async Task Start()
        {
            try
            {
                await Task.Run(() =>
                {
                    totalMemory = GetTotalMemoryInMb();
                    memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return;
            }

            CreatePlot();
            UpdateUsage();

            scheduleID = schedule.Secondly(UpdateUsage, 1);
        }

        private void CreatePlot()
        {
            MemoryPlotModel = new PlotModel
            {
                PlotAreaBorderThickness = new OxyThickness(0),
                PlotAreaBorderColor = OxyColors.Transparent,
                Padding = new OxyThickness(0)
            };

            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                IsAxisVisible = false,
                MajorGridlineStyle = LineStyle.None,
                MinorGridlineStyle = LineStyle.None,
                MaximumPadding = 0,
                MinimumPadding = 0
            };

            var yAxis = new LinearAxis
            {
                Minimum = 0,
                Maximum = 100,
                Position = AxisPosition.Left,
                IsAxisVisible = false,
                MajorGridlineStyle = LineStyle.None,
                MinorGridlineStyle = LineStyle.None,
                MaximumPadding = 0,
                MinimumPadding = 0
            };

            MemoryPlotModel.Axes.Add(xAxis);
            MemoryPlotModel.Axes.Add(yAxis);

            AreaSeries = new AreaSeries
            {
                LineStyle = LineStyle.Solid,
                StrokeThickness = 1,
                Color = OxyColor.Parse(Settings.GraphicColor),
            };

            MemoryPlotModel.Series.Add(AreaSeries);
        }

        private void UpdateUsage()
        {
            if(memoryCounter is null || AreaSeries is null || MemoryPlotModel is null) return;

            double availableMemory = memoryCounter.NextValue() / 1024;
            double memoryUsage = ((totalMemory - availableMemory) / totalMemory) * 100;

            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    UsageText = $"Memory {(totalMemory - availableMemory):F1}/{totalMemory:F1} GB ({memoryUsage:F2}%)";

                    AreaSeries.Points.Add(new DataPoint(timeCounter, memoryUsage));
                    AreaSeries.Points2.Add(new DataPoint(timeCounter, 0));
                    timeCounter++;

                    if (AreaSeries.Points.Count > Settings.TimeLine)
                    {
                        AreaSeries.Points.RemoveAt(0);
                        AreaSeries.Points2.RemoveAt(0);
                    }

                    MemoryPlotModel.InvalidatePlot(true);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                }
            });
        }

        private static double GetTotalMemoryInMb()
        {
            double totalMemoryInMb = 0;
            using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
            {
                foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
                {
                    totalMemoryInMb = Convert.ToDouble(obj["TotalPhysicalMemory"]) / (1024 * 1024 * 1024); //GB
                }
            }
            return totalMemoryInMb;
        }

        public void Dispose()
        {
            schedule.Stop(scheduleID);
            cancellationTokenSource.Cancel();
            GC.SuppressFinalize(this);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
