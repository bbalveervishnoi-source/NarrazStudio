// Copyright (C) 2026 Balveer
// Narraz Studio is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License.
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.InteropServices;
using WinRT.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Dispatching;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Media.Playback;
using Windows.Media.Core;
using Microsoft.UI.Text;
using Microsoft.Web.WebView2.Core;

namespace NarrazStudio
{
    public class VoiceModel
    {
        public string Name { get; set; } = "";
        public string ShortName { get; set; } = "";
        public string Gender { get; set; } = "";
        public string Locale { get; set; } = "";
    }

    public class VoiceGroup
    {
        public string LanguageName { get; set; } = "";
        public ObservableCollection<VoiceModel> Voices { get; set; } = new();
    }

    public class HistoryItem
    {
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string VoiceName { get; set; } = "";
        public string DateString { get; set; } = "";
        public string DateIso { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public int CharCount { get; set; }
        public int WordCount { get; set; }
        public int SrNo { get; set; }
        public LanguageManager Lang => LanguageManager.Instance;
        public string FileSizeStr
        {
            get
            {
                try
                {
                    if (System.IO.File.Exists(FilePath))
                    {
                        long bytes = new System.IO.FileInfo(FilePath).Length;
                        double mb = bytes / (1024.0 * 1024.0);
                        return $"{mb:F2} MB";
                    }
                }
                catch { }
                return "—";
            }
        }
    }

    public class BatchExportItem : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        
        public string FilePath { get; set; } = "";
        public string FileName { get; set; } = "";
        public long FileSizeBytes { get; set; }
        public string FileSizeStr => FileSizeBytes > 0 ? $"{((double)FileSizeBytes / (1024.0 * 1024.0)):F2} MB" : "—";
        public string OutputFilePath { get; set; } = "";
        
        public System.Threading.CancellationTokenSource? ItemCts { get; set; }

        private double _progress;
        public double Progress 
        { 
            get => _progress; 
            set { _progress = value; Notify(nameof(Progress)); } 
        }

        private string _statusText = "Ready";
        public string StatusText 
        { 
            get => _statusText; 
            set { _statusText = value; Notify(nameof(StatusText)); } 
        }
        
        private bool _isProcessing;
        public bool IsProcessing 
        { 
            get => _isProcessing; 
            set { _isProcessing = value; NotifyAllState(); } 
        }

        private bool _isQueued;
        public bool IsQueued 
        { 
            get => _isQueued; 
            set { _isQueued = value; NotifyAllState(); } 
        }

        private bool _isCompleted;
        public bool IsCompleted 
        { 
            get => _isCompleted; 
            set { _isCompleted = value; NotifyAllState(); } 
        }

        public Visibility ProgressVisibility => (IsProcessing || IsQueued) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CheckmarkVisibility => IsCompleted ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ConvertBtnVisibility => (!IsProcessing && !IsQueued && !IsCompleted) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CancelBtnVisibility => (IsProcessing || IsQueued) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility LocateBtnVisibility => IsCompleted ? Visibility.Visible : Visibility.Collapsed;
        public bool IsDeleteEnabled => !IsProcessing && !IsQueued;

        public LanguageManager Lang => LanguageManager.Instance;
        public Action? StateChanged { get; set; }

