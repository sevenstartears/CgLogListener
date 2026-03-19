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

namespace CgLogListener
{
    public partial class FormMain : Form
    {
        const int InitialRecentLogCount = 300;
        const int MaxDisplayedGroups = 4000;
        const int MaxRetainedLogs = 12000;

        readonly Label lblCategoryFilter = new Label();
        readonly Label lblHeaderVersion = new Label();
        readonly FlowLayoutPanel flowCategoryFilters = new FlowLayoutPanel();
        readonly Dictionary<LogCategory, Button> categoryFilters = new Dictionary<LogCategory, Button>();
        readonly HashSet<LogCategory> enabledCategories = new HashSet<LogCategory>();
        readonly List<DisplayedLogGroup> displayedGroups = new List<DisplayedLogGroup>();
        readonly List<DisplayedLogGroup> visibleGroups = new List<DisplayedLogGroup>();
        readonly List<LogLine> logHistory = new List<LogLine>();
        readonly Dictionary<string, string> translationCache = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Dictionary<string, Task<string>> translationTasks = new Dictionary<string, Task<string>>(StringComparer.Ordinal);
        readonly Dictionary<TranslationProvider, ITranslationService> translationServices = new Dictionary<TranslationProvider, ITranslationService>();
        readonly DiscordWebhookNotifier discordNotifier = new DiscordWebhookNotifier();
        readonly FoodCooldownTracker foodCooldownTracker = new FoodCooldownTracker();
        readonly Button btnSimpleView = new Button();
        readonly Button btnFoodTimer = new Button();
        readonly MediaPlayer mp = new MediaPlayer();

        Settings settings;
        CgLogHandler watcher;
        FormFoodTimer foodTimerForm;
        FormSimpleView simpleViewForm;
        bool isShuttingDown;

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
            StyleButton(btnSimpleView, false);
            StyleButton(btnFoodTimer, false);
            lblHeaderVersion.AutoSize = true;
            lblHeaderVersion.ForeColor = Color.FromArgb(220, 238, 255);
            lblHeaderVersion.Font = new Font("Yu Gothic UI", 9F, FontStyle.Regular);
            lblHeaderVersion.BackColor = Color.Transparent;
            panelHeader.Controls.Add(lblHeaderVersion);
            panelHeader.Resize += PanelHeader_Resize;
            InitializeCategoryFilterUi();
            ApplyLocalizedText();
            flowLogs.TranslateRequested += FlowLogs_TranslateRequested;
            btnSimpleView.Click += BtnSimpleView_Click;
            btnFoodTimer.Click += BtnFoodTimer_Click;
        }

        void FrmMain_Load(object sender, EventArgs e)
        {
            settings = Settings.GetInstance();

            ApplySettingsSummary();

            if (HasValidLogPath())
            {
                BindWatcher(loadRecentLogs: true);
            }
            else
            {
                ClearDisplayedLogs(clearHistory: true, resetFoodTimers: true);
                UpdateEmptyState();
            }
        }

        bool HasValidLogPath()
        {
            return CgLogHandler.ValidationPath(settings?.CgLogPath);
        }

