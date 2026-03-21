using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using Color = System.Drawing.Color;

namespace CgLogViewer
{
    public partial class FormMain : Form
    {
        const int MaxDisplayedGroups = 4000;
        const int MaxRetainedLogs = 12000;

        readonly Label lblCategoryFilter = new Label();
        readonly Label lblHeaderVersion = new Label();
        readonly Label lblModeLabel = new Label();
        readonly FlowLayoutPanel flowCategoryFilters = new FlowLayoutPanel();
        readonly Dictionary<LogCategory, Button> categoryFilters = new Dictionary<LogCategory, Button>();
        readonly HashSet<LogCategory> enabledCategories = new HashSet<LogCategory>();
        readonly List<DisplayedLogGroup> displayedGroups = new List<DisplayedLogGroup>();
        readonly List<DisplayedLogGroup> browseGroups = new List<DisplayedLogGroup>();
        readonly List<DisplayedLogGroup> visibleGroups = new List<DisplayedLogGroup>();
        readonly List<LogLine> logHistory = new List<LogLine>();
        readonly Dictionary<string, string> translationCache = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Dictionary<string, Task<string>> translationTasks = new Dictionary<string, Task<string>>(StringComparer.Ordinal);
        readonly Dictionary<TranslationProvider, ITranslationService> translationServices = new Dictionary<TranslationProvider, ITranslationService>();
        readonly DiscordWebhookNotifier discordNotifier = new DiscordWebhookNotifier();
        readonly FoodCooldownTracker foodCooldownTracker = new FoodCooldownTracker();
        readonly Button btnRealtimeMode = new Button();
        readonly Button btnBrowseMode = new Button();
        readonly Button btnBrowseFile = new Button();
        readonly Button btnSimpleView = new Button();
        readonly Button btnFoodTimer = new Button();
        readonly MediaPlayer mp = new MediaPlayer();

        Settings settings;
        CgLogHandler watcher;
        FormFoodTimer foodTimerForm;
        FormSimpleView simpleViewForm;
        bool isShuttingDown;
        MainLogDisplayMode currentDisplayMode = MainLogDisplayMode.Realtime;
        string selectedBrowseFilePath;

        public FormMain()
        {
            InitializeComponent();

            ImeMode = ImeMode.OnHalf;
            Icon = Resource.icon;
            notifyIcon.Icon = Resource.icon;
            translationServices[TranslationProvider.DeepL] = new DeepLTranslationService();
            translationServices[TranslationProvider.Google] = new GoogleTranslationService();
            translationServices[TranslationProvider.OpenAI] = new OpenAITranslationService();

            StyleButton(btnOpenSettings, true);
            StyleButton(btnClearLogs, false);
            StyleButton(btnRealtimeMode, false);
            StyleButton(btnBrowseMode, false);
            StyleButton(btnBrowseFile, false);
            StyleButton(btnSimpleView, false);
            StyleButton(btnFoodTimer, false);
            panelHeader.BackColor = Color.FromArgb(18, 93, 156);
            panelHeader.Height = 64;
            lblHeaderTitle.Visible = false;
            lblHeaderSubtitle.Visible = false;
            panelToolbar.Controls.Remove(btnOpenSettings);
            panelToolbar.Controls.Remove(btnClearLogs);
            panelHeader.Controls.Add(btnOpenSettings);
            panelHeader.Controls.Add(btnClearLogs);
            btnOpenSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOpenSettings.Size = new Size(164, 34);
            btnClearLogs.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnClearLogs.Size = new Size(118, 34);
            StyleHeaderActionButton(btnOpenSettings);
            StyleHeaderActionButton(btnClearLogs);
            lblHeaderVersion.AutoSize = true;
            lblHeaderVersion.ForeColor = Color.FromArgb(220, 238, 255);
            lblHeaderVersion.Font = new Font("Yu Gothic UI", 9F, FontStyle.Regular);
            lblHeaderVersion.BackColor = Color.Transparent;
            panelHeader.Controls.Add(lblHeaderVersion);
            panelHeader.Resize += PanelHeader_Resize;
            panelToolbar.Resize += PanelToolbar_Resize;
            InitializeCategoryFilterUi();
            ApplyUiText();
            flowLogs.TranslateRequested += FlowLogs_TranslateRequested;
            btnRealtimeMode.Click += BtnRealtimeMode_Click;
            btnBrowseMode.Click += BtnBrowseMode_Click;
            btnBrowseFile.Click += BtnBrowseFile_Click;
            btnSimpleView.Click += BtnSimpleView_Click;
            btnFoodTimer.Click += BtnFoodTimer_Click;
        }

        void FrmMain_Load(object sender, EventArgs e)
        {
            settings = Settings.GetInstance();

            RefreshSettingsSummary();

            if (HasValidLogPath())
            {
                BindWatcher(loadRecentLogs: true);
            }
            else
            {
                ClearDisplayedLogs(clearHistory: true, resetFoodTimers: true);
                RefreshEmptyState();
            }

            ApplyDisplayModeVisualState();
        }

