using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CgLogViewer
{
    public sealed class FormSettings : Form
    {
        readonly Settings settings;
        readonly TextBox txtLogPath = new TextBox();
        readonly NumericUpDown numDedupSeconds = new NumericUpDown();
        readonly NumericUpDown numRealtimeDisplayCount = new NumericUpDown();
        readonly CheckBox chkPlaySound = new CheckBox();
        readonly TrackBar trackSoundVolume = new TrackBar();
        readonly Label lblSoundValue = new Label();
        readonly CheckBox chkDiscord = new CheckBox();
        readonly TextBox txtDiscordWebhookUrl = new TextBox();
        readonly ComboBox cmbTranslationProvider = new ComboBox();
        readonly Label lblTranslationKey = new Label();
        readonly TextBox txtDeepLApiKey = new TextBox();
        readonly TextBox txtGoogleApiKey = new TextBox();
        readonly TextBox txtOpenAIApiKey = new TextBox();
        readonly Label lblOpenAIReasoningEffort = new Label();
        readonly ComboBox cmbOpenAIReasoningEffort = new ComboBox();
        readonly Button btnUploadGlossary = new Button();
        readonly Label lblTranslationHint = new Label();
        readonly ListBox listCustomKeywords = new ListBox();
        readonly Dictionary<string, CheckBox> presetCheckBoxes = new Dictionary<string, CheckBox>();
        string selectedLogPath;

        public FormSettings(Settings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            selectedLogPath = settings.CgLogPath;

            InitializeComponent();
            LoadSettings();
        }

        void InitializeComponent()
        {
            SuspendLayout();

            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(244, 247, 251);
            ClientSize = new Size(840, 760);
            MinimumSize = new Size(840, 760);
            Font = new Font("Yu Gothic UI", 9F, FontStyle.Regular);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "表示と通知の設定";
            FormClosing += FormSettings_FormClosing;

            var panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                BackColor = Color.FromArgb(18, 93, 156),
                Padding = new Padding(24, 18, 24, 12),
            };
            panelHeader.Controls.Add(new Label
            {
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Yu Gothic UI Semibold", 18F, FontStyle.Bold),
                Text = "表示と通知の設定",
                Location = new Point(0, 0),
            });
            panelHeader.Controls.Add(new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 238, 255),
                Font = new Font("Yu Gothic UI", 9.5F, FontStyle.Regular),
                Text = "ログ表示、通知、翻訳の設定をここで変更できます。",
                Location = new Point(2, 42),
            });

            var panelTopActions = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.White,
                Padding = new Padding(24, 12, 24, 12),
            };
            panelTopActions.Controls.Add(new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(91, 102, 114),
                Location = new Point(24, 24),
                Text = "変更内容は保存ボタンで反映されます。",
            });

            var btnTopClose = CreateSecondaryButton("閉じる", new Size(128, 40));
            btnTopClose.DialogResult = DialogResult.Cancel;
            btnTopClose.Margin = new Padding(0, 0, 12, 0);

            var btnTopSave = CreatePrimaryButton("保存", new Size(148, 40));
            btnTopSave.Click += BtnSave_Click;

            var topActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.White,
                Margin = new Padding(0),
                Padding = new Padding(0),
            };
            topActions.Controls.Add(btnTopClose);
            topActions.Controls.Add(btnTopSave);
            panelTopActions.Controls.Add(topActions);

            var panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20, 18, 20, 12),
            };

            var flowSections = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Width = 780,
            };
            flowSections.Controls.Add(CreateGeneralSection());
            flowSections.Controls.Add(CreateNotificationSection());
            flowSections.Controls.Add(CreateCustomKeywordSection());
            flowSections.Controls.Add(CreateTranslationSection());
            flowSections.Controls.Add(CreateExternalNotificationSection());
            panelContent.Controls.Add(flowSections);

            var panelBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 82,
                BackColor = Color.White,
                Padding = new Padding(24, 18, 24, 18),
            };
            var btnCancel = CreateSecondaryButton("閉じる", new Size(128, 40));
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Margin = new Padding(0, 0, 12, 0);
            var btnSave = CreatePrimaryButton("設定を保存", new Size(148, 40));
            btnSave.Click += BtnSave_Click;
            var bottomActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.White,
                Margin = new Padding(0),
                Padding = new Padding(0),
            };
            bottomActions.Controls.Add(btnCancel);
            bottomActions.Controls.Add(btnSave);
            panelBottom.Controls.Add(bottomActions);

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            Controls.Add(panelContent);
            Controls.Add(panelBottom);
            Controls.Add(panelTopActions);
            Controls.Add(panelHeader);

            ResumeLayout(false);
        }

        Control CreateGeneralSection()
        {
            var section = CreateSectionPanel("ログ表示");

            var lblPath = CreateSectionLabel("ゲームフォルダ");
            lblPath.Location = new Point(20, 40);
            section.Controls.Add(lblPath);

            txtLogPath.Location = new Point(20, 66);
            txtLogPath.ReadOnly = true;
            txtLogPath.Width = 590;
            txtLogPath.BackColor = Color.White;
            section.Controls.Add(txtLogPath);

            var btnBrowse = CreateSecondaryButton("参照...", new Size(90, 32));
            btnBrowse.Location = new Point(626, 64);
            btnBrowse.Click += BtnBrowse_Click;
            section.Controls.Add(btnBrowse);

            var lblDedup = CreateSectionLabel("同一ログのまとめ秒数");
            lblDedup.Location = new Point(20, 112);
            section.Controls.Add(lblDedup);

            numDedupSeconds.Location = new Point(20, 138);
            numDedupSeconds.Minimum = 0;
            numDedupSeconds.Maximum = 30;
            numDedupSeconds.Width = 100;
            section.Controls.Add(numDedupSeconds);

            section.Controls.Add(new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(91, 102, 114),
                Location = new Point(132, 141),
                Text = "近い時刻に流れた同じ本文のログを 1 件にまとめます。",
            });

            var lblRealtime = CreateSectionLabel("リアルタイム表示件数");
            lblRealtime.Location = new Point(20, 168);
            section.Controls.Add(lblRealtime);

            numRealtimeDisplayCount.Location = new Point(20, 194);
            numRealtimeDisplayCount.Minimum = 20;
            numRealtimeDisplayCount.Maximum = 2000;
            numRealtimeDisplayCount.Increment = 10;
            numRealtimeDisplayCount.Width = 100;
            section.Controls.Add(numRealtimeDisplayCount);

            section.Controls.Add(new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(91, 102, 114),
                Location = new Point(132, 197),
                Text = "リアルタイム監視モードで表示する、重複統合後の件数です。",
            });

            section.Height = 250;
            return section;
        }

        Control CreateNotificationSection()
        {
            var section = CreateSectionPanel("通知サブ機能");

            chkPlaySound.AutoSize = true;
            chkPlaySound.Location = new Point(20, 42);
            chkPlaySound.Text = "通知時に SE を再生";
            section.Controls.Add(chkPlaySound);

            lblSoundValue.AutoSize = true;
            lblSoundValue.Location = new Point(20, 80);
            lblSoundValue.ForeColor = Color.FromArgb(91, 102, 114);
            section.Controls.Add(lblSoundValue);

            trackSoundVolume.Location = new Point(20, 98);
            trackSoundVolume.AutoSize = false;
            trackSoundVolume.Minimum = 0;
            trackSoundVolume.Maximum = 10;
            trackSoundVolume.TickFrequency = 1;
            trackSoundVolume.Width = 280;
            trackSoundVolume.ValueChanged += TrackSoundVolume_ValueChanged;
            section.Controls.Add(trackSoundVolume);

            var lblPresetTitle = CreateSectionLabel("標準通知ルール");
            lblPresetTitle.Location = new Point(20, 144);
            section.Controls.Add(lblPresetTitle);

            var presetPanel = new TableLayoutPanel
            {
                Location = new Point(20, 172),
                Size = new Size(680, 0),
                ColumnCount = 2,
                RowCount = (NotificationPresets.All.Count + 1) / 2,
            };
            presetPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            presetPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            presetPanel.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;

            for (int i = 0; i < NotificationPresets.All.Count; i++)
            {
                var preset = NotificationPresets.All[i];
                var checkBox = new CheckBox
                {
                    AutoSize = true,
                    Text = preset.DisplayLabel,
                    Margin = new Padding(0, 0, 16, 10),
                };
                presetCheckBoxes[preset.Key] = checkBox;
                presetPanel.Controls.Add(checkBox, i % 2, i / 2);
            }

            presetPanel.Height = (presetPanel.RowCount * 34) + 8;
            section.Controls.Add(presetPanel);
            section.Height = presetPanel.Bottom + 24;
            return section;
        }

        Control CreateCustomKeywordSection()
        {
            var section = CreateSectionPanel("カスタムキーワード通知");

            section.Controls.Add(new Label
            {
                AutoSize = true,
                Location = new Point(20, 42),
                ForeColor = Color.FromArgb(91, 102, 114),
                Text = "形式: キーワード|除外語1,除外語2",
            });

            listCustomKeywords.Location = new Point(20, 72);
            listCustomKeywords.Size = new Size(580, 132);
            section.Controls.Add(listCustomKeywords);

            var btnAdd = CreateSecondaryButton("追加", new Size(84, 34));
            btnAdd.Location = new Point(616, 72);
            btnAdd.Click += BtnAdd_Click;
            section.Controls.Add(btnAdd);

            var btnRemove = CreateSecondaryButton("削除", new Size(84, 34));
            btnRemove.Location = new Point(616, 114);
            btnRemove.Click += BtnRemove_Click;
            section.Controls.Add(btnRemove);

            section.Height = 236;
            return section;
        }

        Control CreateTranslationSection()
        {
            var section = CreateSectionPanel("翻訳");

            var lblProvider = CreateSectionLabel("翻訳プロバイダ");
            lblProvider.Location = new Point(20, 42);
            section.Controls.Add(lblProvider);

            cmbTranslationProvider.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTranslationProvider.Location = new Point(20, 68);
            cmbTranslationProvider.Width = 220;
            cmbTranslationProvider.Items.AddRange(new object[]
            {
                "DeepL API Free",
                "Google Cloud Translation",
                "OpenAI API"
            });
            cmbTranslationProvider.SelectedIndexChanged += CmbTranslationProvider_SelectedIndexChanged;
            section.Controls.Add(cmbTranslationProvider);

            var btnEditDictionary = CreateSecondaryButton("辞書を編集...", new Size(120, 32));
            btnEditDictionary.Location = new Point(256, 66);
            btnEditDictionary.Click += BtnEditDictionary_Click;
            section.Controls.Add(btnEditDictionary);

            btnUploadGlossary.Text = "用語集をアップロード";
            btnUploadGlossary.Size = new Size(186, 32);
            btnUploadGlossary.Location = new Point(388, 66);
            btnUploadGlossary.FlatStyle = FlatStyle.Flat;
            btnUploadGlossary.FlatAppearance.BorderColor = Color.FromArgb(194, 210, 226);
            btnUploadGlossary.BackColor = Color.FromArgb(248, 251, 255);
            btnUploadGlossary.ForeColor = Color.FromArgb(31, 55, 81);
            btnUploadGlossary.Click += BtnUploadGlossary_Click;
            section.Controls.Add(btnUploadGlossary);

            lblTranslationKey.AutoSize = true;
            lblTranslationKey.Font = new Font("Yu Gothic UI", 9F, FontStyle.Regular);
            lblTranslationKey.ForeColor = Color.FromArgb(33, 52, 72);
            lblTranslationKey.BackColor = Color.White;
            lblTranslationKey.Location = new Point(20, 110);
            section.Controls.Add(lblTranslationKey);

            txtDeepLApiKey.Location = new Point(20, 136);
            txtDeepLApiKey.Width = 680;
            txtDeepLApiKey.UseSystemPasswordChar = true;
            section.Controls.Add(txtDeepLApiKey);

            txtGoogleApiKey.Location = new Point(20, 136);
            txtGoogleApiKey.Width = 680;
            txtGoogleApiKey.UseSystemPasswordChar = true;
            section.Controls.Add(txtGoogleApiKey);

            txtOpenAIApiKey.Location = new Point(20, 136);
            txtOpenAIApiKey.Width = 680;
            txtOpenAIApiKey.UseSystemPasswordChar = true;
            section.Controls.Add(txtOpenAIApiKey);

            lblOpenAIReasoningEffort.AutoSize = true;
            lblOpenAIReasoningEffort.Font = new Font("Yu Gothic UI", 9F, FontStyle.Regular);
            lblOpenAIReasoningEffort.ForeColor = Color.FromArgb(33, 52, 72);
            lblOpenAIReasoningEffort.BackColor = Color.White;
            lblOpenAIReasoningEffort.Location = new Point(20, 170);
            lblOpenAIReasoningEffort.Text = "OpenAI reasoning_effort";
            section.Controls.Add(lblOpenAIReasoningEffort);

            cmbOpenAIReasoningEffort.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbOpenAIReasoningEffort.Location = new Point(20, 196);
            cmbOpenAIReasoningEffort.Width = 180;
            cmbOpenAIReasoningEffort.Items.AddRange(new object[]
            {
                OpenAIReasoningEffortHelper.GetLabel(OpenAIReasoningEffort.Low),
                OpenAIReasoningEffortHelper.GetLabel(OpenAIReasoningEffort.Medium),
                OpenAIReasoningEffortHelper.GetLabel(OpenAIReasoningEffort.High),
            });
            section.Controls.Add(cmbOpenAIReasoningEffort);

            lblTranslationHint.AutoSize = true;
            lblTranslationHint.MaximumSize = new Size(680, 0);
            lblTranslationHint.Location = new Point(20, 240);
            lblTranslationHint.ForeColor = Color.FromArgb(91, 102, 114);
            section.Controls.Add(lblTranslationHint);

            section.Height = 320;
            return section;
        }

        Control CreateExternalNotificationSection()
        {
            var section = CreateSectionPanel("外部通知");

            chkDiscord.AutoSize = true;
            chkDiscord.Location = new Point(20, 42);
            chkDiscord.Text = "Discord に通知";
            section.Controls.Add(chkDiscord);

            var lblDiscordWebhook = CreateSectionLabel("Discord Webhook URL");
            lblDiscordWebhook.Location = new Point(20, 78);
            section.Controls.Add(lblDiscordWebhook);

            txtDiscordWebhookUrl.Location = new Point(20, 104);
            txtDiscordWebhookUrl.Width = 680;
            section.Controls.Add(txtDiscordWebhookUrl);

            section.Controls.Add(new Label
            {
                AutoSize = true,
                Location = new Point(20, 142),
                ForeColor = Color.FromArgb(91, 102, 114),
                Text = "通知したい場合だけ、この Webhook URL に送信します。",
            });

            section.Height = 194;
            return section;
        }

        Panel CreateSectionPanel(string title)
        {
            var panel = new Panel
            {
                BackColor = Color.White,
                Width = 720,
                Margin = new Padding(0, 0, 0, 14),
            };
            panel.Controls.Add(new Label
            {
                AutoSize = true,
                Font = new Font("Yu Gothic UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 52, 72),
                Location = new Point(18, 10),
                Text = title,
            });
            return panel;
        }

        Label CreateSectionLabel(string text)
        {
            return new Label
            {
                AutoSize = true,
                Font = new Font("Yu Gothic UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(33, 52, 72),
                Text = text,
            };
        }

        Button CreateSecondaryButton(string text, Size size)
        {
            var button = new Button
            {
                Text = text,
                Size = size,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(248, 251, 255),
                ForeColor = Color.FromArgb(31, 55, 81),
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(194, 210, 226);
            return button;
        }

        Button CreatePrimaryButton(string text, Size size)
        {
            var button = new Button
            {
                Text = text,
                Size = size,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(18, 93, 156),
                ForeColor = Color.White,
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(18, 93, 156);
            return button;
        }

        void LoadSettings()
        {
            txtLogPath.Text = selectedLogPath;
            numDedupSeconds.Value = Math.Max(numDedupSeconds.Minimum, Math.Min(numDedupSeconds.Maximum, settings.DeduplicationSeconds));
            numRealtimeDisplayCount.Value = Math.Max(numRealtimeDisplayCount.Minimum, Math.Min(numRealtimeDisplayCount.Maximum, settings.RealtimeDisplayCount));
            chkPlaySound.Checked = settings.PlaySound;
            trackSoundVolume.Value = Math.Max(trackSoundVolume.Minimum, Math.Min(trackSoundVolume.Maximum, settings.SoundVol));
            chkDiscord.Checked = settings.DiscordNotificationEnabled;
            txtDiscordWebhookUrl.Text = settings.DiscordWebhookUrl ?? string.Empty;
            cmbTranslationProvider.SelectedIndex = GetProviderIndex(settings.TranslationProvider);
            txtDeepLApiKey.Text = settings.DeepLApiKey ?? string.Empty;
            txtGoogleApiKey.Text = settings.GoogleApiKey ?? string.Empty;
            txtOpenAIApiKey.Text = settings.OpenAIApiKey ?? string.Empty;
            cmbOpenAIReasoningEffort.SelectedIndex = GetOpenAIReasoningEffortIndex(settings.OpenAIReasoningEffort);
            UpdateTranslationProviderUi();

            foreach (var preset in NotificationPresets.All)
            {
                settings.StandardTips.TryGetValue(preset.Key, out bool enabled);
                presetCheckBoxes[preset.Key].Checked = enabled;
            }

            listCustomKeywords.Items.Clear();
            foreach (var item in settings.CustomizeTips.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                listCustomKeywords.Items.Add(item);
            }

            UpdateSoundValueLabel();
        }

        void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = @"XG のインストールフォルダを選択してください (例: D:\Games\BlueCrossgate)";
                dialog.ShowNewFolderButton = false;
                dialog.SelectedPath = selectedLogPath;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                if (!CgLogHandler.ValidationPath(dialog.SelectedPath))
                {
                    MessageBox.Show(this, "Log フォルダを含むゲームのルートフォルダを選択してください。", "無効なフォルダ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                selectedLogPath = dialog.SelectedPath;
                txtLogPath.Text = selectedLogPath;
            }
        }

        void BtnAdd_Click(object sender, EventArgs e)
        {
            if (FormPrompt.ShowDialog(this, out string value) != DialogResult.OK || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            listCustomKeywords.Items.Add(value);
        }

        void BtnRemove_Click(object sender, EventArgs e)
        {
            if (listCustomKeywords.SelectedItem == null)
            {
                return;
            }

            listCustomKeywords.Items.Remove(listCustomKeywords.SelectedItem);
        }

        void TrackSoundVolume_ValueChanged(object sender, EventArgs e)
        {
            UpdateSoundValueLabel();
        }

        void CmbTranslationProvider_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateTranslationProviderUi();
        }

        void BtnEditDictionary_Click(object sender, EventArgs e)
        {
            using (var dialog = new FormTranslationDictionaryEditor(TranslationDictionaryStore.Instance))
            {
                dialog.ShowDialog(this);
            }
        }

        async void BtnUploadGlossary_Click(object sender, EventArgs e)
        {
            try
            {
                btnUploadGlossary.Enabled = false;
                string glossaryId = await DeepLGlossaryManager.UploadAsync(txtDeepLApiKey.Text).ConfigureAwait(true);
                settings.SetDeepLGlossaryId(glossaryId);
                MessageBox.Show(this, "DeepL の用語集をアップロードしました。", "用語集アップロード", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"用語集アップロードに失敗しました。\r\n{ex.Message}", "用語集アップロード", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                btnUploadGlossary.Enabled = true;
            }
        }

        void UpdateSoundValueLabel()
        {
            lblSoundValue.Text = $"SE 音量: {trackSoundVolume.Value} / 10";
        }

        void BtnSave_Click(object sender, EventArgs e)
        {
            if (!TrySaveSettings())
            {
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        void FormSettings_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK || !HasUnsavedChanges())
            {
                return;
            }

            var result = MessageBox.Show(
                this,
                "設定に未保存の変更があります。保存して閉じますか？",
                "設定の確認",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            if (result != DialogResult.Yes)
            {
                return;
            }

            if (!TrySaveSettings())
            {
                e.Cancel = true;
                return;
            }

            DialogResult = DialogResult.OK;
        }

        bool HasUnsavedChanges()
        {
            if (!string.Equals(selectedLogPath ?? string.Empty, settings.CgLogPath ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if ((int)numDedupSeconds.Value != settings.DeduplicationSeconds ||
                (int)numRealtimeDisplayCount.Value != settings.RealtimeDisplayCount ||
                chkPlaySound.Checked != settings.PlaySound ||
                trackSoundVolume.Value != settings.SoundVol)
            {
                return true;
            }

            if (chkDiscord.Checked != settings.DiscordNotificationEnabled ||
                !string.Equals(txtDiscordWebhookUrl.Text ?? string.Empty, settings.DiscordWebhookUrl ?? string.Empty, StringComparison.Ordinal) ||
                cmbTranslationProvider.SelectedIndex != GetProviderIndex(settings.TranslationProvider) ||
                !string.Equals(txtDeepLApiKey.Text ?? string.Empty, settings.DeepLApiKey ?? string.Empty, StringComparison.Ordinal) ||
                !string.Equals(txtGoogleApiKey.Text ?? string.Empty, settings.GoogleApiKey ?? string.Empty, StringComparison.Ordinal) ||
                !string.Equals(txtOpenAIApiKey.Text ?? string.Empty, settings.OpenAIApiKey ?? string.Empty, StringComparison.Ordinal) ||
                cmbOpenAIReasoningEffort.SelectedIndex != GetOpenAIReasoningEffortIndex(settings.OpenAIReasoningEffort))
            {
                return true;
            }

            foreach (var preset in NotificationPresets.All)
            {
                settings.StandardTips.TryGetValue(preset.Key, out bool enabled);
                if (presetCheckBoxes[preset.Key].Checked != enabled)
                {
                    return true;
                }
            }

            return !GetCurrentCustomKeywords().SequenceEqual(settings.CustomizeTips.Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        bool TrySaveSettings()
        {
            if (string.IsNullOrWhiteSpace(selectedLogPath) || !CgLogHandler.ValidationPath(selectedLogPath))
            {
                MessageBox.Show(this, "先に正しいゲームフォルダを選択してください。", "設定を保存できません", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (chkDiscord.Checked && string.IsNullOrWhiteSpace(txtDiscordWebhookUrl.Text))
            {
                MessageBox.Show(this, "Discord 通知を使う場合は Webhook URL を入力してください。", "Webhook URL 未設定", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            settings.SetCgLogPath(selectedLogPath);
            settings.SetDeduplicationSeconds((int)numDedupSeconds.Value);
            settings.SetRealtimeDisplayCount((int)numRealtimeDisplayCount.Value);
            settings.SetPlaySound(chkPlaySound.Checked);
            settings.SetSoundVol(trackSoundVolume.Value);
            settings.SetDiscordNotificationEnabled(chkDiscord.Checked);
            settings.SetDiscordWebhookUrl(txtDiscordWebhookUrl.Text);
            settings.SetTranslationProvider(GetSelectedTranslationProvider());
            settings.SetDeepLApiKey(txtDeepLApiKey.Text);
            settings.SetGoogleApiKey(txtGoogleApiKey.Text);
            settings.SetOpenAIApiKey(txtOpenAIApiKey.Text);
            settings.SetOpenAIReasoningEffort(GetSelectedOpenAIReasoningEffort());

            foreach (var preset in NotificationPresets.All)
            {
                settings.SetStandardTip(preset.Key, presetCheckBoxes[preset.Key].Checked);
            }

            var existing = settings.CustomizeTips.ToList();
            foreach (var item in existing)
            {
                settings.RemoveCustmizeTip(item);
            }

            foreach (var item in GetCurrentCustomKeywords())
            {
                settings.AddCustmizeTip(item);
            }

            return true;
        }

        List<string> GetCurrentCustomKeywords()
        {
            return listCustomKeywords.Items.Cast<string>().ToList();
        }

        TranslationProvider GetSelectedTranslationProvider()
        {
            switch (cmbTranslationProvider.SelectedIndex)
            {
                case 1:
                    return TranslationProvider.Google;
                case 2:
                    return TranslationProvider.OpenAI;
                default:
                    return TranslationProvider.DeepL;
            }
        }

        int GetProviderIndex(TranslationProvider provider)
        {
            switch (provider)
            {
                case TranslationProvider.Google:
                    return 1;
                case TranslationProvider.OpenAI:
                    return 2;
                default:
                    return 0;
            }
        }

        OpenAIReasoningEffort GetSelectedOpenAIReasoningEffort()
        {
            switch (cmbOpenAIReasoningEffort.SelectedIndex)
            {
                case 0:
                    return OpenAIReasoningEffort.Low;
                case 2:
                    return OpenAIReasoningEffort.High;
                default:
                    return OpenAIReasoningEffort.Medium;
            }
        }

        int GetOpenAIReasoningEffortIndex(OpenAIReasoningEffort value)
        {
            switch (value)
            {
                case OpenAIReasoningEffort.Low:
                    return 0;
                case OpenAIReasoningEffort.High:
                    return 2;
                default:
                    return 1;
            }
        }

        void UpdateTranslationProviderUi()
        {
            var provider = GetSelectedTranslationProvider();
            txtDeepLApiKey.Visible = provider == TranslationProvider.DeepL;
            txtGoogleApiKey.Visible = provider == TranslationProvider.Google;
            txtOpenAIApiKey.Visible = provider == TranslationProvider.OpenAI;
            lblOpenAIReasoningEffort.Visible = provider == TranslationProvider.OpenAI;
            cmbOpenAIReasoningEffort.Visible = provider == TranslationProvider.OpenAI;
            btnUploadGlossary.Visible = provider == TranslationProvider.DeepL;

            switch (provider)
            {
                case TranslationProvider.Google:
                    lblTranslationKey.Text = "Google Cloud Translation API キー";
                    lblTranslationHint.Text = "Google はローカル辞書を使って翻訳に反映します。";
                    break;
                case TranslationProvider.OpenAI:
                    lblTranslationKey.Text = "OpenAI API キー";
                    lblTranslationHint.Text = "reasoning_effort に応じて OpenAI 翻訳のタイムアウトを調整します。";
                    break;
                default:
                    lblTranslationKey.Text = "DeepL API Free キー";
                    lblTranslationHint.Text = "DeepL はローカル辞書に加えて、用語集アップロードも利用できます。";
                    break;
            }
        }
    }
}
