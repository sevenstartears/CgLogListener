using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CgLogViewer
{
    public sealed class FormNpcDialogue : Form
    {
        readonly Settings settings;
        readonly Func<string, Task<string>> translateAsync;
        readonly Func<bool> hasTranslationCredentials;
        readonly Func<string> getTranslationProviderLabel;
        readonly Panel panelTop = new Panel();
        readonly ComboBox cmbProcesses = new ComboBox();
        readonly Button btnRefreshProcesses = new Button();
        readonly NumericUpDown numLineCount = new NumericUpDown();
        readonly CheckBox chkAutoAdd = new CheckBox();
        readonly Button btnAddToMain = new Button();
        readonly Button btnTranslate = new Button();
        readonly Button btnCopy = new Button();
        readonly Label lblStatus = new Label();
        readonly TextBox txtDialogue = new TextBox();
        readonly Timer pollTimer = new Timer();

        string currentDialogue = string.Empty;
        string translatedDialogue = string.Empty;
        string lastSubmittedDialogue = string.Empty;
        TranslationState translationState = TranslationState.None;
        bool isShowingTranslation;
        bool isPolling;

        public FormNpcDialogue(
            Settings settings,
            Func<string, Task<string>> translateAsync,
            Func<bool> hasTranslationCredentials,
            Func<string> getTranslationProviderLabel)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.translateAsync = translateAsync ?? throw new ArgumentNullException(nameof(translateAsync));
            this.hasTranslationCredentials = hasTranslationCredentials ?? throw new ArgumentNullException(nameof(hasTranslationCredentials));
            this.getTranslationProviderLabel = getTranslationProviderLabel ?? throw new ArgumentNullException(nameof(getTranslationProviderLabel));

            SuspendLayout();

            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(243, 246, 251);
            ClientSize = new Size(860, 620);
            Font = new Font("Yu Gothic UI", 9F, FontStyle.Regular);
            Icon = Resource.icon;
            MinimumSize = new Size(720, 420);
            StartPosition = FormStartPosition.CenterParent;
            Text = "NPC会話抽出";

            panelTop.Dock = DockStyle.Top;
            panelTop.Height = 110;
            panelTop.Padding = new Padding(14, 12, 14, 10);
            panelTop.BackColor = Color.White;

            var layoutTop = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 7,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0),
            };
            layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
            layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250F));
            layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
            layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104F));
            layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
            layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
            layoutTop.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            layoutTop.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));

            var lblProcess = CreateLabel("対象プロセス");
            lblProcess.Margin = new Padding(0, 9, 8, 0);

            cmbProcesses.Dock = DockStyle.Fill;
            cmbProcesses.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbProcesses.Margin = new Padding(0, 4, 8, 0);
            cmbProcesses.SelectedIndexChanged += CmbProcesses_SelectedIndexChanged;

            btnRefreshProcesses.Dock = DockStyle.Fill;
            btnRefreshProcesses.Margin = new Padding(0, 3, 8, 0);
            btnRefreshProcesses.Text = "候補更新";
            btnRefreshProcesses.Click += BtnRefreshProcesses_Click;

            btnAddToMain.Dock = DockStyle.Fill;
            btnAddToMain.Margin = new Padding(8, 3, 0, 0);
            btnAddToMain.Text = "ログに追加";
            btnAddToMain.Click += BtnAddToMain_Click;

            btnTranslate.Dock = DockStyle.Fill;
            btnTranslate.Margin = new Padding(8, 3, 0, 0);
            btnTranslate.Text = "翻訳";
            btnTranslate.Click += BtnTranslate_Click;

            btnCopy.Dock = DockStyle.Fill;
            btnCopy.Margin = new Padding(8, 3, 0, 0);
            btnCopy.Text = "コピー";
            btnCopy.Click += BtnCopy_Click;

            var lblLineCount = CreateLabel("行数");
            lblLineCount.Margin = new Padding(0, 9, 8, 0);

            numLineCount.Dock = DockStyle.Left;
            numLineCount.Minimum = 1;
            numLineCount.Maximum = NpcDialogueReader.MaxLineCount;
            numLineCount.Value = NpcDialogueReader.DefaultLineCount;
            numLineCount.Size = new Size(72, 23);
            numLineCount.Margin = new Padding(0, 4, 8, 0);
            numLineCount.ValueChanged += NumLineCount_ValueChanged;

            chkAutoAdd.AutoSize = true;
            chkAutoAdd.Margin = new Padding(8, 8, 0, 0);
            chkAutoAdd.ForeColor = Color.FromArgb(63, 74, 86);
            chkAutoAdd.Text = "自動的にログに追加する";
            chkAutoAdd.Checked = settings.NpcDialogueAutoAdd;
            chkAutoAdd.CheckedChanged += ChkAutoAdd_CheckedChanged;

            lblStatus.AutoSize = false;
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.Margin = new Padding(0, 7, 0, 0);
            lblStatus.ForeColor = Color.FromArgb(91, 102, 114);
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;

            layoutTop.Controls.Add(lblProcess, 0, 0);
            layoutTop.Controls.Add(cmbProcesses, 1, 0);
            layoutTop.Controls.Add(btnRefreshProcesses, 2, 0);
            layoutTop.Controls.Add(btnAddToMain, 4, 0);
            layoutTop.Controls.Add(btnTranslate, 5, 0);
            layoutTop.Controls.Add(btnCopy, 6, 0);
            layoutTop.Controls.Add(lblLineCount, 0, 1);
            layoutTop.Controls.Add(numLineCount, 1, 1);
            layoutTop.Controls.Add(lblStatus, 2, 1);
            layoutTop.Controls.Add(chkAutoAdd, 5, 1);
            layoutTop.SetColumnSpan(lblStatus, 3);
            layoutTop.SetColumnSpan(chkAutoAdd, 2);
            panelTop.Controls.Add(layoutTop);

            var panelBody = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(243, 246, 251),
                Padding = new Padding(14, 14, 14, 14),
            };

            txtDialogue.Dock = DockStyle.Fill;
            txtDialogue.Multiline = true;
            txtDialogue.ReadOnly = true;
            txtDialogue.ScrollBars = ScrollBars.Vertical;
            txtDialogue.WordWrap = true;
            txtDialogue.Font = new Font("MingLiU", 12F, FontStyle.Bold, GraphicsUnit.Point, 136);
            txtDialogue.BackColor = Color.White;

            panelBody.Controls.Add(txtDialogue);

            Controls.Add(panelBody);
            Controls.Add(panelTop);

            pollTimer.Interval = 350;
            pollTimer.Tick += PollTimer_Tick;

            Load += FormNpcDialogue_Load;
            Shown += FormNpcDialogue_Shown;

            ResumeLayout(false);
        }

        public void RefreshCandidates()
        {
            int previousProcessId = (cmbProcesses.SelectedItem as NpcDialogueProcessInfo)?.ProcessId ?? -1;
            var candidates = NpcDialogueReader.FindCandidateProcesses(settings.CgLogPath);

            cmbProcesses.BeginUpdate();
            try
            {
                cmbProcesses.Items.Clear();
                foreach (var candidate in candidates)
                {
                    cmbProcesses.Items.Add(candidate);
                }

                var selected = candidates.FirstOrDefault(item => item.ProcessId == previousProcessId) ?? candidates.FirstOrDefault();
                cmbProcesses.SelectedItem = selected;
            }
            finally
            {
                cmbProcesses.EndUpdate();
            }

            if (candidates.Count == 0)
            {
                ResetDialogueState();
                lblStatus.Text = string.IsNullOrWhiteSpace(settings.CgLogPath)
                    ? "ゲームフォルダ未設定です。"
                    : "候補になる bluecg.exe プロセスが見つかりません。";
            }
            else
            {
                lblStatus.Text = string.Format("{0} 件の候補を表示しています。", candidates.Count);
            }

            UpdateActionButtons();
        }

        public void RefreshTranslationState()
        {
            UpdateActionButtons();
        }

        void FormNpcDialogue_Load(object sender, EventArgs e)
        {
            RefreshCandidates();
            RefreshTranslationState();
        }

        void FormNpcDialogue_Shown(object sender, EventArgs e)
        {
            pollTimer.Start();
            _ = PollSelectedProcessAsync();
        }

        void BtnRefreshProcesses_Click(object sender, EventArgs e)
        {
            RefreshCandidates();
        }

        void CmbProcesses_SelectedIndexChanged(object sender, EventArgs e)
        {
            ResetDialogueState();
            UpdateActionButtons();
            _ = PollSelectedProcessAsync();
        }

        void NumLineCount_ValueChanged(object sender, EventArgs e)
        {
            _ = PollSelectedProcessAsync();
        }

        void ChkAutoAdd_CheckedChanged(object sender, EventArgs e)
        {
            settings.SetNpcDialogueAutoAdd(chkAutoAdd.Checked);
        }

        async void BtnTranslate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(currentDialogue))
            {
                return;
            }

            if (translationState == TranslationState.Ready && !string.IsNullOrWhiteSpace(translatedDialogue))
            {
                isShowingTranslation = !isShowingTranslation;
                txtDialogue.Text = GetVisibleDialogue();
                UpdateActionButtons();
                return;
            }

            if (translationState == TranslationState.InProgress)
            {
                return;
            }

            if (!hasTranslationCredentials())
            {
                MessageBox.Show(
                    this,
                    string.Format("翻訳を使うには設定画面で {0} の API キーを入力してください。", getTranslationProviderLabel()),
                    "翻訳未設定",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            translationState = TranslationState.InProgress;
            UpdateActionButtons();
            lblStatus.Text = "翻訳中...";

            try
            {
                translatedDialogue = (await translateAsync(currentDialogue).ConfigureAwait(true) ?? string.Empty).Trim();
                translationState = TranslationState.Ready;
                isShowingTranslation = true;
                txtDialogue.Text = GetVisibleDialogue();
                lblStatus.Text = "翻訳結果を更新しました。";
            }
            catch (Exception ex)
            {
                translationState = TranslationState.Failed;
                isShowingTranslation = false;
                MessageBox.Show(
                    this,
                    string.Format("{0} での翻訳に失敗しました。\r\n{1}", getTranslationProviderLabel(), ex.Message),
                    "翻訳エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                lblStatus.Text = "翻訳に失敗しました。";
            }
            finally
            {
                UpdateActionButtons();
            }
        }

        void BtnCopy_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(currentDialogue))
            {
                return;
            }

            try
            {
                Clipboard.SetText(GetVisibleDialogue());
                lblStatus.Text = "NPC会話をコピーしました。";
            }
            catch
            {
                lblStatus.Text = "コピーに失敗しました。";
            }
        }

        void BtnAddToMain_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(currentDialogue))
            {
                return;
            }

            if (TrySubmitCurrentDialogue())
            {
                lblStatus.Text = "メインビューへ NPC 会話を追加しました。";
            }
            else
            {
                lblStatus.Text = "メインビューへの追加に失敗しました。";
            }
        }

        async void PollTimer_Tick(object sender, EventArgs e)
        {
            await PollSelectedProcessAsync().ConfigureAwait(true);
        }

        async Task PollSelectedProcessAsync()
        {
            if (isPolling)
            {
                return;
            }

            var selected = cmbProcesses.SelectedItem as NpcDialogueProcessInfo;
            if (selected == null)
            {
                return;
            }

            isPolling = true;
            int lineCount = Decimal.ToInt32(numLineCount.Value);
            try
            {
                await Task.Run(() =>
                {
                    NpcDialogueSnapshot snapshot;
                    string errorMessage;
                    if (!NpcDialogueReader.TryReadDialogue(selected.ProcessId, lineCount, out snapshot, out errorMessage))
                    {
                        BeginInvoke((Action)(() =>
                        {
                            if (!IsDisposed)
                            {
                                lblStatus.Text = errorMessage;
                            }
                        }));
                        return;
                    }

                    BeginInvoke((Action)(() => ApplySnapshot(selected, snapshot)));
                }).ConfigureAwait(true);
            }
            finally
            {
                isPolling = false;
            }
        }

        void ApplySnapshot(NpcDialogueProcessInfo processInfo, NpcDialogueSnapshot snapshot)
        {
            if (IsDisposed)
            {
                return;
            }

            string text = snapshot != null ? snapshot.Text : string.Empty;
            if (!string.Equals(currentDialogue, text, StringComparison.Ordinal))
            {
                currentDialogue = text;
                translatedDialogue = string.Empty;
                translationState = TranslationState.None;
                isShowingTranslation = false;
                txtDialogue.Text = GetVisibleDialogue();

                if (!string.IsNullOrWhiteSpace(currentDialogue) && chkAutoAdd.Checked)
                {
                    TrySubmitCurrentDialogue();
                }
            }

            lblStatus.Text = string.IsNullOrWhiteSpace(text)
                ? string.Format("{0} を監視中です。現在は会話行がありません。", processInfo)
                : string.Format("{0} を監視中です。{1} 行を表示中。", processInfo, snapshot.Lines.Count);

            UpdateActionButtons();
        }

        void ResetDialogueState()
        {
            currentDialogue = string.Empty;
            translatedDialogue = string.Empty;
            lastSubmittedDialogue = string.Empty;
            translationState = TranslationState.None;
            isShowingTranslation = false;
            txtDialogue.Text = string.Empty;
        }

        bool TrySubmitCurrentDialogue()
        {
            if (string.IsNullOrWhiteSpace(currentDialogue))
            {
                return false;
            }

            if (string.Equals(lastSubmittedDialogue, currentDialogue, StringComparison.Ordinal))
            {
                return true;
            }

            if (!NpcDialogueBridgeClient.TrySend(currentDialogue))
            {
                return false;
            }

            lastSubmittedDialogue = currentDialogue;
            return true;
        }

        string GetVisibleDialogue()
        {
            if (isShowingTranslation && translationState == TranslationState.Ready && !string.IsNullOrWhiteSpace(translatedDialogue))
            {
                return translatedDialogue;
            }

            return currentDialogue;
        }

        void UpdateActionButtons()
        {
            bool hasDialogue = !string.IsNullOrWhiteSpace(currentDialogue);
            btnAddToMain.Enabled = hasDialogue;
            btnCopy.Enabled = hasDialogue;

            if (!hasDialogue)
            {
                btnTranslate.Enabled = false;
                btnTranslate.Text = "翻訳";
                return;
            }

            switch (translationState)
            {
                case TranslationState.InProgress:
                    btnTranslate.Enabled = false;
                    btnTranslate.Text = "翻訳中";
                    break;
                case TranslationState.Ready:
                    btnTranslate.Enabled = true;
                    btnTranslate.Text = isShowingTranslation ? "原文" : "翻訳";
                    break;
                case TranslationState.Failed:
                    btnTranslate.Enabled = hasTranslationCredentials();
                    btnTranslate.Text = "再試行";
                    break;
                default:
                    btnTranslate.Enabled = true;
                    btnTranslate.Text = "翻訳";
                    break;
            }
        }

        static Label CreateLabel(string text)
        {
            return new Label
            {
                AutoSize = true,
                Text = text,
                ForeColor = Color.FromArgb(63, 74, 86)
            };
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            pollTimer.Stop();
            pollTimer.Dispose();
            base.OnFormClosed(e);
        }
    }
}