        bool HasValidLogPath()
        {
            return CgLogHandler.ValidationPath(settings?.CgLogPath);
        }

        void InitializeCategoryFilterUi()
        {
            panelToolbar.Height = 172;
            flowLogs.Padding = new Padding(0);
            lblModeLabel.AutoSize = true;
            lblModeLabel.ForeColor = Color.FromArgb(91, 102, 114);
            lblModeLabel.Location = new Point(24, 69);
            panelToolbar.Controls.Add(lblModeLabel);

            btnRealtimeMode.Location = new Point(128, 58);
            btnRealtimeMode.Name = "btnRealtimeMode";
            btnRealtimeMode.Size = new Size(154, 34);
            btnRealtimeMode.TabIndex = 8;
            btnRealtimeMode.UseVisualStyleBackColor = true;
            panelToolbar.Controls.Add(btnRealtimeMode);

            btnBrowseMode.Location = new Point(290, 58);
            btnBrowseMode.Name = "btnBrowseMode";
            btnBrowseMode.Size = new Size(110, 34);
            btnBrowseMode.TabIndex = 9;
            btnBrowseMode.UseVisualStyleBackColor = true;
            panelToolbar.Controls.Add(btnBrowseMode);

            lblLogPath.Visible = false;
            txtCgLogPath.Location = new Point(408, 64);
            txtCgLogPath.Size = new Size(320, 23);
            txtCgLogPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtCgLogPath.BackColor = Color.White;
            txtCgLogPath.ReadOnly = true;
            panelToolbar.Controls.Add(txtCgLogPath);

            btnBrowseFile.Size = new Size(104, 34);
            btnBrowseFile.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            panelToolbar.Controls.Add(btnBrowseFile);
            btnFoodTimer.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnFoodTimer.Location = new Point(900, 24);
            btnFoodTimer.Name = "btnFoodTimer";
            btnFoodTimer.Size = new Size(160, 34);
            btnFoodTimer.TabIndex = 10;
            btnFoodTimer.Text = "お食事タイマー";
            btnFoodTimer.UseVisualStyleBackColor = true;
            panelToolbar.Controls.Add(btnFoodTimer);
            btnSimpleView.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSimpleView.Location = new Point(900, 65);
            btnSimpleView.Name = "btnSimpleView";
            btnSimpleView.Size = new Size(160, 34);
            btnSimpleView.TabIndex = 11;
            btnSimpleView.Text = "シンプルビュー";
            btnSimpleView.UseVisualStyleBackColor = true;
            panelToolbar.Controls.Add(btnSimpleView);
            btnOpenSettings.Location = new Point(900, 106);
            lblCategoryFilter.AutoSize = true;
            lblCategoryFilter.ForeColor = Color.FromArgb(91, 102, 114);
            lblCategoryFilter.Location = new Point(28, 121);

            flowCategoryFilters.AutoSize = false;
            flowCategoryFilters.AutoScroll = false;
            flowCategoryFilters.WrapContents = false;
            flowCategoryFilters.Location = new Point(102, 106);
            flowCategoryFilters.Size = new Size(panelToolbar.ClientSize.Width - 126, 48);
            flowCategoryFilters.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            flowCategoryFilters.Margin = new Padding(0);

            foreach (var category in new[] { LogCategory.Normal, LogCategory.Party, LogCategory.Guild, LogCategory.System, LogCategory.Other })
            {
                enabledCategories.Add(category);
                var button = CreateCategoryFilterButton(category);
                categoryFilters[category] = button;
                flowCategoryFilters.Controls.Add(button);
            }

            panelToolbar.Controls.Add(lblCategoryFilter);
            panelToolbar.Controls.Add(flowCategoryFilters);
            RefreshCategoryFilterStyles();
            LayoutToolbarButtons();
        }

        Button CreateCategoryFilterButton(LogCategory category)
        {
            var button = new Button
            {
                AutoSize = false,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Yu Gothic UI", 9F, FontStyle.Bold),
                Margin = new Padding(0, 0, 8, 0),
                Size = new Size(104, 40),
                Padding = new Padding(6, 1, 6, 1),
                Text = LogCategoryPalette.GetLabel(category),
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = category,
                UseVisualStyleBackColor = false,
            };
            button.FlatAppearance.BorderSize = 1;
            button.Click += CategoryFilterButton_Click;
            return button;
        }

        void CategoryFilterButton_Click(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var category = (LogCategory)button.Tag;

            if (!enabledCategories.Add(category))
            {
                enabledCategories.Remove(category);
            }

            RefreshCategoryFilterStyles();
            RefreshVisibleLogs(scrollToLatest: true);
        }

