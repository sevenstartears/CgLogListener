using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CgLogListener
{
    public class FormSettings : Form
    {
        readonly Settings settings;
        readonly TextBox txtLogPath = new TextBox();
        readonly NumericUpDown numDedupSeconds = new NumericUpDown();
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
        readonly Label lblTranslationHint = new Label();
        readonly ListBox listCustomKeywords = new ListBox();
        readonly Dictionary<string, CheckBox> presetCheckBoxes = new Dictionary<string, CheckBox>();
        string selectedLogPath;

        public FormSettings(Settings settings)
        {
            this.settings = settings;
            selectedLogPath = settings.CgLogPath;

            InitializeComponent();
            LoadSettings();
        }

        void InitializeComponent()
        {
            SuspendLayout();

            AutoScaleMode = AutoScaleMode.Font;
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
            var lblTitle = new Label
            {
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Yu Gothic UI Semibold", 18F, FontStyle.Bold),
                Text = "表示と通知の設定",
                Location = new Point(0, 0),
            };
            var lblSubtitle = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 238, 255),
                Font = new Font("Yu Gothic UI", 9.5F, FontStyle.Regular),
                Text = "ログ表示のまとめ秒数、通知音、外部送信の設定をここで変更できます。",
                Location = new Point(2, 42),
            };
            panelHeader.Controls.Add(lblTitle);
            panelHeader.Controls.Add(lblSubtitle);

            var panelTopActions = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.White,
                Padding = new Padding(24, 12, 24, 12),
            };
            var panelTopActionButtons = new FlowLayoutPanel
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
            var lblTopHint = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(91, 102, 114),
                Location = new Point(24, 24),
                Text = "変更内容は保存ボタンで反映されます。",
            };
            var btnTopClose = new Button
            {
                DialogResult = DialogResult.Cancel,
                Text = "閉じる",
                Width = 128,
                Height = 40,
                Margin = new Padding(0, 0, 12, 0),
            };
            btnTopClose.FlatStyle = FlatStyle.Flat;
            btnTopClose.FlatAppearance.BorderColor = Color.FromArgb(204, 214, 224);
            btnTopClose.BackColor = Color.White;

            var btnTopSave = new Button
            {
                Text = "保存",
                Width = 148,
                Height = 40,
                Margin = new Padding(0),
            };
            btnTopSave.FlatStyle = FlatStyle.Flat;
            btnTopSave.FlatAppearance.BorderColor = Color.FromArgb(18, 93, 156);
            btnTopSave.BackColor = Color.FromArgb(18, 93, 156);
            btnTopSave.ForeColor = Color.White;
            btnTopSave.Click += BtnSave_Click;
            panelTopActionButtons.Controls.Add(btnTopClose);
            panelTopActionButtons.Controls.Add(btnTopSave);
            panelTopActions.Controls.Add(lblTopHint);
            panelTopActions.Controls.Add(panelTopActionButtons);

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

            var panelButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 82,
                BackColor = Color.White,
                Padding = new Padding(24, 18, 24, 18),
            };
            var panelBottomActionButtons = new FlowLayoutPanel
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

            var btnCancel = new Button
            {
                DialogResult = DialogResult.Cancel,
                Text = "閉じる",
                Width = 128,
                Height = 40,
                Margin = new Padding(0, 0, 12, 0),
            };
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(204, 214, 224);
            btnCancel.BackColor = Color.White;

            var btnSave = new Button
            {
                Text = "設定を保存",
                Width = 148,
                Height = 40,
                Margin = new Padding(0),
            };
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.FlatAppearance.BorderColor = Color.FromArgb(18, 93, 156);
            btnSave.BackColor = Color.FromArgb(18, 93, 156);
            btnSave.ForeColor = Color.White;
            btnSave.Click += BtnSave_Click;

            panelBottomActionButtons.Controls.Add(btnCancel);
            panelBottomActionButtons.Controls.Add(btnSave);
            panelButtons.Controls.Add(panelBottomActionButtons);

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            Controls.Add(panelContent);
            Controls.Add(panelButtons);
            Controls.Add(panelTopActions);
            Controls.Add(panelHeader);

            ResumeLayout(false);
        }

        Control CreateGeneralSection()
        {
            var section = CreateSectionPanel("ログ表示");

            var lblPath = CreateSectionLabel("ゲームフォルダ");
            lblPath.Location = new Point(20, 40);

            txtLogPath.Location = new Point(20, 66);
            txtLogPath.ReadOnly = true;
            txtLogPath.Width = 590;
            txtLogPath.BackColor = Color.White;

            var btnBrowse = new Button
            {
                Text = "参照...",
                Location = new Point(626, 64),
                Size = new Size(90, 32),
            };
            StyleSecondaryButton(btnBrowse);
            btnBrowse.Click += BtnBrowse_Click;

            var lblDedup = CreateSectionLabel("同一ログのまとめ秒数");
            lblDedup.Location = new Point(20, 112);

            numDedupSeconds.Location = new Point(20, 138);
            numDedupSeconds.Minimum = 0;
            numDedupSeconds.Maximum = 30;
            numDedupSeconds.Width = 100;

            var lblDedupHint = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(91, 102, 114),
                Location = new Point(132, 141),
                Text = "近い時刻に流れた同じ本文のログを 1 件にまとめます。",
            };

            section.Controls.Add(lblPath);
            section.Controls.Add(txtLogPath);
            section.Controls.Add(btnBrowse);
            section.Controls.Add(lblDedup);
            section.Controls.Add(numDedupSeconds);
            section.Controls.Add(lblDedupHint);
            section.Height = 194;
            return section;
        }

        Control CreateNotificationSection()
        {
            var section = CreateSectionPanel("通知サブ機能");

            chkPlaySound.AutoSize = true;
            chkPlaySound.Location = new Point(20, 42);
            chkPlaySound.Text = "通知時に SE を再生";

            lblSoundValue.AutoSize = true;
            lblSoundValue.Location = new Point(20, 80);
            lblSoundValue.ForeColor = Color.FromArgb(91, 102, 114);

            trackSoundVolume.Location = new Point(20, 98);
            trackSoundVolume.AutoSize = false;
            trackSoundVolume.Minimum = 0;
            trackSoundVolume.Maximum = 10;
            trackSoundVolume.TickFrequency = 1;
            trackSoundVolume.Width = 280;
            trackSoundVolume.ValueChanged += TrackSoundVolume_ValueChanged;

            var lblPresetTitle = CreateSectionLabel("標準通知ルール");
            lblPresetTitle.Location = new Point(20, 144);

            var presetPanel = new TableLayoutPanel
            {
                Location = new Point(20, 172),
                Size = new Size(680, 104),
                ColumnCount = 2,
                RowCount = 3,
            };
            presetPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            presetPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            for (int i = 0; i < NotificationPresets.All.Count; i++)
            {
                var preset = NotificationPresets.All[i];
                var checkBox = new CheckBox
                {
                    AutoSize = true,
                    Text = preset.Label,
                    Margin = new Padding(0, 0, 16, 10),
                };
                presetCheckBoxes[preset.Key] = checkBox;
                presetPanel.Controls.Add(checkBox, i % 2, i / 2);
            }

            section.Controls.Add(chkPlaySound);
            section.Controls.Add(lblSoundValue);
            section.Controls.Add(trackSoundVolume);
            section.Controls.Add(lblPresetTitle);
            section.Controls.Add(presetPanel);
            section.Height = 304;
            return section;
        }

        Control CreateCustomKeywordSection()
        {
            var section = CreateSectionPanel("カスタムキーワード通知");

            var lblHint = new Label
            {
                AutoSize = true,
                Location = new Point(20, 42),
                ForeColor = Color.FromArgb(91, 102, 114),
                Text = "形式: キーワード|除外語1,除外語2",
            };

            listCustomKeywords.Location = new Point(20, 72);
            listCustomKeywords.Size = new Size(580, 132);

            var btnAdd = new Button
            {
                Text = "追加",
                Location = new Point(616, 72),
                Size = new Size(84, 34),
            };
            StyleSecondaryButton(btnAdd);
            btnAdd.Click += BtnAdd_Click;

            var btnRemove = new Button
            {
                Text = "削除",
                Location = new Point(616, 114),
                Size = new Size(84, 34),
            };
            StyleSecondaryButton(btnRemove);
            btnRemove.Click += BtnRemove_Click;

            section.Controls.Add(lblHint);
            section.Controls.Add(listCustomKeywords);
            section.Controls.Add(btnAdd);
            section.Controls.Add(btnRemove);
            section.Height = 236;
            return section;
        }

        Control CreateTranslationSection()
        {
            var section = CreateSectionPanel("翻訳");

            var lblProvider = CreateSectionLabel("翻訳プロバイダ");
            lblProvider.Location = new Point(20, 42);

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

            lblTranslationKey.AutoSize = true;
            lblTranslationKey.Font = new Font("Yu Gothic UI", 9F, FontStyle.Regular);
            lblTranslationKey.ForeColor = Color.FromArgb(33, 52, 72);
            lblTranslationKey.BackColor = Color.White;
            lblTranslationKey.Location = new Point(20, 110);

            txtDeepLApiKey.Location = new Point(20, 136);
            txtDeepLApiKey.Width = 680;
            txtDeepLApiKey.UseSystemPasswordChar = true;

            txtGoogleApiKey.Location = new Point(20, 136);
            txtGoogleApiKey.Width = 680;
            txtGoogleApiKey.UseSystemPasswordChar = true;

            txtOpenAIApiKey.Location = new Point(20, 136);
            txtOpenAIApiKey.Width = 680;
            txtOpenAIApiKey.UseSystemPasswordChar = true;

            lblTranslationHint.AutoSize = true;
            lblTranslationHint.Location = new Point(20, 170);
            lblTranslationHint.ForeColor = Color.FromArgb(91, 102, 114);

            section.Controls.Add(lblProvider);
            section.Controls.Add(cmbTranslationProvider);
            section.Controls.Add(lblTranslationKey);
            section.Controls.Add(txtDeepLApiKey);
            section.Controls.Add(txtGoogleApiKey);
            section.Controls.Add(txtOpenAIApiKey);
            section.Controls.Add(lblTranslationHint);
            section.Height = 222;
            return section;
        }

        Control CreateExternalNotificationSection()
        {
            var section = CreateSectionPanel("外部送信");

            chkDiscord.AutoSize = true;
            chkDiscord.Location = new Point(20, 42);
            chkDiscord.Text = "Discord に送信";

            var lblDiscordWebhook = CreateSectionLabel("Discord Webhook URL");
            lblDiscordWebhook.Location = new Point(20, 78);

            txtDiscordWebhookUrl.Location = new Point(20, 104);
            txtDiscordWebhookUrl.Width = 680;

            var lblDiscord = new Label
            {
                AutoSize = true,
                Location = new Point(20, 142),
                ForeColor = Color.FromArgb(91, 102, 114),
                Text = "通知が一致した時だけ、この Webhook URL に直接送信します。",
            };

            section.Controls.Add(chkDiscord);
            section.Controls.Add(lblDiscordWebhook);
            section.Controls.Add(txtDiscordWebhookUrl);
            section.Controls.Add(lblDiscord);
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

            var lblTitle = new Label
            {
                AutoSize = true,
                Font = new Font("Yu Gothic UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 52, 72),
                Location = new Point(18, 10),
                Text = title,
            };

            panel.Controls.Add(lblTitle);
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

        void StyleSecondaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(194, 210, 226);
            button.BackColor = Color.FromArgb(248, 251, 255);
            button.ForeColor = Color.FromArgb(31, 55, 81);
        }

        void LoadSettings()
        {
            txtLogPath.Text = selectedLogPath;
            numDedupSeconds.Value = (decimal)Math.Max((int)numDedupSeconds.Minimum, Math.Min((int)numDedupSeconds.Maximum, settings.DeduplicationSeconds));
            chkPlaySound.Checked = settings.PlaySound;
            trackSoundVolume.Value = Math.Max(trackSoundVolume.Minimum, Math.Min(trackSoundVolume.Maximum, settings.SoundVol));
            chkDiscord.Checked = settings.DiscordNotificationEnabled;
            txtDiscordWebhookUrl.Text = settings.DiscordWebhookUrl ?? string.Empty;
            cmbTranslationProvider.SelectedIndex = GetProviderIndex(settings.TranslationProvider);
            txtDeepLApiKey.Text = settings.DeepLApiKey ?? string.Empty;
            txtGoogleApiKey.Text = settings.GoogleApiKey ?? string.Empty;
            txtOpenAIApiKey.Text = settings.OpenAIApiKey ?? string.Empty;
            UpdateTranslationProviderUi();

            foreach (var preset in NotificationPresets.All)
            {
                settings.StandardTips.TryGetValue(preset.Key, out bool isEnabled);
                presetCheckBoxes[preset.Key].Checked = isEnabled;
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
                dialog.Description = @"Xg がインストールされているフォルダを選択してください (例: D:\CrossGate\)";
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
                "設定内容が保存されていません。保存して閉じますか？",
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
                !string.Equals(txtOpenAIApiKey.Text ?? string.Empty, settings.OpenAIApiKey ?? string.Empty, StringComparison.Ordinal))
            {
                return true;
            }

            foreach (var preset in NotificationPresets.All)
            {
                settings.StandardTips.TryGetValue(preset.Key, out bool isEnabled);
                if (presetCheckBoxes[preset.Key].Checked != isEnabled)
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
                MessageBox.Show(this, "先に有効なゲームフォルダを選択してください。", "設定を保存できません", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (chkDiscord.Checked && string.IsNullOrWhiteSpace(txtDiscordWebhookUrl.Text))
            {
                MessageBox.Show(this, "Discord 送信を有効にする場合は Webhook URL を入力してください。", "Webhook URL 未設定", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            settings.SetCgLogPath(selectedLogPath);
            settings.SetDeduplicationSeconds((int)numDedupSeconds.Value);
            settings.SetPlaySound(chkPlaySound.Checked);
            settings.SetSoundVol(trackSoundVolume.Value);
            settings.SetDiscordNotificationEnabled(chkDiscord.Checked);
            settings.SetDiscordWebhookUrl(txtDiscordWebhookUrl.Text);
            settings.SetTranslationProvider(GetSelectedTranslationProvider());
            settings.SetDeepLApiKey(txtDeepLApiKey.Text);
            settings.SetGoogleApiKey(txtGoogleApiKey.Text);
            settings.SetOpenAIApiKey(txtOpenAIApiKey.Text);

            foreach (var preset in NotificationPresets.All)
            {
                settings.SetStandardTip(preset.Key, presetCheckBoxes[preset.Key].Checked);
            }

            var currentKeywords = settings.CustomizeTips.ToList();
            foreach (var keyword in currentKeywords)
            {
                settings.RemoveCustmizeTip(keyword);
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

        void UpdateTranslationProviderUi()
        {
            var provider = GetSelectedTranslationProvider();
            txtDeepLApiKey.Visible = provider == TranslationProvider.DeepL;
            txtGoogleApiKey.Visible = provider == TranslationProvider.Google;
            txtOpenAIApiKey.Visible = provider == TranslationProvider.OpenAI;

            switch (provider)
            {
                case TranslationProvider.Google:
                    lblTranslationKey.Text = "Google Cloud Translation API キー";
                    lblTranslationHint.Text = "ログカードの「翻訳」ボタンを押した時だけ Google Cloud Translation API で日本語に翻訳します。";
                    break;
                case TranslationProvider.OpenAI:
                    lblTranslationKey.Text = "OpenAI API キー";
                    lblTranslationHint.Text = "ログカードの「翻訳」ボタンを押した時だけ OpenAI Responses API と gpt-5-mini で日本語に翻訳します。";
                    break;
                default:
                    lblTranslationKey.Text = "DeepL API Free キー";
                    lblTranslationHint.Text = "ログカードの「翻訳」ボタンを押した時だけ DeepL API Free で日本語に翻訳します。";
                    break;
            }
        }

    }
}
