using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CgLogViewer
{
    public sealed class FormTranslationDictionaryEditor : Form
    {
        readonly TranslationDictionaryStore store;
        readonly BindingList<TranslationDictionaryEntry> entries = new BindingList<TranslationDictionaryEntry>();
        readonly DataGridView grid = new DataGridView();
        readonly Label lblPath = new Label();

        public FormTranslationDictionaryEditor(TranslationDictionaryStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));

            SuspendLayout();

            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(244, 247, 251);
            ClientSize = new Size(860, 620);
            MinimumSize = new Size(780, 520);
            Font = new Font("Yu Gothic UI", 9F, FontStyle.Regular);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "翻訳辞書の編集";

            var panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 82,
                Padding = new Padding(16, 12, 16, 12),
                BackColor = Color.White,
            };

            var lblTitle = new Label
            {
                AutoSize = true,
                Font = new Font("Yu Gothic UI Semibold", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 52, 72),
                Text = "クロスゲート固有名詞辞書"
            };

            lblPath.AutoSize = true;
            lblPath.Location = new Point(18, 42);
            lblPath.ForeColor = Color.FromArgb(91, 102, 114);
            lblPath.Text = store.FilePath;
            panelTop.Controls.Add(lblTitle);
            panelTop.Controls.Add(lblPath);

            var panelButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 52,
                Padding = new Padding(16, 10, 16, 8),
                WrapContents = false,
                BackColor = Color.FromArgb(244, 247, 251),
            };

            panelButtons.Controls.Add(CreateButton("行を追加", BtnAdd_Click));
            panelButtons.Controls.Add(CreateButton("選択行を削除", BtnRemove_Click));
            panelButtons.Controls.Add(CreateButton("保存", BtnSave_Click, primary: true));

            grid.Dock = DockStyle.Fill;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.AutoGenerateColumns = false;
            grid.AllowUserToAddRows = true;
            grid.AllowUserToDeleteRows = true;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 36;
            grid.DataSource = entries;
            grid.Columns.Add(CreateTextColumn(nameof(TranslationDictionaryEntry.SourceTerm), "原語", 300));
            grid.Columns.Add(CreateTextColumn(nameof(TranslationDictionaryEntry.TargetTerm), "翻訳", 300));
            grid.Columns.Add(CreateTextColumn(nameof(TranslationDictionaryEntry.Category), "カテゴリ", 180));

            Controls.Add(grid);
            Controls.Add(panelButtons);
            Controls.Add(panelTop);

            LoadEntries();
            ResumeLayout(false);
        }

        void LoadEntries()
        {
            entries.Clear();
            foreach (var entry in store.LoadEntries())
            {
                entries.Add(new TranslationDictionaryEntry
                {
                    SourceTerm = entry.SourceTerm,
                    TargetTerm = entry.TargetTerm,
                    Category = entry.Category,
                });
            }
        }

        DataGridViewTextBoxColumn CreateTextColumn(string dataPropertyName, string headerText, int width)
        {
            return new DataGridViewTextBoxColumn
            {
                DataPropertyName = dataPropertyName,
                HeaderText = headerText,
                Width = width,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            };
        }

        Button CreateButton(string text, EventHandler onClick, bool primary = false)
        {
            var button = new Button
            {
                Text = text,
                Width = primary ? 120 : (text.Length >= 5 ? 140 : 110),
                Height = 32,
                Margin = new Padding(0, 0, 10, 0),
                FlatStyle = FlatStyle.Flat,
            };
            button.FlatAppearance.BorderColor = primary
                ? Color.FromArgb(18, 93, 156)
                : Color.FromArgb(194, 210, 226);
            button.BackColor = primary
                ? Color.FromArgb(18, 93, 156)
                : Color.White;
            button.ForeColor = primary
                ? Color.White
                : Color.FromArgb(31, 55, 81);
            button.Click += onClick;
            return button;
        }

        void BtnAdd_Click(object sender, EventArgs e)
        {
            entries.Add(new TranslationDictionaryEntry());
            if (entries.Count > 0)
            {
                grid.CurrentCell = grid.Rows[entries.Count - 1].Cells[0];
                grid.BeginEdit(true);
            }
        }

        void BtnRemove_Click(object sender, EventArgs e)
        {
            if (grid.CurrentRow == null || grid.CurrentRow.IsNewRow)
            {
                return;
            }

            entries.RemoveAt(grid.CurrentRow.Index);
        }

        void BtnSave_Click(object sender, EventArgs e)
        {
            grid.EndEdit();

            var normalized = entries
                .Where(entry => entry != null)
                .Select(entry => new TranslationDictionaryEntry
                {
                    SourceTerm = (entry.SourceTerm ?? string.Empty).Trim(),
                    TargetTerm = (entry.TargetTerm ?? string.Empty).Trim(),
                    Category = (entry.Category ?? string.Empty).Trim(),
                })
                .Where(entry => !string.IsNullOrWhiteSpace(entry.SourceTerm) || !string.IsNullOrWhiteSpace(entry.TargetTerm))
                .ToList();

            if (normalized.Any(entry => string.IsNullOrWhiteSpace(entry.SourceTerm) || string.IsNullOrWhiteSpace(entry.TargetTerm)))
            {
                MessageBox.Show(this, "原語と翻訳は両方入力してください。", "辞書を保存", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (normalized.GroupBy(entry => entry.SourceTerm, StringComparer.Ordinal).Any(group => group.Count() > 1))
            {
                MessageBox.Show(this, "同じ原語の辞書行が重複しています。", "辞書を保存", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            store.SaveEntries(normalized);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