        void RefreshCategoryFilterStyles()
        {
            foreach (var pair in categoryFilters)
            {
                var button = pair.Value;
                var accent = LogCategoryPalette.GetAccentColor(pair.Key);
                bool enabled = enabledCategories.Contains(pair.Key);
                button.FlatAppearance.BorderColor = accent;
                button.BackColor = enabled ? LogCategoryPalette.GetSurfaceColor(pair.Key) : Color.White;
                button.ForeColor = enabled ? accent : Color.FromArgb(91, 102, 114);
            }
        }

        void ApplyLocalizedText()
        {
            Text = "CgLogViewer";
            notifyIcon.BalloonTipTitle = "CgLogViewer";
            notifyIcon.Text = "CgLogViewer";

            lblHeaderTitle.Text = "CgLogViewer";
            lblHeaderSubtitle.Text = "リアルタイムログ表示をメインにしつつ、通知と外部送信をまとめて扱えます。";

            lblStatusLabel.Text = "状態:";
            lblDedupLabel.Text = "まとめ秒数:";
            lblLogPath.Text = "ゲームフォルダ:";
            btnOpenSettings.Text = "表示と通知の設定";
            btnFoodTimer.Text = "お食事タイマー";
            btnClearLogs.Text = "ログ保存";
            lblHeaderVersion.Text = GetDisplayVersionText();
            LayoutHeaderVersion();

            toolOpen.Text = "表示";
            toolMinsize.Text = "最小化";
            toolExit.Text = "終了";
            lblCategoryFilter.Text = "カテゴリ:";
        }

        void ApplyUiText()
        {
            Text = "CgLogViewer";
            notifyIcon.BalloonTipTitle = "CgLogViewer";
            notifyIcon.Text = "CgLogViewer";

            lblHeaderTitle.Text = "CgLogViewer";
            lblHeaderSubtitle.Text = "リアルタイム監視とログ閲覧を切り替えながら、通知と翻訳もまとめて扱えます。";

            lblStatusLabel.Text = "状態:";
            lblDedupLabel.Text = "まとめ秒数:";
            lblModeLabel.Text = "表示モード:";
            lblLogPath.Text = "閲覧ファイル:";
            btnRealtimeMode.Text = "リアルタイム監視";
            btnBrowseMode.Text = "ログ閲覧";
            btnBrowseFile.Text = "ファイル選択";
            btnOpenSettings.Text = "表示と通知の設定";
            btnFoodTimer.Text = "お食事タイマー";
            btnSimpleView.Text = "シンプルビュー";
            btnClearLogs.Text = "ログ保存";
            lblHeaderVersion.Text = GetDisplayVersionText();
            LayoutHeaderVersion();

            toolOpen.Text = "表示";
            toolMinsize.Text = "最小化";
            toolExit.Text = "終了";
            lblCategoryFilter.Text = "カテゴリ:";
        }

        void PanelToolbar_Resize(object sender, EventArgs e)
        {
            LayoutToolbarButtons();
        }

        void LayoutToolbarButtons()
        {
            int rightEdge = panelToolbar.ClientSize.Width - 24;
            btnFoodTimer.Left = rightEdge - btnFoodTimer.Width;
            btnSimpleView.Left = rightEdge - btnSimpleView.Width;

            int browseRight = btnFoodTimer.Left - 12;
            btnBrowseFile.Left = browseRight - btnBrowseFile.Width;
            btnBrowseFile.Top = 58;

            txtCgLogPath.Left = btnBrowseMode.Right + 10;
            txtCgLogPath.Top = 64;
            int browseWidth = Math.Max(180, btnBrowseFile.Left - txtCgLogPath.Left - 8);
            txtCgLogPath.Width = browseWidth;
            flowCategoryFilters.Width = Math.Max(320, rightEdge - flowCategoryFilters.Left);
        }

        void PanelHeader_Resize(object sender, EventArgs e)
        {
            LayoutHeaderVersion();
        }

        void LayoutHeaderVersion()
        {
            btnClearLogs.Location = new Point(24, 14);
            btnOpenSettings.Location = new Point(
                btnClearLogs.Right + 12,
                14);
            lblHeaderVersion.Location = new Point(
                Math.Max(btnOpenSettings.Right + 20, panelHeader.ClientSize.Width - lblHeaderVersion.Width - 24),
                Math.Max(18, panelHeader.ClientSize.Height - lblHeaderVersion.Height - 18));
            btnClearLogs.BringToFront();
            btnOpenSettings.BringToFront();
            lblHeaderVersion.BringToFront();
        }

        string GetDisplayVersionText()
        {
            Version version;
            if (Version.TryParse(Application.ProductVersion, out version))
            {
                return $"ver {version.Major}.{version.Minor}.{version.Build}";
            }

            return "ver 0.1.2";
        }

        void BindWatcher(bool loadRecentLogs)
        {
            if (!HasValidLogPath())
            {
                watcher?.Dispose();
                watcher = null;
                RefreshSettingsSummary();
                RefreshEmptyState();
                return;
            }

            watcher?.Dispose();
            watcher = new CgLogHandler(settings.CgLogPath);
            watcher.OnNewLog += Watcher_OnNewLog;

            UpdateStatus("リアルタイム監視中");
            txtCgLogPath.Text = settings.CgLogPath;

            if (loadRecentLogs)
            {
                LoadRecentLogs();
            }
            else
            {
                RefreshEmptyState();
            }
        }

