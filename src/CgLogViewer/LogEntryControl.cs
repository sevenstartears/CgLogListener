using System;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CgLogViewer
{
    public class LogEntryControl : UserControl
    {
        readonly Label lblTime = new Label();
        readonly Label lblCategory = new Label();
        readonly Label lblCount = new Label();
        readonly LinkLabel lnkMessage = new LinkLabel();
        readonly Button btnCopy = new Button();
        readonly TableLayoutPanel layout = new TableLayoutPanel();
        Color accentColor = Color.FromArgb(20, 84, 140);
        string copyText = string.Empty;

        public LogEntryControl()
        {
            BackColor = Color.White;
            Margin = new Padding(0, 0, 0, 12);
            Padding = new Padding(14, 12, 14, 12);
            MinimumSize = new Size(280, 0);

            layout.ColumnCount = 4;
            layout.RowCount = 2;
            layout.Dock = DockStyle.Fill;
            layout.BackColor = Color.Transparent;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            lblTime.AutoSize = true;
            lblTime.Font = new Font("Yu Gothic UI Semibold", 10F, FontStyle.Bold);
            lblTime.Margin = new Padding(0, 0, 10, 8);

            lblCategory.AutoSize = true;
            lblCategory.Font = new Font("Yu Gothic UI", 8F, FontStyle.Bold);
            lblCategory.ForeColor = Color.White;
            lblCategory.Padding = new Padding(8, 3, 8, 3);
            lblCategory.Margin = new Padding(0, 0, 10, 8);

            lblCount.AutoSize = true;
            lblCount.Font = new Font("Yu Gothic UI", 8F, FontStyle.Bold);
            lblCount.Padding = new Padding(8, 3, 8, 3);
            lblCount.Margin = new Padding(0, 0, 10, 8);
            lblCount.Visible = false;

            btnCopy.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btnCopy.AutoSize = true;
            btnCopy.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnCopy.MinimumSize = new Size(82, 32);
            btnCopy.FlatStyle = FlatStyle.Flat;
            btnCopy.FlatAppearance.BorderColor = Color.FromArgb(187, 214, 238);
            btnCopy.FlatAppearance.MouseOverBackColor = Color.FromArgb(239, 248, 255);
            btnCopy.BackColor = Color.FromArgb(247, 251, 255);
            btnCopy.ForeColor = Color.FromArgb(31, 55, 81);
            btnCopy.Font = new Font("Yu Gothic UI", 8.5F, FontStyle.Regular);
            btnCopy.Margin = new Padding(0, 0, 0, 8);
            btnCopy.Padding = new Padding(12, 4, 12, 4);
            btnCopy.Text = "コピー";
            btnCopy.UseVisualStyleBackColor = false;
            btnCopy.Click += BtnCopy_Click;

            lnkMessage.AutoSize = true;
            lnkMessage.Dock = DockStyle.Fill;
            lnkMessage.Font = new Font("MingLiU", 10.5F, FontStyle.Regular, GraphicsUnit.Point, 136);
            lnkMessage.LinkBehavior = LinkBehavior.HoverUnderline;
            lnkMessage.LinkColor = Color.FromArgb(0, 102, 182);
            lnkMessage.ActiveLinkColor = Color.FromArgb(0, 71, 140);
            lnkMessage.VisitedLinkColor = Color.FromArgb(0, 102, 182);
            lnkMessage.ForeColor = Color.FromArgb(41, 47, 54);
            lnkMessage.Margin = new Padding(0);
            lnkMessage.MaximumSize = new Size(640, 0);
            lnkMessage.UseCompatibleTextRendering = true;
            lnkMessage.LinkClicked += LnkMessage_LinkClicked;

            layout.Controls.Add(lblTime, 0, 0);
            layout.Controls.Add(lblCategory, 1, 0);
            layout.Controls.Add(lblCount, 2, 0);
            layout.Controls.Add(btnCopy, 3, 0);
            layout.Controls.Add(lnkMessage, 0, 1);
            layout.SetColumnSpan(lnkMessage, 4);

            Controls.Add(layout);
            SizeChanged += LogEntryControl_SizeChanged;
        }

        public void UpdateEntry(DateTime timestamp, string message, int duplicateCount, LogCategory category)
        {
            copyText = $"{timestamp:HH:mm:ss} {message}";
            lblTime.Text = timestamp.ToString("HH:mm:ss");
            lblCategory.Text = LogCategoryPalette.GetLabel(category);
            lblCount.Text = $"x{duplicateCount}";
            lblCount.Visible = duplicateCount > 1;
            lnkMessage.Text = message;
            lnkMessage.Links.Clear();

            foreach (Match match in Regex.Matches(message, @"https?://[^\s]+"))
            {
                lnkMessage.Links.Add(match.Index, match.Length, match.Value);
            }

            ApplyCategoryStyle(category);
            UpdateMessageWidth();
        }

        void ApplyCategoryStyle(LogCategory category)
        {
            accentColor = LogCategoryPalette.GetAccentColor(category);
            BackColor = LogCategoryPalette.GetSurfaceColor(category);
            lblTime.ForeColor = accentColor;
            lblCategory.BackColor = accentColor;
            lblCount.BackColor = ControlPaint.LightLight(accentColor);
            lblCount.ForeColor = accentColor;
            btnCopy.FlatAppearance.BorderColor = ControlPaint.Light(accentColor);
            btnCopy.BackColor = ControlPaint.LightLight(accentColor);
            btnCopy.ForeColor = accentColor;
            Invalidate();
        }

        void BtnCopy_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(copyText))
            {
                Clipboard.SetText(copyText);
            }
        }

        void LnkMessage_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Link.LinkData.ToString()) { UseShellExecute = true });
            }
            catch
            {
                // ignored
            }
        }

        void LogEntryControl_SizeChanged(object sender, EventArgs e)
        {
            UpdateMessageWidth();
        }

        void UpdateMessageWidth()
        {
            var width = Math.Max(220, Width - Padding.Horizontal - 12);
            lnkMessage.MaximumSize = new Size(width, 0);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using (var accentBrush = new SolidBrush(accentColor))
            using (var borderPen = new Pen(ControlPaint.Light(accentColor)))
            {
                var borderRect = ClientRectangle;
                borderRect.Width -= 1;
                borderRect.Height -= 1;
                e.Graphics.FillRectangle(accentBrush, 0, 0, 5, Height);
                e.Graphics.DrawRectangle(borderPen, borderRect);
            }
        }
    }
}
