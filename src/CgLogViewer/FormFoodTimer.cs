using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CgLogViewer
{
    public sealed class FormFoodTimer : Form
    {
        readonly FoodCooldownTracker tracker;
        readonly Timer refreshTimer = new Timer();
        readonly FlowLayoutPanel flowEntries = new FlowLayoutPanel();
        readonly Label lblEmpty = new Label();
        readonly Dictionary<string, FoodTimerEntryControl> rowControls = new Dictionary<string, FoodTimerEntryControl>(StringComparer.OrdinalIgnoreCase);

        public FormFoodTimer(FoodCooldownTracker tracker)
        {
            this.tracker = tracker;

            SuspendLayout();

            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(242, 246, 251);
            ClientSize = new Size(460, 560);
            MinimumSize = new Size(460, 420);
            Font = new Font("Yu Gothic UI", 9F, FontStyle.Regular);
            Icon = Resource.icon;
            StartPosition = FormStartPosition.CenterParent;
            Text = "お食事タイマー";

            var panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 112,
                BackColor = Color.FromArgb(18, 93, 156),
                Padding = new Padding(20, 14, 20, 16),
            };

            var lblTitle = new Label
            {
                AutoSize = true,
                Font = new Font("Yu Gothic UI Semibold", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 12),
                Text = "お食事タイマー",
            };

            var lblSubtitle = new Label
            {
                AutoSize = false,
                Font = new Font("Yu Gothic UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(223, 238, 255),
                Location = new Point(22, 54),
                Size = new Size(396, 28),
                Text = "料理を食べたキャラごとの3分クールダウンを表示",
            };

            panelHeader.Controls.Add(lblTitle);
            panelHeader.Controls.Add(lblSubtitle);

            flowEntries.Dock = DockStyle.Fill;
            flowEntries.FlowDirection = FlowDirection.TopDown;
            flowEntries.WrapContents = false;
            flowEntries.AutoScroll = true;
            flowEntries.Padding = new Padding(18, 18, 18, 18);
            flowEntries.BackColor = Color.FromArgb(242, 246, 251);

            lblEmpty.AutoSize = true;
            lblEmpty.Font = new Font("Yu Gothic UI", 11F, FontStyle.Regular);
            lblEmpty.ForeColor = Color.FromArgb(91, 102, 114);
            lblEmpty.Location = new Point(22, 24);
            lblEmpty.Text = "まだお食事ログは検出されていません。";

            flowEntries.Controls.Add(lblEmpty);

            Controls.Add(flowEntries);
            Controls.Add(panelHeader);

            refreshTimer.Interval = 500;
            refreshTimer.Tick += RefreshTimer_Tick;

            tracker.Changed += Tracker_Changed;

            ResumeLayout(false);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            refreshTimer.Start();
            RefreshEntries();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            refreshTimer.Stop();
            tracker.Changed -= Tracker_Changed;
            refreshTimer.Tick -= RefreshTimer_Tick;
            base.OnFormClosed(e);
        }

        void Tracker_Changed(object sender, EventArgs e)
        {
            if (!IsHandleCreated || IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke((Action)RefreshEntries);
                return;
            }

            RefreshEntries();
        }

        void RefreshTimer_Tick(object sender, EventArgs e)
        {
            RefreshEntries();
        }

        void RefreshEntries()
        {
            var snapshot = tracker.GetSnapshot(DateTime.Now);
            var snapshotKeys = new HashSet<string>(snapshot.Select(entry => entry.CharacterName), StringComparer.OrdinalIgnoreCase);

            foreach (var key in rowControls.Keys.Where(key => !snapshotKeys.Contains(key)).ToList())
            {
                var control = rowControls[key];
                flowEntries.Controls.Remove(control);
                control.Dispose();
                rowControls.Remove(key);
            }

            flowEntries.SuspendLayout();
            flowEntries.Controls.Clear();

            if (snapshot.Count == 0)
            {
                lblEmpty.Visible = true;
                flowEntries.Controls.Add(lblEmpty);
                flowEntries.ResumeLayout();
                return;
            }

            lblEmpty.Visible = false;

            foreach (var entry in snapshot)
            {
                if (!rowControls.TryGetValue(entry.CharacterName, out var control))
                {
                    control = new FoodTimerEntryControl();
                    rowControls[entry.CharacterName] = control;
                }

                int availableWidth = flowEntries.ClientSize.Width
                    - flowEntries.Padding.Horizontal
                    - SystemInformation.VerticalScrollBarWidth
                    - 8;
                control.Width = Math.Max(320, availableWidth);
                control.UpdateEntry(entry, DateTime.Now);
                flowEntries.Controls.Add(control);
            }

            flowEntries.ResumeLayout();
        }
    }
}
