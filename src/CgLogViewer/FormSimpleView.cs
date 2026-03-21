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
        readonly Button btnReturnFullView = new Button();
        readonly Button btnFoodTimer = new Button();
        readonly BufferedFlowLayoutPanel flowLogs = new BufferedFlowLayoutPanel();
        readonly Label lblEmptyState = new Label();

        public event EventHandler ReturnRequested;
        public event EventHandler FoodTimerRequested;
        public event EventHandler<BufferedFlowLayoutPanel.TranslateRequestedEventArgs> TranslateRequested;

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
            panelTop.Height = 64;
            panelTop.BackColor = Color.White;
            panelTop.Padding = new Padding(16, 14, 16, 14);
            panelTop.MouseDown += PanelTop_MouseDown;

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

        void FlowLogs_SizeChanged(object sender, EventArgs e)
        {
            flowLogs.RefreshLayoutMetrics();
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