        void InitializeCategoryFilterUi()
        {
            panelToolbar.Height = 236;
            flowLogs.Padding = new Padding(0);
            btnFoodTimer.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnFoodTimer.Location = new Point(900, 65);
            btnFoodTimer.Name = "btnFoodTimer";
            btnFoodTimer.Size = new Size(160, 34);
            btnFoodTimer.TabIndex = 8;
            btnFoodTimer.Text = "お食事タイマー";
            btnFoodTimer.UseVisualStyleBackColor = true;
            panelToolbar.Controls.Add(btnFoodTimer);
            btnSimpleView.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSimpleView.Location = new Point(900, 106);
            btnSimpleView.Name = "btnSimpleView";
            btnSimpleView.Size = new Size(160, 34);
            btnSimpleView.TabIndex = 9;
            btnSimpleView.Text = "シンプルビュー";
            btnSimpleView.UseVisualStyleBackColor = true;
            panelToolbar.Controls.Add(btnSimpleView);
            btnClearLogs.Location = new Point(900, 147);

            lblCategoryFilter.AutoSize = true;
            lblCategoryFilter.ForeColor = Color.FromArgb(91, 102, 114);
            lblCategoryFilter.Location = new Point(28, 162);

            flowCategoryFilters.AutoSize = false;
            flowCategoryFilters.AutoScroll = false;
            flowCategoryFilters.WrapContents = false;
            flowCategoryFilters.Location = new Point(102, 147);
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
            Text = "BlueCG ログビューア";
            notifyIcon.BalloonTipTitle = "BlueCG ログビューア";
            notifyIcon.Text = "BlueCG ログビューア";

            lblHeaderTitle.Text = "BlueCG ログビューア";
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

        void PanelHeader_Resize(object sender, EventArgs e)
        {
            LayoutHeaderVersion();
        }

        void LayoutHeaderVersion()
        {
            lblHeaderVersion.Location = new Point(
                Math.Max(24, panelHeader.ClientSize.Width - lblHeaderVersion.Width - 28),
                Math.Max(18, panelHeader.ClientSize.Height - lblHeaderVersion.Height - 18));
            lblHeaderVersion.BringToFront();
        }

        string GetDisplayVersionText()
        {
            Version version;
            if (Version.TryParse(Application.ProductVersion, out version))
            {
                return $"ver {version.Major}.{version.Minor}.{version.Build}";
            }

            return "ver 0.1.1";
        }

        void BindWatcher(bool loadRecentLogs)
        {
            if (!HasValidLogPath())
            {
                watcher?.Dispose();
                watcher = null;
                ApplySettingsSummary();
                UpdateEmptyState();
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
                UpdateEmptyState();
            }
        }

        void LoadRecentLogs()
        {
            var recentLogs = watcher.ReadRecentLogs(InitialRecentLogCount);

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

            var group = FindGroupToMerge(log);
            bool shouldRefreshVisible = false;
            bool shouldScrollToLatest = false;

            if (group != null)
            {
                group.Merge(log);

                if (refreshUi && IsCategoryVisible(group.Category))
                {
                    flowLogs.RefreshEntry(group);
                    RefreshSimpleViewEntry(group);
                    shouldScrollToLatest = true;
                }
            }
            else
            {
                var newGroup = new DisplayedLogGroup(log);
                displayedGroups.Add(newGroup);

                bool trimmedVisible = TrimDisplayedLogs();
                if (refreshUi)
                {
                    if (trimmedVisible)
                    {
                        shouldRefreshVisible = true;
                    }
                    else if (IsCategoryVisible(newGroup.Category))
                    {
                        visibleGroups.Add(newGroup);
                        flowLogs.AppendEntry(newGroup);
                        if (simpleViewForm != null && !simpleViewForm.IsDisposed)
                        {
                            simpleViewForm.AppendEntry(newGroup, scrollToLatest: true);
                        }
                        shouldScrollToLatest = true;
                    }
                }
            }

            if (allowNotification)
            {
                RunNotificationSubfeatures(log);
            }

            if (!refreshUi)
            {
                return;
            }

            if (shouldRefreshVisible)
            {
                RefreshVisibleLogs(scrollToLatest: IsCategoryVisible(log.Category));
            }
            else
            {
                if (shouldScrollToLatest)
                {
                    ScrollToLatestVisibleLog();
                    if (simpleViewForm != null && !simpleViewForm.IsDisposed)
                    {
                        simpleViewForm.ScrollToEnd();
                    }
                }

                UpdateEmptyState();
            }
        }

        DisplayedLogGroup FindGroupToMerge(LogLine log)
        {
            var window = TimeSpan.FromSeconds(settings.DeduplicationSeconds);

            for (int i = displayedGroups.Count - 1; i >= 0; i--)
            {
                var group = displayedGroups[i];
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
            visibleGroups.AddRange(displayedGroups.Where(group => IsCategoryVisible(group.Category)));
            flowLogs.SetEntries(visibleGroups);
            SyncSimpleView(scrollToLatest);

            if (scrollToLatest)
            {
                ScrollToLatestVisibleLog();
            }

            UpdateEmptyState();
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

        void BtnOpenSettings_Click(object sender, EventArgs e)
        {
            var oldPath = settings.CgLogPath;
            var oldDedup = settings.DeduplicationSeconds;
            bool oldPathValid = HasValidLogPath();

            using (var dialog = new FormSettings(settings))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
            }

            ApplySettingsSummary();

            bool newPathValid = HasValidLogPath();
            if (!newPathValid)
            {
                watcher?.Dispose();
                watcher = null;
                ClearDisplayedLogs(clearHistory: true, resetFoodTimers: true);
                UpdateEmptyState();
                return;
            }

            if (!oldPathValid || !string.Equals(oldPath, settings.CgLogPath, StringComparison.OrdinalIgnoreCase))
            {
                BindWatcher(loadRecentLogs: true);
            }
            else if (oldDedup != settings.DeduplicationSeconds)
            {
                RebuildDisplayedLogs();
            }
        }

        void BtnClearLogs_Click(object sender, EventArgs e)
        {
            SaveVisibleLogs();
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

            UpdateEmptyState();
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
            bool removedVisible = false;

            while (displayedGroups.Count > MaxDisplayedGroups)
            {
                var group = displayedGroups[0];
                displayedGroups.RemoveAt(0);

                if (visibleGroups.Remove(group))
                {
                    removedVisible = true;
                }
            }

            return removedVisible;
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
                dialog.FileName = $"BlueCG_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

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
    }
}
