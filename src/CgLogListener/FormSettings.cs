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
        readonly CheckBox chkTelegram = new CheckBox();
        readonly CheckBox chkDiscord = new CheckBox();
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
            flowSections.Controls.Add(CreateExternalNotificationSection());

            panelContent.Controls.Add(flowSections);

            var panelButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 82,
                BackColor = Color.White,
                Padding = new Padding(24, 18, 24, 18),
            };

            var btnCancel = new Button
            {
                DialogResult = DialogResult.Cancel,
                Text = "閉じる",
                Width = 128,
                Height = 40,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Location = new Point(536, 20),
            };
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(204, 214, 224);
            btnCancel.BackColor = Color.White;

            var btnSave = new Button
            {
                Text = "設定を保存",
                Width = 148,
                Height = 40,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Location = new Point(676, 20),
            };
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.FlatAppearance.BorderColor = Color.FromArgb(18, 93, 156);
            btnSave.BackColor = Color.FromArgb(18, 93, 156);
            btnSave.ForeColor = Color.White;
            btnSave.Click += BtnSave_Click;

            panelButtons.Controls.Add(btnCancel);
            panelButtons.Controls.Add(btnSave);

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            Controls.Add(panelContent);
            Controls.Add(panelButtons);
            Controls.Add(panelHeader);

            ResumeLayout(false);
        }

        Control CreateGeneralSection()
        {
            var section = CreateSectionPanel("ログ表示");

            var lblPath = CreateSectionLabel("ゲームフォルダ");
            lblPath.Location = new Point(20, 30);

            txtLogPath.Location = new Point(20, 56);
            txtLogPath.ReadOnly = true;
            txtLogPath.Width = 590;
            txtLogPath.BackColor = Color.White;

            var btnBrowse = new Button
            {
                Text = "参照...",
                Location = new Point(626, 54),
                Size = new Size(90, 32),
            };
            StyleSecondaryButton(btnBrowse);
            btnBrowse.Click += BtnBrowse_Click;

            var lblDedup = CreateSectionLabel("同一ログのまとめ秒数");
            lblDedup.Location = new Point(20, 102);

            numDedupSeconds.Location = new Point(20, 128);
            numDedupSeconds.Minimum = 0;
            numDedupSeconds.Maximum = 30;
            numDedupSeconds.Width = 100;

            var lblDedupHint = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(91, 102, 114),
                Location = new Point(132, 131),
                Text = "近い時刻に流れた同じ本文のログを 1 件にまとめます。",
            };

            section.Controls.Add(lblPath);
            section.Controls.Add(txtLogPath);
            section.Controls.Add(btnBrowse);
            section.Controls.Add(lblDedup);
            section.Controls.Add(numDedupSeconds);
            section.Controls.Add(lblDedupHint);
            section.Height = 184;
            return section;
        }

        Control CreateNotificationSection()
        {
            var section = CreateSectionPanel("通知サブ機能");

            chkPlaySound.AutoSize = true;
            chkPlaySound.Location = new Point(20, 36);
            chkPlaySound.Text = "通知時に SE を再生";

            lblSoundValue.AutoSize = true;
            lblSoundValue.Location = new Point(20, 72);
            lblSoundValue.ForeColor = Color.FromArgb(91, 102, 114);

            trackSoundVolume.Location = new Point(20, 90);
            trackSoundVolume.AutoSize = false;
            trackSoundVolume.Minimum = 0;
            trackSoundVolume.Maximum = 10;
            trackSoundVolume.TickFrequency = 1;
            trackSoundVolume.Width = 280;
            trackSoundVolume.ValueChanged += TrackSoundVolume_ValueChanged;

            var lblPresetTitle = CreateSectionLabel("標準通知ルール");
            lblPresetTitle.Location = new Point(20, 134);

            var presetPanel = new TableLayoutPanel
            {
                Location = new Point(20, 162),
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
            section.Height = 294;
            return section;
        }

        Control CreateCustomKeywordSection()
        {
            var section = CreateSectionPanel("カスタムキーワード通知");

            var lblHint = new Label
            {
                AutoSize = true,
                Location = new Point(20, 34),
                ForeColor = Color.FromArgb(91, 102, 114),
                Text = "形式: キーワード|除外語1,除外語2",
            };

            listCustomKeywords.Location = new Point(20, 64);
            listCustomKeywords.Size = new Size(580, 132);

            var btnAdd = new Button
            {
                Text = "追加",
                Location = new Point(616, 64),
                Size = new Size(84, 34),
            };
            StyleSecondaryButton(btnAdd);
            btnAdd.Click += BtnAdd_Click;

            var btnRemove = new Button
            {
                Text = "削除",
                Location = new Point(616, 106),
                Size = new Size(84, 34),
            };
            StyleSecondaryButton(btnRemove);
            btnRemove.Click += BtnRemove_Click;

            section.Controls.Add(lblHint);
            section.Controls.Add(listCustomKeywords);
            section.Controls.Add(btnAdd);
            section.Controls.Add(btnRemove);
            section.Height = 228;
            return section;
        }

        Control CreateExternalNotificationSection()
        {
            var section = CreateSectionPanel("外部送信");

            chkTelegram.AutoSize = true;
            chkTelegram.Location = new Point(20, 38);
            chkTelegram.Text = "Telegram に送信";

            var lblTelegram = new Label
            {
                AutoSize = true,
                Location = new Point(40, 64),
                ForeColor = Color.FromArgb(91, 102, 114),
                Text = "token と chat_id は TelegramNotifier.ini に設定します。",
            };

            chkDiscord.AutoSize = true;
            chkDiscord.Location = new Point(20, 104);
            chkDiscord.Text = "Discord に送信";

            var lblDiscord = new Label
            {
                AutoSize = true,
                Location = new Point(40, 130),
                ForeColor = Color.FromArgb(91, 102, 114),
                Text = "Webhook URL は DiscordNotifier.ini に設定します。",
            };

            section.Controls.Add(chkTelegram);
            section.Controls.Add(lblTelegram);
            section.Controls.Add(chkDiscord);
            section.Controls.Add(lblDiscord);
            section.Height = 192;
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
            chkTelegram.Checked = settings.CustomNotifyTypes.Contains(FormMain.CustomNotifyType.Telegram);
            chkDiscord.Checked = settings.CustomNotifyTypes.Contains(FormMain.CustomNotifyType.Discord);

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

            if (chkTelegram.Checked != settings.CustomNotifyTypes.Contains(FormMain.CustomNotifyType.Telegram) ||
                chkDiscord.Checked != settings.CustomNotifyTypes.Contains(FormMain.CustomNotifyType.Discord))
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

            settings.SetCgLogPath(selectedLogPath);
            settings.SetDeduplicationSeconds((int)numDedupSeconds.Value);
            settings.SetPlaySound(chkPlaySound.Checked);
            settings.SetSoundVol(trackSoundVolume.Value);

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

            ApplyNotifierSetting(chkTelegram.Checked, FormMain.CustomNotifyType.Telegram);
            ApplyNotifierSetting(chkDiscord.Checked, FormMain.CustomNotifyType.Discord);
            return true;
        }

        List<string> GetCurrentCustomKeywords()
        {
            return listCustomKeywords.Items.Cast<string>().ToList();
        }

        void ApplyNotifierSetting(bool enabled, FormMain.CustomNotifyType notifyType)
        {
            if (enabled)
            {
                settings.SetCustomNotify(notifyType);
            }
            else
            {
                settings.RemoveCustomNotify(notifyType);
            }
        }
    }
}
