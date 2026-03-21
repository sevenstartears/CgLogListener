using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CgLogViewer
{
    public sealed class FormSimpleView : Form
    {
        const int WmNchittest = 0x84;
        const int WmNclbuttondown = 0xA1;
        const int HtClient = 1;
        const int HtCaption = 2;
        const int HtLeft = 10;
        const int HtRight = 11;
        const int HtTop = 12;
        const int HtTopLeft = 13;
        const int HtTopRight = 14;
        const int HtBottom = 15;
        const int HtBottomLeft = 16;
        const int HtBottomRight = 17;
        const int ResizeBorderSize = 6;

        readonly Panel panelTop = new Panel();
        readonly FlowLayoutPanel panelAdjustments = new FlowLayoutPanel();
        readonly Label lblRowHeight = new Label();
        readonly NumericUpDown numRowHeight = new NumericUpDown();
        readonly Label lblFontSize = new Label();
        readonly NumericUpDown numFontSize = new NumericUpDown();
        readonly Button btnReturnFullView = new Button();
        readonly Button btnFoodTimer = new Button();
        readonly BufferedFlowLayoutPanel flowLogs = new BufferedFlowLayoutPanel();
        readonly Label lblEmptyState = new Label();
        bool suppressPreferenceEvents;

        public event EventHandler ReturnRequested;
        public event EventHandler FoodTimerRequested;
        public event EventHandler<BufferedFlowLayoutPanel.TranslateRequestedEventArgs> TranslateRequested;
        public event EventHandler PreferencesChanged;

        public FormSimpleView()
        {
            SuspendLayout();

            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(243, 246, 251);
            ClientSize = new Size(860, 620);
            MinimumSize = new Size(520, 360);
            Font = new Font("Yu Gothic UI", 9F, FontStyle.Regular);
            FormBorderStyle = FormBorderStyle.None;
            Padding = new Padding(ResizeBorderSize);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "CgLogViewer - シンプルビュー";

            panelTop.Dock = DockStyle.Top;
            panelTop.Height = 72;
            panelTop.BackColor = Color.White;
            panelTop.Padding = new Padding(14, 12, 14, 12);
            panelTop.MouseDown += PanelTop_MouseDown;

            panelAdjustments.Dock = DockStyle.Left;
            panelAdjustments.AutoSize = true;
            panelAdjustments.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panelAdjustments.WrapContents = false;
            panelAdjustments.FlowDirection = FlowDirection.LeftToRight;
            panelAdjustments.BackColor = Color.Transparent;
            panelAdjustments.Padding = new Padding(0, 5, 0, 0);

            lblRowHeight.AutoSize = true;
            lblRowHeight.Margin = new Padding(0, 8, 8, 0);
            lblRowHeight.ForeColor = Color.FromArgb(63, 74, 86);
            lblRowHeight.Text = "カード高さ";

            numRowHeight.Width = 64;
            numRowHeight.Minimum = 28;
            numRowHeight.Maximum = 88;
            numRowHeight.Value = flowLogs.SimpleRowHeight;
            numRowHeight.Margin = new Padding(0, 3, 14, 0);
            numRowHeight.TextAlign = HorizontalAlignment.Center;
            numRowHeight.ValueChanged += NumRowHeight_ValueChanged;

            lblFontSize.AutoSize = true;
            lblFontSize.Margin = new Padding(0, 8, 8, 0);
            lblFontSize.ForeColor = Color.FromArgb(63, 74, 86);
            lblFontSize.Text = "文字サイズ";

            numFontSize.Width = 64;
            numFontSize.Minimum = 8;
            numFontSize.Maximum = 18;
            numFontSize.DecimalPlaces = 1;
            numFontSize.Increment = 0.5M;
            numFontSize.Value = (decimal)flowLogs.SimpleFontSize;
            numFontSize.Margin = new Padding(0, 3, 0, 0);
            numFontSize.TextAlign = HorizontalAlignment.Center;
            numFontSize.ValueChanged += NumFontSize_ValueChanged;

            panelAdjustments.Controls.Add(lblRowHeight);
            panelAdjustments.Controls.Add(numRowHeight);
            panelAdjustments.Controls.Add(lblFontSize);
            panelAdjustments.Controls.Add(numFontSize);

            btnReturnFullView.Dock = DockStyle.Right;
            btnReturnFullView.Width = 168;
            btnReturnFullView.Text = "フルビューに戻る";
            btnReturnFullView.FlatStyle = FlatStyle.Flat;
            btnReturnFullView.BackColor = Color.FromArgb(18, 93, 156);
            btnReturnFullView.ForeColor = Color.White;
            btnReturnFullView.FlatAppearance.BorderColor = Color.FromArgb(18, 93, 156);
            btnReturnFullView.Click += BtnReturnFullView_Click;

            btnFoodTimer.Dock = DockStyle.Right;
            btnFoodTimer.Width = 154;
            btnFoodTimer.Text = "お食事タイマー";
            btnFoodTimer.FlatStyle = FlatStyle.Flat;
            btnFoodTimer.BackColor = Color.FromArgb(248, 251, 255);
            btnFoodTimer.ForeColor = Color.FromArgb(31, 55, 81);
            btnFoodTimer.FlatAppearance.BorderColor = Color.FromArgb(194, 210, 226);
            btnFoodTimer.Margin = new Padding(0, 0, 10, 0);
            btnFoodTimer.Click += BtnFoodTimer_Click;

            panelTop.Controls.Add(panelAdjustments);
            panelTop.Controls.Add(btnFoodTimer);
            panelTop.Controls.Add(btnReturnFullView);

            var panelLogHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(243, 246, 251),
                Padding = new Padding(12, 8, 12, 12),
            };

            flowLogs.Dock = DockStyle.Fill;
            flowLogs.ViewMode = LogViewMode.Simple;
            flowLogs.TranslateRequested += FlowLogs_TranslateRequested;
            flowLogs.SizeChanged += FlowLogs_SizeChanged;

            lblEmptyState.AutoSize = true;
            lblEmptyState.BackColor = Color.Transparent;
            lblEmptyState.Font = new Font("Yu Gothic UI", 11F, FontStyle.Regular);
            lblEmptyState.ForeColor = Color.FromArgb(91, 102, 114);
            lblEmptyState.Location = new Point(16, 16);
            lblEmptyState.Text = "表示できるログがまだありません。";

            panelLogHost.Controls.Add(lblEmptyState);
            panelLogHost.Controls.Add(flowLogs);

            Controls.Add(panelLogHost);
            Controls.Add(panelTop);

            LocationChanged += FormSimpleView_PreferencesChanged;
            SizeChanged += FormSimpleView_PreferencesChanged;

            ResumeLayout(false);
        }

        [DllImport("user32.dll")]
        static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public bool TranslationConfigured
        {
            get { return flowLogs.TranslationConfigured; }
            set { flowLogs.TranslationConfigured = value; }
        }

        public int SimpleRowHeight
        {
            get { return flowLogs.SimpleRowHeight; }
            set
            {
                int next = Math.Max((int)numRowHeight.Minimum, Math.Min((int)numRowHeight.Maximum, value));
                suppressPreferenceEvents = true;
                try
                {
                    numRowHeight.Value = next;
                    flowLogs.SimpleRowHeight = next;
                }
                finally
                {
                    suppressPreferenceEvents = false;
                }
            }
        }

        public float SimpleFontSize
        {
            get { return flowLogs.SimpleFontSize; }
            set
            {
                decimal next = Math.Max(numFontSize.Minimum, Math.Min(numFontSize.Maximum, (decimal)value));
                suppressPreferenceEvents = true;
                try
                {
                    numFontSize.Value = next;
                    flowLogs.SimpleFontSize = (float)next;
                }
                finally
                {
                    suppressPreferenceEvents = false;
                }
            }
        }

        public void SetEntries(IReadOnlyList<DisplayedLogGroup> entries, bool scrollToLatest)
        {
            flowLogs.SetEntries(entries);
            UpdateEmptyState(entries);
            if (scrollToLatest && entries != null && entries.Count > 0)
            {
                flowLogs.ScrollToEnd();
            }
        }

        public void AppendEntry(DisplayedLogGroup entry, bool scrollToLatest)
        {
            flowLogs.AppendEntry(entry);
            lblEmptyState.Visible = false;
            if (scrollToLatest)
            {
                flowLogs.ScrollToEnd();
            }
        }

        public void RefreshEntry(DisplayedLogGroup entry)
        {
            flowLogs.RefreshEntry(entry);
        }

        public void ScrollToEnd()
        {
            flowLogs.ScrollToEnd();
        }

        public void ScrollToEndDeferred()
        {
            if (!IsHandleCreated)
            {
                return;
            }

            BeginInvoke((Action)(() => flowLogs.ScrollToEnd()));
        }

        public void RefreshLayoutMetrics()
        {
            flowLogs.RefreshLayoutMetrics();
        }

        public void ApplySavedBounds(Rectangle bounds)
        {
            suppressPreferenceEvents = true;
            try
            {
                StartPosition = FormStartPosition.Manual;
                Bounds = bounds;
            }
            finally
            {
                suppressPreferenceEvents = false;
            }
        }

        void FlowLogs_SizeChanged(object sender, EventArgs e)
        {
            flowLogs.RefreshLayoutMetrics();
        }

        void NumRowHeight_ValueChanged(object sender, EventArgs e)
        {
            flowLogs.SimpleRowHeight = Decimal.ToInt32(numRowHeight.Value);
            RaisePreferencesChanged();
        }

        void NumFontSize_ValueChanged(object sender, EventArgs e)
        {
            flowLogs.SimpleFontSize = (float)numFontSize.Value;
            RaisePreferencesChanged();
        }

        void FormSimpleView_PreferencesChanged(object sender, EventArgs e)
        {
            RaisePreferencesChanged();
        }

        void BtnReturnFullView_Click(object sender, EventArgs e)
        {
            var handler = ReturnRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        void BtnFoodTimer_Click(object sender, EventArgs e)
        {
            var handler = FoodTimerRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        void PanelTop_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            ReleaseCapture();
            SendMessage(Handle, WmNclbuttondown, (IntPtr)HtCaption, IntPtr.Zero);
        }

        void FlowLogs_TranslateRequested(object sender, BufferedFlowLayoutPanel.TranslateRequestedEventArgs e)
        {
            var handler = TranslateRequested;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        void UpdateEmptyState(IReadOnlyList<DisplayedLogGroup> entries)
        {
            lblEmptyState.Visible = entries == null || entries.Count == 0;
        }

        void RaisePreferencesChanged()
        {
            if (suppressPreferenceEvents || !IsHandleCreated || WindowState != FormWindowState.Normal)
            {
                return;
            }

            var handler = PreferencesChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmNchittest)
            {
                base.WndProc(ref m);
                if ((int)m.Result == HtClient)
                {
                    var screenPoint = new Point((short)((int)m.LParam & 0xFFFF), (short)(((int)m.LParam >> 16) & 0xFFFF));
                    var clientPoint = PointToClient(screenPoint);
                    m.Result = (IntPtr)HitTestResizeBorder(clientPoint);
                    return;
                }

                return;
            }

            base.WndProc(ref m);
        }

        int HitTestResizeBorder(Point point)
        {
            bool left = point.X <= ResizeBorderSize;
            bool right = point.X >= ClientSize.Width - ResizeBorderSize;
            bool top = point.Y <= ResizeBorderSize;
            bool bottom = point.Y >= ClientSize.Height - ResizeBorderSize;

            if (left && top)
            {
                return HtTopLeft;
            }

            if (right && top)
            {
                return HtTopRight;
            }

            if (left && bottom)
            {
                return HtBottomLeft;
            }

            if (right && bottom)
            {
                return HtBottomRight;
            }

            if (left)
            {
                return HtLeft;
            }

            if (right)
            {
                return HtRight;
            }

            if (top)
            {
                return HtTop;
            }

            if (bottom)
            {
                return HtBottom;
            }

            return HtClient;
        }
    }
}