        void LoadRecentLogs()
        {
            int targetGroupCount = Math.Max(20, settings.RealtimeDisplayCount);
            int rawReadCount = Math.Max(targetGroupCount * 4, 160);
            var recentLogs = watcher.ReadRecentLogs(rawReadCount);

            ClearDisplayedLogs(clearHistory: true, resetFoodTimers: true);
            foreach (var log in recentLogs)
            {
                AppendLog(log, allowNotification: false, storeHistory: true, refreshUi: false);
            }

            RefreshVisibleLogs(scrollToLatest: true);
        }

        void Watcher_OnNewLog(object sender, LogLineEventArgs e)
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => AppendLog(e.LogLine, allowNotification: true, storeHistory: true, refreshUi: true)));
                return;
            }

            AppendLog(e.LogLine, allowNotification: true, storeHistory: true, refreshUi: true);
        }

        void AppendLog(LogLine log, bool allowNotification, bool storeHistory, bool refreshUi)
        {
            if (storeHistory)
            {
                logHistory.Add(log);
                if (logHistory.Count > MaxRetainedLogs)
                {
                    logHistory.RemoveRange(0, logHistory.Count - MaxRetainedLogs);
                }
            }

            foodCooldownTracker.TryRegister(log);

            var group = FindGroupToMerge(displayedGroups, log);

            if (group != null)
            {
                group.Merge(log);
            }
            else
            {
                displayedGroups.Add(new DisplayedLogGroup(log));
                TrimDisplayedLogs();
            }

            if (allowNotification)
            {
                RunNotificationSubfeatures(log);
            }

            if (!refreshUi || currentDisplayMode != MainLogDisplayMode.Realtime)
            {
                return;
            }

            RefreshVisibleLogs(scrollToLatest: true);
        }

        DisplayedLogGroup FindGroupToMerge(IList<DisplayedLogGroup> groups, LogLine log)
        {
            var window = TimeSpan.FromSeconds(settings.DeduplicationSeconds);

            for (int i = groups.Count - 1; i >= 0; i--)
            {
                var group = groups[i];
                if (group.Category != log.Category || group.Message != log.Message)
                {
                    continue;
                }

                if (Math.Abs((log.Timestamp - group.LastTimestamp).TotalSeconds) <= window.TotalSeconds)
                {
                    return group;
                }
            }

            return null;
        }

        bool IsCategoryVisible(LogCategory category)
        {
            return enabledCategories.Contains(category);
        }

        void RefreshVisibleLogs(bool scrollToLatest = false)
        {
            visibleGroups.Clear();
            IEnumerable<DisplayedLogGroup> sourceGroups = currentDisplayMode == MainLogDisplayMode.Browse
                ? browseGroups
                : displayedGroups;

            var filteredGroups = sourceGroups
                .Where(group => IsCategoryVisible(group.Category))
                .ToList();

            if (currentDisplayMode == MainLogDisplayMode.Realtime)
            {
                int takeCount = Math.Max(20, settings.RealtimeDisplayCount);
                if (filteredGroups.Count > takeCount)
                {
                    filteredGroups = filteredGroups.Skip(filteredGroups.Count - takeCount).ToList();
                }
            }

            visibleGroups.AddRange(filteredGroups);
            flowLogs.SetEntries(visibleGroups);
            SyncSimpleView(scrollToLatest);

            if (scrollToLatest)
            {
                ScrollToLatestVisibleLog();
            }

            RefreshEmptyState();
        }

        void SyncSimpleView(bool scrollToLatest)
        {
            if (simpleViewForm == null || simpleViewForm.IsDisposed)
            {
                return;
            }

            simpleViewForm.TranslationConfigured = HasTranslationCredentials();
            simpleViewForm.SetEntries(visibleGroups, scrollToLatest);
        }

        void RefreshSimpleViewEntry(DisplayedLogGroup entry)
        {
            if (simpleViewForm == null || simpleViewForm.IsDisposed)
            {
                return;
            }

            simpleViewForm.RefreshEntry(entry);
        }

        async void FlowLogs_TranslateRequested(object sender, BufferedFlowLayoutPanel.TranslateRequestedEventArgs e)
        {
            var entry = e.Entry;
            if (entry == null)
            {
                return;
            }

            if (entry.CanToggleTranslation)
            {
                entry.ToggleTranslation();
                flowLogs.RefreshEntry(entry);
                RefreshSimpleViewEntry(entry);
                return;
            }

            if (entry.TranslationState == TranslationState.InProgress)
            {
                return;
            }

            if (!HasTranslationCredentials())
            {
                MessageBox.Show(this, $"翻訳を使うには設定画面で {GetTranslationProviderLabel()} の API キーを入力してください。", "翻訳未設定", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string translatedMessage;
            string cacheKey = BuildTranslationCacheKey(entry.Message);
            if (translationCache.TryGetValue(cacheKey, out translatedMessage))
            {
                entry.SetTranslation(translatedMessage);
                flowLogs.RefreshEntry(entry);
                RefreshSimpleViewEntry(entry);
                return;
            }

            entry.StartTranslation();
            flowLogs.RefreshEntry(entry);
            RefreshSimpleViewEntry(entry);

            try
            {
                translatedMessage = await GetOrCreateTranslationTask(entry.Message).ConfigureAwait(true);
                entry.SetTranslation(translatedMessage);
            }
            catch (Exception ex)
            {
                entry.MarkTranslationFailed();
                MessageBox.Show(this, $"{GetTranslationProviderLabel()} での翻訳に失敗しました。\r\n{ex.Message}", "翻訳エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            flowLogs.RefreshEntry(entry);
            RefreshSimpleViewEntry(entry);
        }

        Task<string> GetOrCreateTranslationTask(string message)
        {
            string cacheKey = BuildTranslationCacheKey(message);
            Task<string> task;
            if (translationTasks.TryGetValue(cacheKey, out task))
            {
                return task;
            }

            task = TranslateAndCacheAsync(message);
            translationTasks[cacheKey] = task;
            return task;
        }

        async Task<string> TranslateAndCacheAsync(string message)
        {
            string cacheKey = BuildTranslationCacheKey(message);
            try
            {
                string translated = await GetSelectedTranslationService()
                    .TranslateToJapaneseAsync(GetSelectedTranslationApiKey(), message, CancellationToken.None)
                    .ConfigureAwait(true);
                translationCache[cacheKey] = translated;
                return translated;
            }
            finally
            {
                translationTasks.Remove(cacheKey);
            }
        }

        bool HasTranslationCredentials()
        {
            return !string.IsNullOrWhiteSpace(GetSelectedTranslationApiKey());
        }

        string GetSelectedTranslationApiKey()
        {
            switch (settings.TranslationProvider)
            {
                case TranslationProvider.Google:
                    return settings.GoogleApiKey;
                case TranslationProvider.OpenAI:
                    return settings.OpenAIApiKey;
                default:
                    return settings.DeepLApiKey;
            }
        }

        ITranslationService GetSelectedTranslationService()
        {
            return translationServices[settings.TranslationProvider];
        }

        string GetTranslationProviderLabel()
        {
            switch (settings.TranslationProvider)
            {
                case TranslationProvider.Google:
                    return "Google Cloud Translation";
                case TranslationProvider.OpenAI:
                    return "OpenAI API";
                default:
                    return "DeepL API Free";
            }
        }

        string BuildTranslationCacheKey(string message)
        {
            return $"{settings.TranslationProvider}:{message}";
        }

        void RunNotificationSubfeatures(LogLine log)
        {
            string displayLine = log.DisplayLine;
            if (!ShouldNotify(log))
            {
                return;
            }

            notifyIcon.ShowBalloonTip(3000, notifyIcon.BalloonTipTitle, displayLine, ToolTipIcon.None);
            PlayNotificationSound();
            _ = SendDiscordNotificationAsync(displayLine);
        }

        bool ShouldNotify(LogLine log)
        {
            string displayLine = log.DisplayLine;
            foreach (var preset in NotificationPresets.All)
            {
                bool enabled;
                if (settings.StandardTips.TryGetValue(preset.Key, out enabled) &&
                    enabled &&
                    (!preset.RequiredCategory.HasValue || preset.RequiredCategory.Value == log.Category) &&
                    Regex.IsMatch(displayLine, preset.Pattern))
                {
                    return true;
                }
            }

            foreach (var customTip in settings.CustomizeTips)
            {
                var split = customTip.Split('|');
                if (!displayLine.Contains(split[0]))
                {
                    continue;
                }

                if (split.Length <= 1)
                {
                    return true;
                }

                var exclusions = split[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (!exclusions.Any(displayLine.Contains))
                {
                    return true;
                }
            }

            return false;
        }

        void PlayNotificationSound()
        {
            const string soundName = "sound.wav";
            if (!settings.PlaySound || !File.Exists(soundName))
            {
                return;
            }

            try
            {
                mp.Stop();
                mp.Open(new Uri(new FileInfo(soundName).FullName));
                mp.Volume = settings.SoundVol / 10d;
                mp.Play();
            }
            catch
            {
                // ignored
            }
        }

        async Task SendDiscordNotificationAsync(string displayLine)
        {
            if (!settings.DiscordNotificationEnabled || string.IsNullOrWhiteSpace(settings.DiscordWebhookUrl))
            {
                return;
            }

            try
            {
                await discordNotifier.SendAsync(settings.DiscordWebhookUrl, displayLine, CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }
        }

        void SwitchDisplayMode(MainLogDisplayMode mode)
        {
            currentDisplayMode = mode;
            ApplyDisplayModeVisualState();
            RefreshVisibleLogs(scrollToLatest: mode == MainLogDisplayMode.Realtime);
        }

        void ApplyDisplayModeUi()
        {
            bool browseMode = currentDisplayMode == MainLogDisplayMode.Browse;
            lblLogPath.Visible = false;
            txtCgLogPath.Visible = browseMode;
            btnBrowseFile.Visible = browseMode;

            if (browseMode)
            {
                txtCgLogPath.Text = string.IsNullOrWhiteSpace(selectedBrowseFilePath)
                    ? "まだログファイルは選択されていません"
                    : Path.GetFileName(selectedBrowseFilePath);
                UpdateStatus("ログ閲覧モード");
            }
            else
            {
                txtCgLogPath.Text = string.Empty;
                UpdateStatus(HasValidLogPath() ? "リアルタイム監視中" : "ゲームフォルダ未設定");
            }

            RefreshModeButtonStyles();
            LayoutToolbarButtons();
            RefreshEmptyState();
        }

        void RefreshModeButtonStyles()
        {
            ApplyModeButtonStyle(btnRealtimeMode, currentDisplayMode == MainLogDisplayMode.Realtime);
            ApplyModeButtonStyle(btnBrowseMode, currentDisplayMode == MainLogDisplayMode.Browse);
        }

        void ApplyModeButtonStyle(Button button, bool active)
        {
            if (active)
            {
                button.BackColor = Color.FromArgb(18, 93, 156);
                button.ForeColor = Color.White;
                button.FlatAppearance.BorderColor = Color.FromArgb(18, 93, 156);
            }
            else
            {
                button.BackColor = Color.FromArgb(248, 251, 255);
                button.ForeColor = Color.FromArgb(31, 55, 81);
                button.FlatAppearance.BorderColor = Color.FromArgb(194, 210, 226);
            }
        }

        void ApplyDisplayModeVisualState()
        {
            bool browseMode = currentDisplayMode == MainLogDisplayMode.Browse;
            lblLogPath.Visible = false;
            txtCgLogPath.Visible = browseMode;
            btnBrowseFile.Visible = browseMode;

            if (browseMode)
            {
                txtCgLogPath.Text = string.IsNullOrWhiteSpace(selectedBrowseFilePath)
                    ? "ログファイル未選択"
                    : Path.GetFileName(selectedBrowseFilePath);
                UpdateStatus("ログ閲覧モード");
            }
            else
            {
                txtCgLogPath.Text = string.Empty;
                UpdateStatus(HasValidLogPath() ? "リアルタイム監視中" : "ゲームフォルダ未設定");
            }

            RefreshModeButtonStyles();
            LayoutToolbarButtons();
            RefreshEmptyState();
        }

        void SelectBrowseFileAndLoad()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "閲覧するログファイルを選択";
                dialog.Filter = "ログファイル (*.txt)|*.txt|すべてのファイル (*.*)|*.*";
                dialog.DefaultExt = "txt";
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;
                dialog.Multiselect = false;

                if (HasValidLogPath())
                {
                    var initialDirectory = Path.Combine(settings.CgLogPath, "Log");
                    if (Directory.Exists(initialDirectory))
                    {
                        dialog.InitialDirectory = initialDirectory;
                    }
                }

                if (!string.IsNullOrWhiteSpace(selectedBrowseFilePath) && File.Exists(selectedBrowseFilePath))
                {
                    dialog.FileName = Path.GetFileName(selectedBrowseFilePath);
                    dialog.InitialDirectory = Path.GetDirectoryName(selectedBrowseFilePath);
                }

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                LoadBrowseFile(dialog.FileName);
                SwitchDisplayMode(MainLogDisplayMode.Browse);
            }
        }

        void LoadBrowseFile(string filePath)
        {
            selectedBrowseFilePath = filePath;
            browseGroups.Clear();

            foreach (var log in CgLogHandler.ReadLogFile(filePath))
            {
                var existing = FindGroupToMerge(browseGroups, log);
                if (existing != null)
                {
                    existing.Merge(log);
                }
                else
                {
                    browseGroups.Add(new DisplayedLogGroup(log));
                }
            }
        }

        void BtnOpenSettings_Click(object sender, EventArgs e)
        {
            var oldPath = settings.CgLogPath;
            var oldDedup = settings.DeduplicationSeconds;
            var oldRealtimeCount = settings.RealtimeDisplayCount;
            bool oldPathValid = HasValidLogPath();

            using (var dialog = new FormSettings(settings))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
            }

            RefreshSettingsSummary();

            bool newPathValid = HasValidLogPath();
            if (!newPathValid)
            {
                watcher?.Dispose();
                watcher = null;
                ClearDisplayedLogs(clearHistory: true, resetFoodTimers: true);
                RefreshEmptyState();
                return;
            }

            if (!oldPathValid || !string.Equals(oldPath, settings.CgLogPath, StringComparison.OrdinalIgnoreCase))
            {
                BindWatcher(loadRecentLogs: true);
            }
            else if (oldDedup != settings.DeduplicationSeconds || oldRealtimeCount != settings.RealtimeDisplayCount)
            {
                RebuildDisplayedLogs();
            }

            RefreshVisibleLogs(scrollToLatest: currentDisplayMode == MainLogDisplayMode.Realtime);
        }

        void BtnClearLogs_Click(object sender, EventArgs e)
        {
            SaveVisibleLogs();
        }

        void BtnRealtimeMode_Click(object sender, EventArgs e)
        {
            SwitchDisplayMode(MainLogDisplayMode.Realtime);
        }

        void BtnBrowseMode_Click(object sender, EventArgs e)
        {
            SwitchDisplayMode(MainLogDisplayMode.Browse);
        }

        void BtnBrowseFile_Click(object sender, EventArgs e)
        {
            SelectBrowseFileAndLoad();
        }

        void BtnSimpleView_Click(object sender, EventArgs e)
        {
            EnsureSimpleViewForm();
            Hide();
            simpleViewForm.Show();
            simpleViewForm.WindowState = FormWindowState.Normal;
            SyncSimpleView(scrollToLatest: true);
            simpleViewForm.ScrollToEndDeferred();
            simpleViewForm.Activate();
        }

        void BtnFoodTimer_Click(object sender, EventArgs e)
        {
            if (foodTimerForm == null || foodTimerForm.IsDisposed)
            {
                foodTimerForm = new FormFoodTimer(foodCooldownTracker);
            }

            if (!foodTimerForm.Visible)
            {
                foodTimerForm.Show(this);
            }

            foodTimerForm.WindowState = FormWindowState.Normal;
            foodTimerForm.Activate();
        }

        void EnsureSimpleViewForm()
        {
            if (simpleViewForm != null && !simpleViewForm.IsDisposed)
            {
                return;
            }

            simpleViewForm = new FormSimpleView();
            simpleViewForm.TranslateRequested += FlowLogs_TranslateRequested;
            simpleViewForm.ReturnRequested += SimpleViewForm_ReturnRequested;
            simpleViewForm.FoodTimerRequested += BtnFoodTimer_Click;
            simpleViewForm.FormClosed += SimpleViewForm_FormClosed;
            simpleViewForm.TranslationConfigured = HasTranslationCredentials();
        }

        void SimpleViewForm_ReturnRequested(object sender, EventArgs e)
        {
            ReturnToFullView();
        }

        void SimpleViewForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (isShuttingDown)
            {
                return;
            }

            ShowMainWindow();
        }

        void ReturnToFullView()
        {
            ShowMainWindow();
            if (simpleViewForm != null && !simpleViewForm.IsDisposed)
            {
                simpleViewForm.FormClosed -= SimpleViewForm_FormClosed;
                simpleViewForm.Close();
                simpleViewForm = null;
            }
        }

        void FlowLogs_SizeChanged(object sender, EventArgs e)
        {
            flowLogs.RefreshLayoutMetrics();
            if (simpleViewForm != null && !simpleViewForm.IsDisposed)
            {
                simpleViewForm.RefreshLayoutMetrics();
            }
        }

        void ClearDisplayedLogs(bool clearHistory, bool resetFoodTimers = false)
        {
            displayedGroups.Clear();
            browseGroups.Clear();
            visibleGroups.Clear();
            flowLogs.SetEntries(Array.Empty<DisplayedLogGroup>());
            SyncSimpleView(scrollToLatest: false);

            if (clearHistory)
            {
                logHistory.Clear();
            }

            if (resetFoodTimers)
            {
                foodCooldownTracker.Reset();
            }

            RefreshEmptyState();
        }

        void RebuildDisplayedLogs()
        {
            var snapshot = logHistory
                .OrderBy(log => log.Timestamp)
                .ToList();

            ClearDisplayedLogs(clearHistory: false);
            foreach (var log in snapshot)
            {
                AppendLog(log, allowNotification: false, storeHistory: false, refreshUi: false);
            }

            RefreshVisibleLogs(scrollToLatest: true);
        }

        bool TrimDisplayedLogs()
        {
            while (displayedGroups.Count > MaxDisplayedGroups)
            {
                displayedGroups.RemoveAt(0);
            }

            return false;
        }

        void RefreshEmptyState()
        {
            if (visibleGroups.Count > 0)
            {
                lblEmptyState.Visible = false;
                return;
            }

            lblEmptyState.Visible = true;
            lblEmptyState.BringToFront();

            if (currentDisplayMode == MainLogDisplayMode.Browse)
            {
                lblEmptyState.Text = string.IsNullOrWhiteSpace(selectedBrowseFilePath)
                    ? "ログ閲覧モードです。ファイル選択から閲覧するログを選んでください。"
                    : "選択中のログファイルに表示できる行がありません。";
                return;
            }

            if (!HasValidLogPath())
            {
                lblEmptyState.Text = "まずは設定から有効なゲームフォルダを選択してください。";
                return;
            }

            lblEmptyState.Text = displayedGroups.Count == 0
                ? "まだ表示できるログがありません。監視中のログが流れるとここに表示されます。"
                : "選択中のカテゴリに一致するログがありません。";
        }

        void UpdateEmptyState()
        {
            if (visibleGroups.Count > 0)
            {
                lblEmptyState.Visible = false;
                return;
            }

            lblEmptyState.Visible = true;
            lblEmptyState.BringToFront();

            if (!HasValidLogPath())
            {
                lblEmptyState.Text = "ログ表示を開始するには、右上の「表示と通知の設定」からゲームフォルダを指定してください。";
                return;
            }

            lblEmptyState.Text = displayedGroups.Count == 0
                ? "まだ表示できるログがありません。新しいログを受信するとここに流れます。"
                : "選択中のカテゴリに該当するログがありません。";
        }

        void RefreshSettingsSummary()
        {
            lblDedupValue.Text = $"{settings.DeduplicationSeconds} 秒";
            flowLogs.TranslationConfigured = HasTranslationCredentials();
            if (simpleViewForm != null && !simpleViewForm.IsDisposed)
            {
                simpleViewForm.TranslationConfigured = HasTranslationCredentials();
            }

            UpdateStatus(HasValidLogPath() ? "リアルタイム監視中" : "ゲームフォルダ未設定");
            ApplyDisplayModeVisualState();
        }

        void ApplySettingsSummary()
        {
            txtCgLogPath.Text = settings.CgLogPath;
            lblDedupValue.Text = $"{settings.DeduplicationSeconds} 秒";
            flowLogs.TranslationConfigured = HasTranslationCredentials();
            if (simpleViewForm != null && !simpleViewForm.IsDisposed)
            {
                simpleViewForm.TranslationConfigured = HasTranslationCredentials();
            }
            UpdateStatus(HasValidLogPath() ? "リアルタイム監視中" : "監視フォルダ未設定");
        }

        void UpdateStatus(string text)
        {
            lblStatusValue.Text = text;
        }

        void SaveVisibleLogs()
        {
            if (visibleGroups.Count == 0)
            {
                MessageBox.Show(this, "保存できる表示ログがありません。", "ログ保存", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "表示ログを保存";
                dialog.Filter = "テキスト ファイル (*.txt)|*.txt|すべてのファイル (*.*)|*.*";
                dialog.DefaultExt = "txt";
                dialog.AddExtension = true;
                dialog.FileName = $"CgLogViewer_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    File.WriteAllLines(dialog.FileName, BuildVisibleLogExportLines(), Encoding.Unicode);
                    MessageBox.Show(this, "表示中のログを保存しました。", "ログ保存", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"ログ保存に失敗しました。\r\n{ex.Message}", "ログ保存", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        IEnumerable<string> BuildVisibleLogExportLines()
        {
            foreach (var group in visibleGroups)
            {
                string line = $"{group.DisplayTimestamp:HH:mm:ss} [{LogCategoryPalette.GetLabel(group.Category)}] {group.VisibleMessage}";
                if (group.Count > 1)
                {
                    line += $" (x{group.Count})";
                }

                yield return line;
            }
        }

        void ScrollToLatestVisibleLog()
        {
            if (visibleGroups.Count > 0)
            {
                flowLogs.ScrollToEnd();
            }
        }

        void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowMainWindow();
        }

        void ToolOpen_Click(object sender, EventArgs e)
        {
            ShowMainWindow();
        }

        void ToolMinsize_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        void ToolExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        void ShowMainWindow()
        {
            Visible = true;
            WindowState = FormWindowState.Normal;
            Activate();
        }

        void FormMain_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Visible = false;
            }
        }

        void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            isShuttingDown = true;
            watcher?.Dispose();
            notifyIcon?.Dispose();
            if (foodTimerForm != null && !foodTimerForm.IsDisposed)
            {
                foodTimerForm.Close();
            }
            if (simpleViewForm != null && !simpleViewForm.IsDisposed)
            {
                simpleViewForm.Close();
            }
            foreach (var service in translationServices.Values.OfType<IDisposable>())
            {
                service.Dispose();
            }
            discordNotifier.Dispose();
        }

        void StyleButton(Button button, bool primary)
        {
            button.FlatStyle = FlatStyle.Flat;
            if (primary)
            {
                button.BackColor = Color.FromArgb(18, 93, 156);
                button.ForeColor = Color.White;
                button.FlatAppearance.BorderColor = Color.FromArgb(18, 93, 156);
            }
            else
            {
                button.BackColor = Color.FromArgb(248, 251, 255);
                button.ForeColor = Color.FromArgb(31, 55, 81);
                button.FlatAppearance.BorderColor = Color.FromArgb(194, 210, 226);
            }
        }

        void StyleHeaderActionButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = Color.White;
            button.ForeColor = Color.FromArgb(34, 59, 84);
            button.FlatAppearance.BorderColor = Color.FromArgb(194, 210, 226);
        }
    }
}
