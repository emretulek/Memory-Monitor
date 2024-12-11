using System.ComponentModel;
using System.Windows;
using Widgets.Common;

namespace Memory_Monitor
{
    public partial class MainWindow : Window,IWidgetWindow
    {
        public readonly static string WidgetName = "Memory Monitor";
        public readonly static string SettingsFile = "settings.memorymonitor.json";
        private readonly Config config = new(SettingsFile);

        public MemoryViewModel ViewModel { get; set; }
        private MemoryViewModel.SettingsStruct Settings = MemoryViewModel.Default;

        public MainWindow()
        {
            InitializeComponent();

            LoadSettings();
            ViewModel = new()
            {
                Settings = Settings
            };
            DataContext = ViewModel;
            _= ViewModel.Start();
            Logger.Info($"{WidgetName} is started");
        }

        public void LoadSettings()
        {
            try
            {
                Settings.GraphicColor = PropertyParser.ToString(config.GetValue("graphic_color"), Settings.GraphicColor);
                Settings.TimeLine = PropertyParser.ToFloat(config.GetValue("graphic_timeline"), Settings.TimeLine);
                UsageText.FontSize = PropertyParser.ToFloat(config.GetValue("usage_font_size"));
                UsageText.Foreground = PropertyParser.ToColorBrush(config.GetValue("usage_foreground"));
            }
            catch (Exception)
            {
                config.Add("usage_font_size", UsageText.FontSize);
                config.Add("usage_foreground", UsageText.Foreground);
                config.Add("graphic_color", Settings.GraphicColor);
                config.Add("graphic_timeline", Settings.TimeLine);
                config.Save();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            ViewModel.Dispose();
            Logger.Info($"{WidgetName} is closed");
        }

        public WidgetWindow WidgetWindow()
        {
            return new WidgetWindow(this, WidgetDefaultStruct());
        }

        public static WidgetDefaultStruct WidgetDefaultStruct()
        {
            return new WidgetDefaultStruct();
        }
    }
}