        private void Notify(string prop) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(prop));
        private void NotifyAllState()
        {
            Notify(nameof(IsProcessing));
            Notify(nameof(IsQueued));
            Notify(nameof(IsCompleted));
            Notify(nameof(ProgressVisibility));
            Notify(nameof(CheckmarkVisibility));
            Notify(nameof(ConvertBtnVisibility));
            Notify(nameof(CancelBtnVisibility));
            Notify(nameof(LocateBtnVisibility));
            Notify(nameof(IsDeleteEnabled));
            StateChanged?.Invoke();
        }
    }

    public class BatchCompletedItem
    {
        public string SrNo { get; set; } = "";
        public string FileName { get; set; } = "";
        public string OutputFilePath { get; set; } = "";
        public string VoiceName { get; set; } = "";
        public string DateString { get; set; } = "";
        public string TimeString { get; set; } = "";
        public string FileSizeStr { get; set; } = "";
    }

    public sealed partial class MainWindow : Window
    {
        // Fields
        private List<VoiceModel> _allVoices = new();
        private CollectionViewSource _groupedVoices = new() { IsSourceGrouped = true, ItemsPath = new PropertyPath("Voices") };
        private string _appDataPath;
        private VoiceModel? _selectedVoice;
        private MediaPlayer? _mediaPlayer;
        private DispatcherQueueTimer? _playbackTimer;
        private DispatcherQueueTimer? _wordCountTimer;
        private bool _isUpdatingSlider = false;
        private Process? _currentExportProcess;
        private bool _isExportCancelled;
        private System.Threading.CancellationTokenSource? _wordCountCts;
        private bool _isInitialSync = true;
        private bool _isInternetError = false;
        private bool _isLoaded = false;
        public LanguageManager Lang => LanguageManager.Instance;
        private ObservableCollection<HistoryItem> _historyList = new();
        private List<HistoryItem> _fullHistory = new();
        private string _historyFilePath = "";
        private string _selectedPeriod = "All Records";
        private ObservableCollection<BatchExportItem> _batchItems = new();
        private ObservableCollection<BatchCompletedItem> _completedItems = new();
        private string _batchSavePath = "";
        private bool _isBatchProcessing = false;
        private bool _skipDeleteConfirm = false;
        private System.Threading.CancellationTokenSource? _batchCts;

        public MainWindow()
        {
            Console.WriteLine("MainWindow Constructor Started");
            // INITIALIZE LANGUAGE FIRST so x:Bind resolves correctly
            var initialSettings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
            string initialLang = (initialSettings["AppLanguage"] as string) ?? "System";
            LanguageManager.Instance.SetLanguage(initialLang);

            try
            {
                this.InitializeComponent();
                Console.WriteLine("InitializeComponent succeeded");
            }
            catch (Exception ex)
            {
                Console.WriteLine("XAML InitializeComponent Exception: " + ex.ToString());
                throw;
            }

            this.Closed += MainWindow_Closed;
            _batchItems.CollectionChanged += (s, e) => UpdateBatchMasterButtons();
            BatchFilesList.ItemsSource = _batchItems;
            CompletedList.ItemsSource = _completedItems;

            // Set up Title Bar
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);
            
            var titleBar = this.AppWindow.TitleBar;
            titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;

            // Restore saved theme before window is visible
            if (this.Content is FrameworkElement fe) {
                var settings = ApplicationData.Current.LocalSettings.Values;
                string theme = (settings["AppTheme"] as string) ?? "System";
                if (theme == "Aqua") {
                    fe.RequestedTheme = ElementTheme.Light;
                    ApplyAquaTheme();
                } else {
                    fe.RequestedTheme = theme switch {
                        "Light" => ElementTheme.Light,
                        "Dark" => ElementTheme.Dark,
                        _ => ElementTheme.Default
                    };
                }
            }
            UpdateCaptionButtonColors();

            // Apply backdrop based on saved setting
            try
            {
                var micaSettings = ApplicationData.Current.LocalSettings.Values;
                bool micaEnabled = (micaSettings["MicaEnabled"] as bool?) ?? true;
                if (micaEnabled)
                    this.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
                else
                    this.SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
            }
            catch { }

            this.Activate();

            // Force native window to foreground
            try
            {
                var hwnd = WindowNative.GetWindowHandle(this);
                ShowWindow(hwnd, 5);
                SetForegroundWindow(hwnd);
            }
            catch { }

            // Resolve app data path
            try
            {
                _appDataPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
            }
            catch
            {
                _appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NarrazStudio");
            }
            Directory.CreateDirectory(_appDataPath);
            _historyFilePath = Path.Combine(_appDataPath, "history.json");
            LoadHistory();

            // Defer voice sync so window renders first
            this.DispatcherQueue?.TryEnqueue(() => { 
                if (CheckInternetConnection(true)) {
                    SyncVoices();
                } else {
                    SelectedVoiceName.Text = "Connection Required";
                }
            });

            // Initialize media player and timers
            _mediaPlayer = new MediaPlayer();
            _mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            if (this.DispatcherQueue != null)
            {
                _playbackTimer = this.DispatcherQueue.CreateTimer();
                _playbackTimer.Interval = TimeSpan.FromMilliseconds(500);
                _playbackTimer.Tick += PlaybackTimer_Tick;
            }

            if (this.DispatcherQueue != null)
            {
                _wordCountTimer = this.DispatcherQueue.CreateTimer();
                _wordCountTimer.Interval = TimeSpan.FromSeconds(1.5);
                _wordCountTimer.Tick += (s, e) => {
                    _wordCountTimer?.Stop();
                    UpdateWordCountAsync();
                };
            }
            
            this.Closed += MainWindow_Closed;

            // Set window taskbar icon
            try {
                IntPtr hwnd = WindowNative.GetWindowHandle(this);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
                appWindow.SetIcon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/AppIcon.png"));
            } catch { }

            EditorWebView.Loaded += EditorWebView_Loaded;
            _isLoaded = true;
        }

        // Load themed WebView2 editor
        private async void EditorWebView_Loaded(object sender, RoutedEventArgs e)
        {
            try {
                await EditorWebView.EnsureCoreWebView2Async();
                EditorWebView.DefaultBackgroundColor = Microsoft.UI.Colors.Transparent;
                var settings = ApplicationData.Current.LocalSettings.Values;
                string appTheme = (settings["AppTheme"] as string) ?? "System";
                string themeClass = appTheme.ToLower();
                if (themeClass == "system") 
                {
                    themeClass = Application.Current.RequestedTheme == ApplicationTheme.Light ? "light" : "dark";
                }
                
                string html = $@"
                <!DOCTYPE html>
                <html>
                <head>
                <style>
                  body, html {{ margin: 0; padding: 0; height: 100%; background-color: transparent; }}
                  textarea {{
                    width: 100%; height: 100%; box-sizing: border-box;
                    background-color: transparent;
                    border: none; outline: none; resize: none; overflow-y: auto;
                    font-family: 'Segoe UI Variable', 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                    font-size: 16px; padding: 10px 10px 60px 10px; line-height: 1.6;
                  }}
                  
                  body.light textarea {{ color: #1E293B !important; }}
                  body.light textarea::placeholder {{ color: rgba(0,0,0,0.4) !important; }}
                  body.light ::-webkit-scrollbar-thumb {{ background-color: rgba(0,0,0,0.2); }}
                  body.light ::-webkit-scrollbar-thumb:hover {{ background-color: rgba(0,0,0,0.4); }}

                  body.dark textarea {{ color: #f3f3f3 !important; }}
                  body.dark textarea::placeholder {{ color: rgba(255,255,255,0.4) !important; }}
                  body.dark ::-webkit-scrollbar-thumb {{ background-color: rgba(128,128,128,0.4); }}
                  body.dark ::-webkit-scrollbar-thumb:hover {{ background-color: rgba(128,128,128,0.6); }}

                  ::-webkit-scrollbar {{ width: 12px; background-color: transparent; }}
                  ::-webkit-scrollbar-track {{ background-color: transparent; }}
                  ::-webkit-scrollbar-thumb:vertical {{ min-height: 40px; border-radius: 6px; }}
                </style>
                </head>
                <body class='{themeClass}'>
                  <textarea id='editor' placeholder='{LanguageManager.Instance["EditorPlaceholder"]}' spellcheck='false'></textarea>
                  <script>
                    const editor = document.getElementById('editor');
                    editor.addEventListener('input', function() {{ window.chrome.webview.postMessage('text_changed'); }});

                    document.addEventListener('dragover', function(e) {{
                        e.preventDefault();
                    }});
                    document.addEventListener('drop', function(e) {{
                        e.preventDefault();
                        if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {{
                            const file = e.dataTransfer.files[0];
                            if (file.name.toLowerCase().endsWith('.txt')) {{
                                const reader = new FileReader();
                                reader.onload = function(evt) {{
                                    editor.value = evt.target.result;
                                    window.chrome.webview.postMessage('text_changed');
                                }};
                                reader.readAsText(file);
                            }} else {{
                                window.chrome.webview.postMessage('invalid_file');
                            }}
                        }}
                    }});
                  </script>
                </body>
                </html>";
                EditorWebView.NavigateToString(html);
            } catch { }
        }

        // Push current theme class into the live WebView
        private async void UpdateWebViewTheme()
        {
            try 
            {
                if (EditorWebView?.CoreWebView2 != null)
                {
                    var settings = ApplicationData.Current.LocalSettings.Values;
                    string theme = (settings["AppTheme"] as string) ?? "System";
                    string jsTheme = theme.ToLower();
                    if (jsTheme == "system") 
                    {
                        jsTheme = Application.Current.RequestedTheme == ApplicationTheme.Light ? "light" : "dark";
                    }
                    string js = $"document.body.className = '{jsTheme}';";
                    await EditorWebView.CoreWebView2.ExecuteScriptAsync(js);
                }
            } 
            catch { }
        }

        // Show failure overlay when offline
        private bool CheckInternetConnection(bool showOverlay = false)
        {
            bool isAvailable = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            if (!isAvailable && showOverlay)
            {
                DispatcherQueue.TryEnqueue(() => {
                    FailureTitle.Text = "No Internet Connection";
                    FailureReason.Text = "Narraz Studio requires an active internet connection to sync voices and generate audio. Please check your connection and try again.";
                    FailurePrimaryBtn.Content = "OK";
                    _isInternetError = true;
                    ShowOverlayState("Failure");
                    FailureSecondaryBtn.Visibility = Visibility.Collapsed;
                });
            }
            return isAvailable;
        }

        // Load history from disk into observable list
        private void LoadHistory()
        {
            try {
                if (File.Exists(_historyFilePath)) {
                    string json = File.ReadAllText(_historyFilePath);
                    var items = JsonSerializer.Deserialize<List<HistoryItem>>(json);
                    if (items != null) {
                        _fullHistory = items.OrderByDescending(x => x.Timestamp).ToList();
                        UpdateHistoryUI(_fullHistory);
                    }
                }
            } catch { }
        }

        private void SaveHistory()
        {
            try {
                string json = JsonSerializer.Serialize(_fullHistory);
                File.WriteAllText(_historyFilePath, json);
            } catch { }
        }

        // Add new export record to history
        private void AddToHistory(string fileName, string filePath, int chars, int words)
        {
            var now = DateTime.Now;
            var item = new HistoryItem {
                FileName = fileName,
                FilePath = filePath,
                VoiceName = _selectedVoice?.Name ?? "Unknown",
                Timestamp = now,
                DateIso = now.ToString("yyyy-MM-dd"),
                DateString = now.ToString("dd MMM yyyy, HH:mm"),
                CharCount = chars,
                WordCount = words
            };
            _fullHistory.Insert(0, item);
            ApplyAdvancedFilters();
        }

        private void UpdateFilteringOptions() { }

        // Navigation handlers
        private void NavigateToHome(object sender, RoutedEventArgs e)
        {
            _batchCts?.Cancel();
            foreach (var item in _batchItems) { if (item.IsProcessing) item.ItemCts?.Cancel(); item.IsQueued = false; }
            _isBatchProcessing = false;
            if (BatchCancelAllBtn != null) BatchCancelAllBtn.Visibility = Visibility.Collapsed;
            UpdateBatchMasterButtons();
            CleanTempFiles();

            MainAppLayout.Visibility = Visibility.Visible;
            HomeView.Visibility = Visibility.Visible;
            HistoryView.Visibility = Visibility.Collapsed;
            SettingsView.Visibility = Visibility.Collapsed;
            BatchView.Visibility = Visibility.Collapsed;
        }

        private void NavigateToSaved(object sender, RoutedEventArgs e)
        {
            MainAppLayout.Visibility = Visibility.Collapsed;
            HistoryView.Visibility = Visibility.Visible;
            SettingsView.Visibility = Visibility.Collapsed;
            BatchView.Visibility = Visibility.Collapsed;
        }

        private void NavigateToSettings(object sender, RoutedEventArgs e)
        {
            MainAppLayout.Visibility = Visibility.Collapsed;
            HistoryView.Visibility = Visibility.Collapsed;
            SettingsView.Visibility = Visibility.Visible;
            BatchView.Visibility = Visibility.Collapsed;
            CheckFFmpegStatus();
            LoadSettings();
        }

        private void NavigateToBatch(object sender, RoutedEventArgs e)
        {
            MainAppLayout.Visibility = Visibility.Collapsed;
            HistoryView.Visibility = Visibility.Collapsed;
            SettingsView.Visibility = Visibility.Collapsed;
            BatchView.Visibility = Visibility.Visible;
            if (BatchFilesList.ItemsSource == null)
            {
                BatchFilesList.ItemsSource = _batchItems;
            }
        }


        private void CheckFFmpegBtn_Click(object sender, RoutedEventArgs e) => CheckFFmpegStatus();

        // Detect and display FFmpeg availability
        private void CheckFFmpegStatus()
        {
            string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Engine", "ffmpeg.exe");
            bool exists = File.Exists(ffmpegPath);
            DispatcherQueue.TryEnqueue(() => {
                if (FFmpegStatusIcon != null) {
                    FFmpegStatusIcon.Glyph = exists ? "\uE73E" : "\uE711";
                    FFmpegStatusIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(exists ? Microsoft.UI.Colors.LightGreen : Microsoft.UI.Colors.Red);
                    FFmpegStatusText.Text = exists ? LanguageManager.Instance["FFmpegLinked"] : LanguageManager.Instance["FFmpegMissing"];
                }
            });
        }

        // Restore persisted settings into UI
        private void LoadSettings()
        {
            var settings = ApplicationData.Current.LocalSettings.Values;
            MicaSwitch.IsOn = (settings["MicaEnabled"] as bool?) ?? true;
            string theme = (settings["AppTheme"] as string) ?? "System";
            ThemeSelector.SelectedItem = ThemeSelector.Items.Cast<ComboBoxItem>().FirstOrDefault(x => x.Tag?.ToString() == theme);
            string lang = (settings["AppLanguage"] as string) ?? "System";
            LanguageSelector.SelectedItem = LanguageSelector.Items.Cast<ComboBoxItem>().FirstOrDefault(x => x.Tag?.ToString() == lang) ?? LanguageSelector.Items[0];
            
            if (ParallelLimitSelector.Items.Count == 0)
            {
                for (int i = 1; i <= 30; i++)
                {
                    ParallelLimitSelector.Items.Add(new ComboBoxItem { Content = i.ToString(), Tag = i });
                }
            }
            int parallelLimit = (settings["ParallelChunkLimit"] as int?) ?? 15;
            ParallelLimitSelector.SelectedIndex = parallelLimit - 1;

            // Restore Aqua theme overlay if it was active
            if (theme == "Aqua")
            {
                if (this.Content is FrameworkElement fe) fe.RequestedTheme = ElementTheme.Light;
                ApplyAquaTheme();
            }
        }

        // Auto-apply theme selection
        private void ThemeSelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            var settings = ApplicationData.Current.LocalSettings.Values;
            string theme = (ThemeSelector.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "System";
            settings["AppTheme"] = theme;

            if (theme == "Aqua")
            {
                // Aqua forces light element theme, then overlays Aqua visuals
                if (this.Content is FrameworkElement fe2) fe2.RequestedTheme = ElementTheme.Light;
                ApplyAquaTheme();
            }
            else
            {
                RemoveAquaTheme();
                if (this.Content is FrameworkElement fe) {
                    fe.RequestedTheme = theme switch {
                        "Light" => ElementTheme.Light,
                        "Dark"  => ElementTheme.Dark,
                        _       => ElementTheme.Default
                    };
                }
            }
            UpdateCaptionButtonColors();
            UpdateWebViewTheme();
        }

        private void UpdateCaptionButtonColors()
        {
            try {
                var titleBar = this.AppWindow.TitleBar;
                bool isLight = false;
                if (this.Content is FrameworkElement root) {
                    isLight = root.ActualTheme == ElementTheme.Light;
                }
                titleBar.ButtonForegroundColor = isLight ? Microsoft.UI.Colors.Black : Microsoft.UI.Colors.White;
                titleBar.ButtonHoverForegroundColor = isLight ? Microsoft.UI.Colors.Black : Microsoft.UI.Colors.White;
                titleBar.ButtonHoverBackgroundColor = isLight 
                    ? Windows.UI.Color.FromArgb(30, 0, 0, 0) 
                    : Windows.UI.Color.FromArgb(30, 255, 255, 255);
            } catch { }
        }

        // Auto-apply mica backdrop
        private void MicaSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            var settings = ApplicationData.Current.LocalSettings.Values;
            settings["MicaEnabled"] = MicaSwitch.IsOn;
            try {
                if (MicaSwitch.IsOn)
                {
                    this.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
                }
                else
                {
                    this.SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
                }
            } catch { }
        }

        // Apply the Aqua theme overlay — standard mac OS style with custom color design
        private void ApplyAquaTheme()
        {
            try
            {
                // 1. Classic Mac app background and default colours
                Application.Current.Resources["SystemAccentColor"] = Windows.UI.Color.FromArgb(255, 10, 132, 255); // Classic Aqua Blue
                Application.Current.Resources["ApplicationPageBackgroundThemeBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 245, 247));
                Application.Current.Resources["CardBackgroundFillColorDefaultBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
                Application.Current.Resources["ControlFillColorDefaultBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
                
                // Classic Mac Button styles
                Application.Current.Resources["ButtonBackgroundPointerOver"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 230, 230, 230));
                Application.Current.Resources["ButtonBackgroundPressed"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 210, 210, 210));
                Application.Current.Resources["ButtonBorderBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 200, 200, 200));
                Application.Current.Resources["ButtonBorderBrushPointerOver"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 180, 180, 180));
                
                // Clear the root grid background if previously set
                if (this.Content is Grid rootGrid)
                {
                    rootGrid.Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"];
                }

                // Trigger a UI update by toggling the theme
                if (this.Content is FrameworkElement root)
                {
                    root.RequestedTheme = ElementTheme.Dark;
                    root.RequestedTheme = ElementTheme.Light;
                }

                // 2. Brushed-metal title bar colour
                var titleBar = this.AppWindow.TitleBar;
                titleBar.BackgroundColor         = Windows.UI.Color.FromArgb(255, 226, 226, 230);
                titleBar.InactiveBackgroundColor = Windows.UI.Color.FromArgb(255, 245, 245, 247);
                titleBar.ForegroundColor         = Windows.UI.Color.FromArgb(255, 28, 28, 30);
                titleBar.InactiveForegroundColor = Windows.UI.Color.FromArgb(255, 142, 142, 147);
                titleBar.ButtonBackgroundColor   = Windows.UI.Color.FromArgb(0, 0, 0, 0); // transparent
                titleBar.ButtonForegroundColor   = Windows.UI.Color.FromArgb(255, 28, 28, 30);
                titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(30, 0, 0, 0);

                // 3. Disable Mica so the standard background shows
                this.SystemBackdrop = null;
            }
            catch { }
        }

        private void RemoveAquaTheme()
        {
            try
            {
                // Restore standard resources
                Application.Current.Resources.Remove("SystemAccentColor");
                Application.Current.Resources.Remove("ApplicationPageBackgroundThemeBrush");
                Application.Current.Resources.Remove("CardBackgroundFillColorDefaultBrush");
                Application.Current.Resources.Remove("ControlFillColorDefaultBrush");
                Application.Current.Resources.Remove("ButtonBackgroundPointerOver");
                Application.Current.Resources.Remove("ButtonBackgroundPressed");
                Application.Current.Resources.Remove("ButtonBorderBrush");
                Application.Current.Resources.Remove("ButtonBorderBrushPointerOver");

                // Trigger a UI update by toggling the theme
                if (this.Content is FrameworkElement root)
                {
                    var settings = ApplicationData.Current.LocalSettings.Values;
                    string theme = (settings["AppTheme"] as string) ?? "System";
                    root.RequestedTheme = theme switch {
                        "Light" => ElementTheme.Light,
                        "Dark" => ElementTheme.Dark,
                        _ => ElementTheme.Default
                    };
                }

                // Restore normal page background
                if (this.Content is Grid rootGrid)
                    rootGrid.Background = null; // let WinUI ThemeResource take over

                // Restore transparent title bar
                var titleBar = this.AppWindow.TitleBar;
                titleBar.BackgroundColor         = Microsoft.UI.Colors.Transparent;
                titleBar.InactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
                titleBar.ButtonBackgroundColor   = Microsoft.UI.Colors.Transparent;

                // Restore Mica if enabled in settings
                var s = ApplicationData.Current.LocalSettings.Values;
                bool micaOn = (s["MicaEnabled"] as bool?) ?? true;
                this.SystemBackdrop = micaOn
                    ? (Microsoft.UI.Xaml.Media.SystemBackdrop)new Microsoft.UI.Xaml.Media.MicaBackdrop()
                    : new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
            }
            catch { }
        }

        // Auto-apply language selection
        private void LanguageSelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            string lang = (LanguageSelector.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "System";
            LanguageManager.Instance.SetLanguage(lang);
            CheckFFmpegStatus();
            UpdateWebViewTheme(); // Also re-injects the HTML to update the placeholder text
        }

        // Auto-apply parallel limit selection
        private void ParallelLimitSelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            var settings = ApplicationData.Current.LocalSettings.Values;
            int parallelLimit = (ParallelLimitSelector.SelectedItem as ComboBoxItem)?.Tag as int? ?? 15;
            settings["ParallelChunkLimit"] = parallelLimit;
        }

        // Legacy handler kept for backward compatibility
        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            NavigateToHome(sender, e);
        }

        private void ResetParallelLimit_Click(object sender, RoutedEventArgs e)
        {
            if (ParallelLimitSelector.Items.Count >= 15)
                ParallelLimitSelector.SelectedIndex = 14;
        }

        private async void SupportCreator_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://youtube.com/@HeartNovelsHindiFM"));
        }

        // Shared helper: shows in-app email picker dialog and launches chosen service
        private async Task ShowEmailPickerAsync(
            string dialogTitleKey, string dialogDescKey,
            string emailSubject, string emailBody)
        {
            var lang = LanguageManager.Instance;
            string to = "balveer09@outlook.com";
            string subject = Uri.EscapeDataString(emailSubject);
            string body    = Uri.EscapeDataString(emailBody);

            var panel = new StackPanel { Spacing = 6 };
            panel.Children.Add(new TextBlock
            {
                Text = lang[dialogDescKey],
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                Opacity = 0.7,
                Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 6)
            });

            var options = new (string Label, string? Url, string Glyph)[]
            {
                ("Gmail",       $"https://mail.google.com/mail/?view=cm&to={to}&su={subject}&body={body}", "\uE715"),
                ("Outlook Web", $"https://outlook.live.com/mail/0/deeplink/compose?to={to}&subject={subject}&body={body}", "\uE715"),
                ("Yahoo Mail",  $"https://compose.mail.yahoo.com/?to={to}&subject={subject}&body={body}", "\uE715"),
                (lang["ReportViaOther"], null, "\uE8F4"),
            };

            var dialog = new ContentDialog
            {
                Title           = lang[dialogTitleKey],
                CloseButtonText = lang["Cancel"],
                XamlRoot        = this.Content.XamlRoot,
                DefaultButton   = ContentDialogButton.Close
            };

            string? chosenUrl = null;
            bool usePickerApp = false;

            foreach (var (label, url, glyph) in options)
            {
                var btn = new Button
                {
                    HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
                    Padding      = new Microsoft.UI.Xaml.Thickness(16, 10, 16, 10),
                    CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                    Margin       = new Microsoft.UI.Xaml.Thickness(0, 2, 0, 2),
                };
                var row = new StackPanel { Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal, Spacing = 12 };
                row.Children.Add(new FontIcon { Glyph = glyph, FontSize = 16 });
                row.Children.Add(new TextBlock { Text = label, FontSize = 14, VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center });
                btn.Content = row;

                var capturedUrl    = url;
                var capturedPicker = (url == null);
                btn.Click += (s, _) =>
                {
                    chosenUrl    = capturedUrl;
                    usePickerApp = capturedPicker;
                    dialog.Hide();
                };
                panel.Children.Add(btn);
            }

            dialog.Content = panel;
            await dialog.ShowAsync();

            if (usePickerApp)
            {
                var opts = new Windows.System.LauncherOptions { DisplayApplicationPicker = true };
                await Windows.System.Launcher.LaunchUriAsync(
                    new Uri($"mailto:{to}?subject={subject}&body={body}"), opts);
            }
            else if (chosenUrl != null)
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(chosenUrl));
            }
        }

        // Policy 11.16: Report Inappropriate AI-Generated Content
        private async void ReportContent_Click(object sender, RoutedEventArgs e)
        {
            var lang = LanguageManager.Instance;
            await ShowEmailPickerAsync(
                "ReportPickEmailTitle", "ReportPickEmailDesc",
                lang["ReportEmailSubject"],
                lang["ReportEmailBody"]);
        }

        // Send Feedback to developer
        private async void FeedbackContent_Click(object sender, RoutedEventArgs e)
        {
            var lang = LanguageManager.Instance;
            await ShowEmailPickerAsync(
                "FeedbackPickEmailTitle", "FeedbackPickEmailDesc",
                lang["FeedbackEmailSubject"],
                lang["FeedbackEmailBody"]);
        }

        // History filter by date period
        private void FilterDateSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            if (FilterDateSelector?.SelectedItem is ComboBoxItem item)
            {
                _selectedPeriod = item.Content?.ToString() ?? "All Records";
                ApplyAdvancedFilters();
            }
        }

        // Apply date + text search filters to history
        private void ApplyAdvancedFilters()
        {
            if (!_isLoaded || HistorySearchBox == null) return;
            var filtered = _fullHistory.AsEnumerable();
            DateTime now = DateTime.Now.Date;
            if (_selectedPeriod == "Today") filtered = filtered.Where(x => x.Timestamp.Date == now);
            else if (_selectedPeriod == "Yesterday") filtered = filtered.Where(x => x.Timestamp.Date == now.AddDays(-1));
            else if (_selectedPeriod == "Last Week") filtered = filtered.Where(x => x.Timestamp.Date >= now.AddDays(-7));
            string query = HistorySearchBox.Text.ToLower();
            if (!string.IsNullOrEmpty(query)) filtered = filtered.Where(x => x.FileName.ToLower().Contains(query));
            UpdateHistoryUI(filtered.ToList());
        }

        private void HistorySearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) => ApplyAdvancedFilters();

        // Sort history list by selected criterion
        private void SortSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            if (SortSelector?.SelectedItem is ComboBoxItem selected)
            {
                string option = selected.Content?.ToString() ?? "";
                List<HistoryItem> sorted;
                switch (option)
                {
                    case "Date (Newest)": sorted = _fullHistory.OrderByDescending(x => x.Timestamp).ToList(); break;
                    case "Date (Oldest)": sorted = _fullHistory.OrderBy(x => x.Timestamp).ToList(); break;
                    case "Name (A-Z)": sorted = _fullHistory.OrderBy(x => x.FileName).ToList(); break;
                    case "Char Count": sorted = _fullHistory.OrderByDescending(x => x.CharCount).ToList(); break;
                    default: sorted = _fullHistory; break;
                }
                UpdateHistoryUI(sorted);
            }
        }

        private void UpdateHistoryUI(List<HistoryItem> list)
        {
            _historyList.Clear();
            int index = 1;
            foreach (var item in list)
            {
                item.SrNo = index++;
                _historyList.Add(item);
            }
        }

        private void OpenHistoryFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is HistoryItem item) {
                try {
                    if (File.Exists(item.FilePath)) {
                        Process.Start(new ProcessStartInfo { FileName = item.FilePath, UseShellExecute = true });
                    }
                } catch { }
            }
        }

        private async void OpenHistoryFolder_Click(object sender, RoutedEventArgs e)
        {
             if (sender is Button btn && btn.DataContext is HistoryItem item) {
                try {
                    string? dir = Path.GetDirectoryName(item.FilePath);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir)) {
                        await Windows.System.Launcher.LaunchFolderPathAsync(dir);
                    }
                } catch { }
            }
        }

        private async void DeleteHistoryItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is HistoryItem item) {
                if (!_skipDeleteConfirm)
                {
                    var chk = new CheckBox { Content = Lang.DeleteDontAsk, IsChecked = false };
                    var panel = new StackPanel { Spacing = 10 };
                    panel.Children.Add(new TextBlock { Text = string.Format(Lang.DeleteHistoryMsg, item.FileName), TextWrapping = TextWrapping.Wrap });
                    panel.Children.Add(chk);

                    var dialog = new ContentDialog
                    {
                        Title = Lang.DeleteHistoryTitle,
                        Content = panel,
                        PrimaryButtonText = Lang.BatchDelete,
                        CloseButtonText = Lang.Cancel,
                        XamlRoot = this.Content.XamlRoot
                    };

                    var result = await dialog.ShowAsync();
                    if (result != ContentDialogResult.Primary) return;
                    if (chk.IsChecked == true) _skipDeleteConfirm = true;
                }

                try
                {
                    if (File.Exists(item.FilePath))
                    {
                        File.Delete(item.FilePath);
                    }
                }
                catch { }

                _fullHistory.Remove(item);
                ApplyAdvancedFilters();
                SaveHistory();
            }
        }

        // Fetch voices from Python engine and populate list
        private async void SyncVoices()
        {
            if (!CheckInternetConnection(true)) {
                SelectedVoiceName.Text = "Sync Failed (No Internet)";
                return;
            }

            SelectedVoiceName.Text = _isInitialSync ? "Syncing Engine..." : "Refreshing...";
            _isInitialSync = false;
            
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Engine", "tts_engine.exe"),
                Arguments = "--list-voices",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            try {
                using Process? process = Process.Start(start);
                if (process == null) return;

                string json = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                var voices = JsonSerializer.Deserialize<List<VoiceModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (voices != null && voices.Count > 0)
                {
                    _allVoices = voices;
                    UpdateListView(_allVoices);
                    
                    string? savedVoice = ApplicationData.Current.LocalSettings.Values["SelectedVoice"] as string;
                    VoiceModel? voiceToSelect = null;
                    if (!string.IsNullOrEmpty(savedVoice)) {
                        voiceToSelect = _allVoices.FirstOrDefault(v => v.ShortName == savedVoice);
                    }
                    
                    if (voiceToSelect != null) {
                        VoiceListView.SelectedItem = voiceToSelect;
                        _selectedVoice = voiceToSelect;
                        SelectedVoiceName.Text = voiceToSelect.Name;
                        CurrentVoiceLabel.Text = voiceToSelect.Name;
                    } else {
                        SelectedVoiceName.Text = "Select a Narrator";
                    }
                }
                else
                {
                    throw new Exception("No voices returned.");
                }
            } catch {
                SelectedVoiceName.Text = "Sync Failed!";
                DispatcherQueue.TryEnqueue(() => {
                    FailureTitle.Text = "No Internet Connection";
                    FailureReason.Text = "Narraz Studio requires an active internet connection to sync voices and generate audio. Please check your connection and try again.";
                    FailurePrimaryBtn.Content = "OK";
                    _isInternetError = true;
                    ShowOverlayState("Failure");
                    FailureSecondaryBtn.Visibility = Visibility.Collapsed;
                });
            }
        }

        // Group voices by locale and bind to list
        private void UpdateListView(List<VoiceModel> sourceList)
        {
            var groups = sourceList
                .GroupBy(v => v.Locale)
                .Select(g => new VoiceGroup { 
                    LanguageName = GetLanguageFriendlyName(g.Key), 
                    Voices = new ObservableCollection<VoiceModel>(g) 
                })
                .OrderBy(g => g.LanguageName)
                .ToList();

            _groupedVoices.Source = groups;
            VoiceListView.ItemsSource = _groupedVoices.View;
        }

        private string GetLanguageFriendlyName(string locale)
        {
            try {
                return new System.Globalization.CultureInfo(locale).NativeName.ToUpper() + " [" + locale.ToUpper() + "]";
            } catch {
                return locale.ToUpper();
            }
        }

        private void VoiceSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filter = VoiceSearchBox.Text.ToLower();
            var filtered = _allVoices.Where(v => v.Name.ToLower().Contains(filter)).ToList();
            UpdateListView(filtered);
        }

        private void VoiceListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is VoiceModel selected)
            {
                VoiceListView.SelectedItem = selected;
                _selectedVoice = selected;
                SelectedVoiceName.Text = selected.Name;
                CurrentVoiceLabel.Text = selected.Name;
                try {
                    ApplicationData.Current.LocalSettings.Values["SelectedVoice"] = selected.ShortName;
                } catch { }
                VoiceSelectorBtn.Flyout.Hide();
            }
        }

        private void RefreshVoicesBtn_Click(object sender, RoutedEventArgs e) => SyncVoices();

        // Read raw text from WebView editor
        private async Task<string> GetRawTextAsync()
        {
            if (EditorWebView?.CoreWebView2 == null) return "";
            try {
                string json = await EditorWebView.ExecuteScriptAsync("document.getElementById('editor').value;");
                if (string.IsNullOrEmpty(json) || json == "null") return "";
                return JsonSerializer.Deserialize<string>(json) ?? "";
            } catch { return ""; }
        }

        // --- Batch Export Logic ---
        private async void BatchOpenFiles_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.List;
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".txt");
            
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var files = await picker.PickMultipleFilesAsync();
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    await AddFileToBatchAsync(file);
                }
            }
        }

        private async Task AddFileToBatchAsync(Windows.Storage.StorageFile file)
        {
            if (!_batchItems.Any(x => x.FilePath == file.Path))
            {
                var props = await file.GetBasicPropertiesAsync();
                var newItem = new BatchExportItem 
                { 
                    FileName = file.Name, 
                    FilePath = file.Path, 
                    FileSizeBytes = (long)props.Size,
                    Progress = 0,
                    IsProcessing = false
                };
                newItem.StateChanged = UpdateBatchMasterButtons;
                _batchItems.Add(newItem);
            }
        }

        private void BatchFilesList_DragItemsStarting(object sender, Microsoft.UI.Xaml.Controls.DragItemsStartingEventArgs e)
        {
            if (_isBatchProcessing)
            {
                e.Cancel = true;
            }
        }

        private void BatchFilesList_DragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
        {
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            }
        }

        private async void BatchFilesList_Drop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
        {
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                bool invalidFound = false;

                foreach (var item in items)
                {
                    if (item is Windows.Storage.StorageFile file)
                    {
                        if (file.FileType.ToLower() == ".txt")
                        {
                            await AddFileToBatchAsync(file);
                        }
                        else
                        {
                            invalidFound = true;
                        }
                    }
                }

                if (invalidFound)
                {
                    ShowInvalidFilePopup();
                }
            }
        }

        private void UpdateBatchMasterButtons()
        {
            DispatcherQueue.TryEnqueue(() => {
                if (BatchEmptyStatePanel != null && BatchFilesList != null)
                {
                    BatchEmptyStatePanel.Visibility = _batchItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                    BatchFilesList.Visibility = _batchItems.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                }

                if (_batchItems.Count > 0 && _batchItems.All(i => i.IsCompleted))
                {
                    if (BatchExportAllBtn != null) BatchExportAllBtn.Visibility = Visibility.Collapsed;
                    if (BatchLocateAllBtn != null) BatchLocateAllBtn.Visibility = Visibility.Visible;
                }
                else
                {
                    if (BatchExportAllBtn != null) BatchExportAllBtn.Visibility = !_isBatchProcessing ? Visibility.Visible : Visibility.Collapsed;
                    if (BatchLocateAllBtn != null) BatchLocateAllBtn.Visibility = Visibility.Collapsed;
                }
            });
        }

        private void BatchClearAll_Click(object sender, RoutedEventArgs e)
        {
            _batchCts?.Cancel();
            try { _currentExportProcess?.Kill(); } catch { }
            foreach (var item in _batchItems) { item.ItemCts?.Cancel(); }
            _batchItems.Clear();
            _isBatchProcessing = false;
            if (BatchCancelAllBtn != null) BatchCancelAllBtn.Visibility = Visibility.Collapsed;
            UpdateBatchMasterButtons();
        }

        private void BatchLocateAll_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_batchSavePath) && Directory.Exists(_batchSavePath))
            {
                Process.Start("explorer.exe", $"\"{_batchSavePath}\"");
            }
        }

        private async Task TriggerBatchSavePathAsync()
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");

            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                _batchSavePath = folder.Path;
                BatchSavePathText.Text = folder.Path;
            }
        }

        private async void BatchSelectSavePath_Click(object sender, RoutedEventArgs e)
        {
            await TriggerBatchSavePathAsync();
        }

        private void BatchDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is BatchExportItem item)
            {
                _batchItems.Remove(item);
            }
        }

        private async void BatchConvertSingle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is BatchExportItem item)
            {
                if (string.IsNullOrEmpty(_batchSavePath))
                {
                    BatchSavePathText.Text = "Please select a save path first!";
                    BatchSavePathText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                    return;
                }
                BatchSavePathText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);

                if (_selectedVoice == null) return;
                
                int rateVal = (int)RateSlider.Value;
                int pitchVal = (int)PitchSlider.Value;
                string rate = $"{(rateVal >= 0 ? "+" : "")}{rateVal}%";
                string pitch = $"{(pitchVal >= 0 ? "+" : "")}{pitchVal}Hz";

                if (_isBatchProcessing)
                {
                    item.IsQueued = true;
                    item.IsCompleted = false;
                    item.StatusText = "Queued...";
                }
                else
                {
                    item.IsProcessing = true;
                    item.IsQueued = false;
                    item.IsCompleted = false;
                    item.Progress = 0;
                    item.StatusText = "Preparing...";
                    item.ItemCts = new System.Threading.CancellationTokenSource();

                    bool ok = await ProcessBatchItemExport(item, _selectedVoice.ShortName, rate, pitch);
                    
                    item.IsProcessing = false;
                    if (ok && !item.ItemCts.IsCancellationRequested)
                    {
                        item.IsCompleted = true;
                        item.StatusText = "Completed";
                        try {
                            string fullText = await File.ReadAllTextAsync(item.FilePath);
                            int chars = fullText.Length;
                            int words = fullText.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                            string safePath = "";
                            try { safePath = item.OutputFilePath; } catch { }
                            AddToHistory(Path.GetFileName(item.OutputFilePath), safePath, chars, words);
                        } catch { }
                        DispatcherQueue.TryEnqueue(() => {
                            AddToCompletedTab(item);
                            _batchItems.Remove(item);
                        });
                    }
                    else if (item.ItemCts.IsCancellationRequested)
                    {
                        item.StatusText = "Cancelled";
                    }
                    else
                    {
                        item.StatusText = "Failed";
                    }
                }
            }
        }

        private async void BatchExportAll_Click(object sender, RoutedEventArgs e)
        {
            bool isFolderMissing = string.IsNullOrEmpty(_batchSavePath);
            bool isFilesMissing = _batchItems.Count == 0;

            if (isFolderMissing && isFilesMissing)
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = Lang.WarningTitle,
                    Content = Lang.WarningNoFilesOrFolder,
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }
            else if (isFolderMissing)
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = Lang.WarningTitle,
                    Content = Lang.WarningNoFolder,
                    PrimaryButtonText = Lang.SelectFolderBtn,
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await TriggerBatchSavePathAsync();
                }
                return;
            }
            else if (isFilesMissing)
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = Lang.WarningTitle,
                    Content = Lang.WarningNoFiles,
                    PrimaryButtonText = Lang.AddFilesBtn,
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    BatchOpenFiles_Click(this, new RoutedEventArgs());
                }
                return;
            }

            if (_selectedVoice == null) return;

            _isBatchProcessing = true;
            _batchCts = new System.Threading.CancellationTokenSource();
            BatchExportAllBtn.Visibility = Visibility.Collapsed;
            BatchCancelAllBtn.Visibility = Visibility.Visible;

            int rateVal = (int)RateSlider.Value;
            int pitchVal = (int)PitchSlider.Value;
            string rate = $"{(rateVal >= 0 ? "+" : "")}{rateVal}%";
            string pitch = $"{(pitchVal >= 0 ? "+" : "")}{pitchVal}Hz";

            foreach (var item in _batchItems)
            {
                if (!item.IsCompleted)
                {
                    item.IsQueued = true;
                    item.StatusText = "Queued...";
                }
            }

            while (true)
            {
                if (_batchCts.IsCancellationRequested) break;
                var item = _batchItems.FirstOrDefault(x => x.IsQueued && !x.IsProcessing && !x.IsCompleted);
                if (item == null) break;

                item.IsProcessing = true;
                item.IsQueued = false;
                item.Progress = 0;
                item.StatusText = "Preparing...";
                item.ItemCts = new System.Threading.CancellationTokenSource();
                
                bool ok = await ProcessBatchItemExport(item, _selectedVoice.ShortName, rate, pitch);
                
                item.IsProcessing = false;
                if (ok && !item.ItemCts.IsCancellationRequested)
                {
                    item.IsCompleted = true;
                    item.StatusText = "Completed";
                    try {
                        string fullText = await File.ReadAllTextAsync(item.FilePath);
                        int chars = fullText.Length;
                        int words = fullText.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                        string safePath = "";
                        try { safePath = item.OutputFilePath; } catch { }
                        AddToHistory(Path.GetFileName(item.OutputFilePath), safePath, chars, words);
                    } catch { }
                    DispatcherQueue.TryEnqueue(() => {
                        AddToCompletedTab(item);
                        _batchItems.Remove(item);
                    });
                }
                else if (item.ItemCts.IsCancellationRequested)
                {
                    item.StatusText = "Cancelled";
                }
                else
                {
                    item.StatusText = "Failed";
                }
            }

            _isBatchProcessing = false;
            BatchExportAllBtn.Visibility = Visibility.Visible;
            BatchCancelAllBtn.Visibility = Visibility.Collapsed;
        }

        private void BatchCancelAll_Click(object sender, RoutedEventArgs e)
        {
            _batchCts?.Cancel();
            try { _currentExportProcess?.Kill(); } catch { }
            
            foreach (var item in _batchItems)
            {
                if (item.IsQueued)
                {
                    item.IsQueued = false;
                    item.StatusText = "Cancelled";
                }
                if (item.IsProcessing)
                {
                    item.ItemCts?.Cancel();
                }
            }
        }

        private void BatchCancelSingle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is BatchExportItem item)
            {
                item.ItemCts?.Cancel();
                item.IsQueued = false;
                item.IsProcessing = false;
                item.StatusText = "Cancelled";
                if (item == _batchItems.FirstOrDefault(x => x.IsProcessing))
                {
                    try { _currentExportProcess?.Kill(); } catch { }
                }
            }
        }

        private void BatchLocateSingle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is BatchExportItem item)
            {
                if (!string.IsNullOrEmpty(item.OutputFilePath) && File.Exists(item.OutputFilePath))
                {
                    Process.Start("explorer.exe", $"/select,\"{item.OutputFilePath}\"");
                }
            }
        }

        private void AddToCompletedTab(BatchExportItem item)
        {
            long size = 0;
            try { if (File.Exists(item.OutputFilePath)) size = new FileInfo(item.OutputFilePath).Length; } catch { }
            var completed = new BatchCompletedItem
            {
                SrNo = (_completedItems.Count + 1).ToString(),
                FileName = item.FileName,
                OutputFilePath = item.OutputFilePath,
                VoiceName = _selectedVoice?.Name ?? "",
                DateString = DateTime.Now.ToString("dd MMM yyyy"),
                TimeString = DateTime.Now.ToString("hh:mm tt"),
                FileSizeStr = size > 0 ? $"{size / 1024} KB" : "—"
            };
            _completedItems.Add(completed);
        }

        private void BatchTabConverting_Click(object sender, RoutedEventArgs e)
        {
            BatchConvertingPanel.Visibility = Visibility.Visible;
            BatchFinishedPanel.Visibility = Visibility.Collapsed;
            BatchTabConvertingIndicator.Opacity = 1;
            BatchTabFinishedIndicator.Opacity = 0;
            BatchTabConvertingText.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
            BatchTabFinishedText.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
        }

        private void BatchTabFinished_Click(object sender, RoutedEventArgs e)
        {
            BatchConvertingPanel.Visibility = Visibility.Collapsed;
            BatchFinishedPanel.Visibility = Visibility.Visible;
            BatchTabConvertingIndicator.Opacity = 0;
            BatchTabFinishedIndicator.Opacity = 1;
            BatchTabConvertingText.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
            BatchTabFinishedText.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        }

        private async void CompletedOpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is BatchCompletedItem item)
            {
                if (string.IsNullOrEmpty(item.OutputFilePath) || !File.Exists(item.OutputFilePath)) return;

                // Build list of apps that can open audio files
                var apps = new List<(string Name, string Executable, string Args)>
                {
                    ("Windows Media Player", "wmplayer.exe", $"\"{item.OutputFilePath}\""),
                    ("Groove Music", "shell:AppsFolder\\Microsoft.ZuneMusic_8wekyb3d8bbwe!Microsoft.ZuneMusic", $"\"{item.OutputFilePath}\""),
                    ("VLC Media Player", "vlc", $"\"{item.OutputFilePath}\""),
                    ("Winamp", "winamp.exe", $"\"{item.OutputFilePath}\""),
                    ("foobar2000", "foobar2000.exe", $"\"{item.OutputFilePath}\""),
                    ("Open with default app", "", "")
                };

                // Build dialog with radio buttons
                var panel = new StackPanel { Spacing = 8 };
                var radioButtons = new List<RadioButton>();
                foreach (var app in apps)
                {
                    var rb = new RadioButton { Content = app.Name, Tag = app };
                    if (radioButtons.Count == 0) rb.IsChecked = true;
                    radioButtons.Add(rb);
                    panel.Children.Add(rb);
                }

                var dialog = new ContentDialog
                {
                    Title = "Open With",
                    Content = new ScrollViewer { Content = panel, MaxHeight = 300 },
                    PrimaryButtonText = "Open",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.Content.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    var selected = radioButtons.FirstOrDefault(r => r.IsChecked == true);
                    if (selected?.Tag is (string name, string exe, string args))
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(exe))
                            {
                                // Default app
                                await Windows.System.Launcher.LaunchFileAsync(await Windows.Storage.StorageFile.GetFileFromPathAsync(item.OutputFilePath));
                            }
                            else if (exe.StartsWith("shell:"))
                            {
                                Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = exe + " " + args, UseShellExecute = true });
                            }
                            else
                            {
                                Process.Start(new ProcessStartInfo { FileName = exe, Arguments = args, UseShellExecute = true });
                            }
                        }
                        catch { }
                    }
                }
            }
        }

        private void CompletedLocate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is BatchCompletedItem item)
            {
                if (!string.IsNullOrEmpty(item.OutputFilePath))
                    Process.Start("explorer.exe", $"/select,\"{item.OutputFilePath}\"");
            }
        }

        private async void CompletedDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is BatchCompletedItem item)
            {
                if (!_skipDeleteConfirm)
                {
                    var chk = new CheckBox { Content = Lang.DeleteDontAsk, IsChecked = false };
                    var panel = new StackPanel { Spacing = 10 };
                    panel.Children.Add(new TextBlock { Text = string.Format(Lang.DeleteHistoryMsg, item.FileName), TextWrapping = TextWrapping.Wrap });
                    panel.Children.Add(chk);

                    var dialog = new ContentDialog
                    {
                        Title = Lang.DeleteHistoryTitle,
                        Content = panel,
                        PrimaryButtonText = Lang.BatchDelete,
                        CloseButtonText = Lang.Cancel,
                        XamlRoot = this.Content.XamlRoot
                    };

                    var result = await dialog.ShowAsync();
                    if (result != ContentDialogResult.Primary) return;
                    if (chk.IsChecked == true) _skipDeleteConfirm = true;
                }

                try { if (File.Exists(item.OutputFilePath)) File.Delete(item.OutputFilePath); } catch { }
                _completedItems.Remove(item);

                // Re-number remaining items
                for (int i = 0; i < _completedItems.Count; i++)
                    _completedItems[i].SrNo = (i + 1).ToString();
            }
        }

        private async Task<bool> ProcessBatchItemExport(BatchExportItem item, string voice, string rate, string pitch)
        {
            string fullText = "";
            try { fullText = await File.ReadAllTextAsync(item.FilePath); } catch { return false; }
            if (string.IsNullOrWhiteSpace(fullText)) return false;

            string itemId = Path.GetFileNameWithoutExtension(item.FileName) + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string configPath = Path.Combine(_appDataPath, $"batch_config_{itemId}.json");
            string textPath = Path.Combine(_appDataPath, $"batch_input_{itemId}.txt");
            string outputFileName = Path.GetFileNameWithoutExtension(item.FileName) + ".mp3";
            string outputPath = Path.Combine(_batchSavePath, outputFileName);
            item.OutputFilePath = outputPath;
            
            int parallelLimit = (ApplicationData.Current.LocalSettings.Values["ParallelChunkLimit"] as int?) ?? 15;

            var config = new
            {
                text_file = textPath,
                text = fullText.Length < 100000 ? fullText : "[large text in file]",
                voice = voice,
                output_file = outputPath,
                rate = rate,
                pitch = pitch,
                parallel_limit = parallelLimit
            };
            
            await File.WriteAllTextAsync(textPath, fullText);
            await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(config));

            string installPath = AppDomain.CurrentDomain.BaseDirectory;
            string ffmpegDir = Path.Combine(installPath, "Engine");

            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Engine", "tts_engine.exe"),
                Arguments = $"\"{configPath}\" \"{ffmpegDir}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
                CreateNoWindow = true,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            bool success = false;

            try
            {
                _currentExportProcess = new Process { StartInfo = start };
                if (_currentExportProcess == null) return false;

                _currentExportProcess.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        if (e.Data.StartsWith("STATUS:"))
                        {
                            var statusMsg = e.Data.Substring(7).Trim();
                            if (statusMsg.StartsWith("Completed chunk"))
                            {
                                var parts = statusMsg.Split('/');
                                if (parts.Length == 2 && int.TryParse(parts[0].Split(' ').Last(), out int c) && int.TryParse(parts[1], out int t))
                                {
                                    DispatcherQueue.TryEnqueue(() => {
                                        if (t > 0) item.Progress = (double)c / t * 100;
                                        item.StatusText = string.Format(LanguageManager.Instance["CompletedChunk"], c, t);
                                    });
                                }
                            }
                            else
                            {
                                DispatcherQueue.TryEnqueue(() => {
                                    item.StatusText = statusMsg;
                                });
                            }
                        }
                        else if (e.Data.StartsWith("RESULT:"))
                        {
                            success = e.Data.Contains("SUCCESS");
                        }
                    }
                };

                _currentExportProcess.ErrorDataReceived += (s, e) => {
                    if (!string.IsNullOrEmpty(e.Data)) {
                        Console.WriteLine("Batch Export PYTHON ERR: " + e.Data);
                    }
                };

                _currentExportProcess.Start();
                _currentExportProcess.BeginOutputReadLine();
                _currentExportProcess.BeginErrorReadLine();
                
                try {
                    await _currentExportProcess.WaitForExitAsync(item.ItemCts != null ? item.ItemCts.Token : default);
                    await Task.Delay(200);
                } catch (TaskCanceledException) {
                    try { _currentExportProcess.Kill(); } catch { }
                    return false;
                }
            }
            catch { }
            finally
            {
                if (_currentExportProcess != null)
                {
                    try { _currentExportProcess.Dispose(); } catch { }
                    _currentExportProcess = null;
                }
                try { if (File.Exists(configPath)) File.Delete(configPath); } catch { }
                try { if (File.Exists(textPath)) File.Delete(textPath); } catch { }
            }

            return success;
        }

        private async Task SetRawTextAsync(string text)
        {
            if (EditorWebView?.CoreWebView2 != null) {
                string escaped = JsonSerializer.Serialize(text);
                await EditorWebView.ExecuteScriptAsync($"document.getElementById('editor').value = {escaped}; window.chrome.webview.postMessage('text_changed');");
            }
        }

        // Preview audio in playback bar
        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckInternetConnection(true)) return;

            if (_selectedVoice == null) {
                SelectedVoiceName.Text = "🚨 Please Select a Narrator!";
                VoiceSelectorBtn.Flyout.ShowAt(VoiceSelectorBtn);
                return;
            }
            
            string fullText = await GetRawTextAsync();
            if (string.IsNullOrEmpty(fullText)) return;

            PlayButton.IsEnabled = false;
            StatusLabel.Text = "Preparing Audio...";

            StopPlayback();
            await Task.Delay(100);

            string tempPlayPath = Path.Combine(_appDataPath, "narraz_preview.mp3");
            try { if (File.Exists(tempPlayPath)) File.Delete(tempPlayPath); } catch { }

            int rateVal = (int)RateSlider.Value;
            int pitchVal = (int)PitchSlider.Value;
            string rate = $"{(rateVal >= 0 ? "+" : "")}{rateVal}%";
            string pitch = $"{(pitchVal >= 0 ? "+" : "")}{pitchVal}Hz";

            bool ok = await ProcessExport(fullText, _selectedVoice.ShortName, tempPlayPath, rate, pitch);

            if (ok) {
                DispatcherQueue.TryEnqueue(() => {
                    BottomBarRow.Height = new GridLength(80);
                    PlaybackBar.Visibility = Visibility.Visible; 
                    if (_mediaPlayer != null)
                    {
                        _mediaPlayer.Source = MediaSource.CreateFromUri(new Uri(tempPlayPath));
                        _mediaPlayer.Play();
                    }
                    PlayPauseButton.Content = "\uE769";
                    _playbackTimer?.Start();
                });
                StatusLabel.Text = "Playing...";
            } else {
                StatusLabel.Text = FailureReason.Text;
            }
            
            PlayButton.IsEnabled = true;
        }

        // Export narration to user-selected MP3 file
        private async void ExportButton_Click(object sender, RoutedEventArgs e) 
        { 
            if (!CheckInternetConnection(true)) return;

            string fullText = await GetRawTextAsync();
            if (string.IsNullOrWhiteSpace(fullText)) return;

            if (_selectedVoice == null) {
                SelectedVoiceName.Text = "🚨 Please Select a Narrator!";
                VoiceSelectorBtn.Flyout.ShowAt(VoiceSelectorBtn);
                return;
            }

            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            savePicker.FileTypeChoices.Add("MP3 Audio", new List<string>() { ".mp3" });
            savePicker.SuggestedFileName = "Narraz_Narration_" + DateTime.Now.ToString("yyyyMMdd_HHmm");

            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(savePicker, hwnd);

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                ShowOverlayState("Progress");
                StatusChunkLabel.Text = "";
                
                try 
                {
                    string voice = _selectedVoice.ShortName;
                    int rateVal = (int)RateSlider.Value;
                    int pitchVal = (int)PitchSlider.Value;
                    string rate = $"{(rateVal >= 0 ? "+" : "")}{rateVal}%";
                    string pitch = $"{(pitchVal >= 0 ? "+" : "")}{pitchVal}Hz";
                    
                    // Write to internal path then copy to brokered file
                    string internalOutput = Path.Combine(_appDataPath, "temp_export.mp3");
                    bool success = await ProcessExport(fullText, voice, internalOutput, rate, pitch);

                    if (success)
                    {
                        using (var stream = await file.OpenStreamForWriteAsync())
                        {
                            using (var sourceStream = File.OpenRead(internalOutput))
                            {
                                await sourceStream.CopyToAsync(stream);
                            }
                        }
                        
                        string safePath = "";
                        try { safePath = file.Path; } catch { }
                        
                        SuccessFileName.Text = file.Name;
                        SuccessFilePath.Text = safePath;
                        
                        try {
                            int chars = fullText.Length;
                            int words = fullText.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                            AddToHistory(file.Name, safePath, chars, words);
                        } catch { }

                        ShowOverlayState("Success");
                    }
                    else
                    {
                        try { await file.DeleteAsync(); } catch { }
                        if (string.IsNullOrEmpty(FailureReason.Text) || FailureReason.Text == "Something went wrong.")
                            FailureReason.Text = "Python engine failed to merge audio.";
                        ShowOverlayState("Failure");
                    }
                }
                catch (Exception ex)
                {
                    FailureReason.Text = ex.Message;
                    ShowOverlayState("Failure");
                }
            }
        }

        private void FailurePrimaryBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isInternetError) {
                CloseOverlay_Click(sender, e);
            } else {
                ExportButton_Click(sender, e);
            }
        }

        // Run Python TTS engine and stream status updates
        private async Task<bool> ProcessExport(string fullText, string voice, string outputPath, string rate, string pitch)
        {
            string configPath = Path.Combine(_appDataPath, "export_config.json");
            string textPath = Path.Combine(_appDataPath, "export_input.txt");
            int parallelLimit = (ApplicationData.Current.LocalSettings.Values["ParallelChunkLimit"] as int?) ?? 15;
            var config = new
            {
                text_file = textPath,
                text = fullText.Length < 100000 ? fullText : "[large text in file]",
                voice = voice,
                output_file = outputPath,
                rate = rate,
                pitch = pitch,
                parallel_limit = parallelLimit
            };
            await File.WriteAllTextAsync(textPath, fullText);
            await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(config));

            string installPath = AppDomain.CurrentDomain.BaseDirectory;
            string ffmpegDir = Path.Combine(installPath, "Engine");

            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Engine", "tts_engine.exe"),
                Arguments = $"\"{configPath}\" \"{ffmpegDir}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
                CreateNoWindow = true,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            bool success = false;
            FailureReason.Text = "Something went wrong.";
            _isExportCancelled = false;

            try
            {
                _currentExportProcess = new Process { StartInfo = start };
                if (_currentExportProcess == null) return false;

                // Forward stdout status/result lines to UI
                _currentExportProcess.OutputDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            if (e.Data.StartsWith("STATUS:"))
                            {
                                string statusMsg = e.Data.Substring(7).Trim();
                                DispatcherQueue.TryEnqueue(() => {
                                    if (statusMsg.StartsWith("Completed chunk")) {
                                        // Parse c and t
                                        var parts = statusMsg.Split('/');
                                        if (parts.Length == 2) {
                                            string cStr = parts[0].Replace("Completed chunk", "").Trim();
                                            string tStr = parts[1].Trim();
                                            StatusChunkLabel.Text = string.Format(LanguageManager.Instance["CompletedChunk"], cStr, tStr);
                                        } else {
                                            StatusChunkLabel.Text = statusMsg;
                                        }
                                    } else {
                                        StatusDetailLabel.Text = statusMsg;
                                    }
                                });
                            }
                            else if (e.Data.StartsWith("RESULT: SUCCESS"))
                            {
                                success = true;
                            }
                            else if (e.Data.StartsWith("RESULT: ERROR - "))
                            {
                                string errStr = e.Data.Substring(16).Trim();
                                DispatcherQueue.TryEnqueue(() => {
                                    FailureReason.Text = errStr;
                                });
                            }
                        }
                };
                
                string capturedError = "";
                _currentExportProcess.ErrorDataReceived += (s, e) => {
                     if (!string.IsNullOrEmpty(e.Data)) {
                         capturedError += e.Data + "\n";
                     }
                };

                _currentExportProcess.Start();
                _currentExportProcess.BeginOutputReadLine();
                _currentExportProcess.BeginErrorReadLine();
                await _currentExportProcess.WaitForExitAsync();
                await Task.Delay(100);
                
                if (_isExportCancelled) return false;
                
                if (_currentExportProcess.ExitCode != 0) {
                        success = false;
                        if (!string.IsNullOrEmpty(capturedError)) {
                            string finalErr = capturedError.Length > 250 ? "..." + capturedError.Substring(capturedError.Length - 247) : capturedError;
                            DispatcherQueue.TryEnqueue(() => {
                                FailureReason.Text = finalErr;
                            });
                        }
                }
            }
            catch (Exception ex)
            {
                if (!_isExportCancelled) {
                    DispatcherQueue.TryEnqueue(() => {
                        FailureReason.Text = ex.Message;
                    });
                }
            }
            finally
            {
                if (_currentExportProcess != null)
                {
                    try { _currentExportProcess.Dispose(); } catch { }
                    _currentExportProcess = null;
                }
            }

            return success;
        }

        // Toggle overlay panel state: Progress / Success / Failure
        private void ShowOverlayState(string state)
        {
            ProgressOverlay.Visibility = Visibility.Visible;
            ExportProgressState.Visibility = state == "Progress" ? Visibility.Visible : Visibility.Collapsed;
            ExportSuccessState.Visibility = state == "Success" ? Visibility.Visible : Visibility.Collapsed;
            ExportFailureState.Visibility = state == "Failure" ? Visibility.Visible : Visibility.Collapsed;
            if (state == "Failure") {
                FailureOkBtn.Visibility = Visibility.Visible;
                FailurePrimaryBtn.Visibility = Visibility.Visible;
                FailureSecondaryBtn.Visibility = Visibility.Visible;
            }
        }

        private async void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try {
                string? folder = Path.GetDirectoryName(SuccessFilePath.Text);
                if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder)) {
                    await Windows.System.Launcher.LaunchFolderPathAsync(folder);
                }
            } catch { }
        }

        private void CloseOverlay_Click(object sender, RoutedEventArgs e) 
        {
            ProgressOverlay.Visibility = Visibility.Collapsed;
            _isInternetError = false;
            FailurePrimaryBtn.Content = "Try Again";
            FailurePrimaryBtn.Visibility = Visibility.Visible;
            FailureSecondaryBtn.Visibility = Visibility.Visible;
            FailureOkBtn.Visibility = Visibility.Visible;
            FailureTitle.Text = "Export Failed";
        }

        // Playback event handlers
        private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            DispatcherQueue.TryEnqueue(() => {
                PlayPauseButton.Content = "\uE768";
                _playbackTimer?.Stop();
                PlaybackSlider.Value = 0;
                TimeLabel.Text = "00:00 / 00:00";
                StatusLabel.Text = "Finished Playing";
            });
        }

        private void PlaybackTimer_Tick(DispatcherQueueTimer sender, object args)
        {
            try {
                if (_mediaPlayer != null && _mediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds > 0)
                {
                    var position = _mediaPlayer.PlaybackSession.Position;
                    var duration = _mediaPlayer.PlaybackSession.NaturalDuration;
                    _isUpdatingSlider = true;
                    PlaybackSlider.Maximum = duration.TotalSeconds;
                    PlaybackSlider.Value = position.TotalSeconds;
                    _isUpdatingSlider = false;
                    TimeLabel.Text = $"{position.ToString(@"mm\:ss")} / {duration.ToString(@"mm\:ss")}";
                }
            } catch { }
        }

        private void PlaybackSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!_isUpdatingSlider && _mediaPlayer != null && _mediaPlayer.PlaybackSession.CanSeek)
            {
                _mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(e.NewValue);
            }
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer == null) return;
            if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                _mediaPlayer.Pause();
                PlayPauseButton.Content = "\uE768";
            }
            else
            {
                _mediaPlayer.Play();
                PlayPauseButton.Content = "\uE769";
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer == null) return;
            _mediaPlayer.Pause();
            _mediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
            PlayPauseButton.Content = "\uE768";
            PlaybackSlider.Value = 0;
            TimeLabel.Text = "00:00 / 00:00";
            StatusLabel.Text = "Ready";
        }

        private void ClosePlaybackButton_Click(object sender, RoutedEventArgs e)
        {
            BottomBarRow.Height = new GridLength(0);
            PlaybackBar.Visibility = Visibility.Collapsed;
            StatusLabel.Text = "Ready";
            CleanTempFiles();
        }

        private void StopPlayback()
        {
            try {
                if (_mediaPlayer != null) {
                    _mediaPlayer.Pause();
                    _mediaPlayer.Source = null;
                }
                _playbackTimer?.Stop();
            } catch { }
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            try { _batchCts?.Cancel(); } catch { }
            try { if (_currentExportProcess != null && !_currentExportProcess.HasExited) _currentExportProcess.Kill(); } catch { }

            try { CleanTempFiles(); } catch { }

            try {
                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Dispose();
                    _mediaPlayer = null;
                }
            } catch { }

            Environment.Exit(0);
        }

        private void ResetRate_Click(object sender, RoutedEventArgs e) => RateSlider.Value = 0;
        private void ResetPitch_Click(object sender, RoutedEventArgs e) => PitchSlider.Value = 0;

        // Debounce text changes and trigger word count update
        private void EditorWebView_WebMessageReceived(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            try {
                string message = args.TryGetWebMessageAsString();
                if (message == "text_changed") {
                    _wordCountTimer?.Stop();
                    _wordCountTimer?.Start();
                } else if (message == "invalid_file") {
                    ShowInvalidFilePopup();
                }
            } catch { }
        }

        private async void ShowInvalidFilePopup()
        {
            var dialog = new ContentDialog
            {
                Title = "Invalid File",
                Content = "Only text (.txt) files are supported.",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        // Count words and characters on a background thread
        private async void UpdateWordCountAsync()
        {
            if (EditorWebView == null || WordCharCountTextBlock == null) return;
            string text = await GetRawTextAsync();
            _wordCountCts?.Cancel();
            _wordCountCts = new System.Threading.CancellationTokenSource();
            var token = _wordCountCts.Token;

            try {
                var counts = await Task.Run(() => {
                    int charCount = text.Length;
                    int wordCount = 0;
                    bool inWord = false;
                    for (int i = 0; i < text.Length; i++) {
                        if (token.IsCancellationRequested) return (wordCount: -1, charCount: -1);
                        if (char.IsWhiteSpace(text[i])) {
                            inWord = false;
                        } else if (!inWord) {
                            wordCount++;
                            inWord = true;
                        }
                    }
                    return (wordCount: wordCount, charCount: charCount);
                }, token);
                
                if (counts.wordCount != -1) {
                    WordCharCountTextBlock.Text = string.Format(LanguageManager.Instance["WordCharCount"], counts.wordCount, counts.charCount);
                }
            } catch (OperationCanceledException) { }
        }

        private async void ClearTextButton_Click(object sender, RoutedEventArgs e)
        {
            if (EditorWebView != null) {
                _wordCountCts?.Cancel();
                await SetRawTextAsync("");
                WordCharCountTextBlock.Text = string.Format(LanguageManager.Instance["WordCharCount"], 0, 0);
            }
        }

        // Confirm and cancel active export
        private async void CancelExportButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Cancel Export?",
                Content = "Are you sure you want to cancel the active export? This will securely stop the generation process and clean up temporary files.",
                PrimaryButtonText = "Yes, Cancel",
                CloseButtonText = "No, Continue",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot
            };

            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                _isExportCancelled = true;
                if (_currentExportProcess != null && !_currentExportProcess.HasExited)
                {
                    try { _currentExportProcess.Kill(true); } catch { }
                }
                CleanTempFiles();
                if (ProgressOverlay.Visibility == Visibility.Visible) {
                    FailureReason.Text = "Export was safely cancelled by the user.";
                    ShowOverlayState("Failure");
                }
            }
        }

        // Delete all temporary audio and config files
        private void CleanTempFiles()
        {
            try {
                StopPlayback();
                string[] filesToDelete = {
                    "narraz_preview.mp3",
                    "temp_export.mp3",
                    "export_config.json",
                    "export_input.txt",
                    "concat.txt"
                };

                foreach (var file in filesToDelete)
                {
                    string path = Path.Combine(_appDataPath, file);
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                
                string tempChunksPath = Path.Combine(Path.GetTempPath(), "narraz_temp_chunks");
                if (Directory.Exists(tempChunksPath)) {
                    Directory.Delete(tempChunksPath, true);
                }
            }
            catch { }
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}

