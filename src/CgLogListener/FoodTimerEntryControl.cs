using System;
using System.Drawing;
using System.Windows.Forms;

namespace CgLogListener
{
    public sealed class FoodTimerEntryControl : UserControl
    {
        const int HorizontalPadding = 16;
        const int TopPadding = 14;
        const int VerticalGap = 10;

        readonly Label lblCharacter = new Label();
        readonly Label lblRemaining = new Label();
        readonly Label lblDetail = new Label();
        readonly Panel panelGaugeTrack = new Panel();
        readonly Panel panelGaugeFill = new Panel();

        double currentRatio = 1d;

        public FoodTimerEntryControl()
        {
            BackColor = Color.White;
            Margin = new Padding(0, 0, 0, 12);
            Padding = new Padding(HorizontalPadding, TopPadding, HorizontalPadding, TopPadding);
            Height = 116;

            lblCharacter.AutoSize = false;
            lblCharacter.Font = new Font("Yu Gothic UI Semibold", 12F, FontStyle.Bold);
            lblCharacter.ForeColor = Color.FromArgb(33, 52, 72);
            lblCharacter.Location = new Point(HorizontalPadding, TopPadding);
            lblCharacter.Size = new Size(200, 28);
            lblCharacter.AutoEllipsis = true;

            lblRemaining.AutoSize = true;
            lblRemaining.Font = new Font("Yu Gothic UI Semibold", 16F, FontStyle.Bold);
            lblRemaining.ForeColor = Color.FromArgb(18, 93, 156);
            lblRemaining.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            panelGaugeTrack.BackColor = Color.FromArgb(230, 236, 244);
            panelGaugeTrack.Location = new Point(HorizontalPadding, 56);
            panelGaugeTrack.Size = new Size(300, 16);
            panelGaugeTrack.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            panelGaugeFill.BackColor = Color.FromArgb(30, 136, 229);
            panelGaugeFill.Location = new Point(0, 0);
            panelGaugeFill.Size = panelGaugeTrack.Size;
            panelGaugeTrack.Controls.Add(panelGaugeFill);

            lblDetail.AutoSize = true;
            lblDetail.Font = new Font("Yu Gothic UI", 9F, FontStyle.Regular);
            lblDetail.ForeColor = Color.FromArgb(91, 102, 114);
            lblDetail.Location = new Point(HorizontalPadding, 82);

            Controls.Add(lblCharacter);
            Controls.Add(lblRemaining);
            Controls.Add(panelGaugeTrack);
            Controls.Add(lblDetail);

            Resize += FoodTimerEntryControl_Resize;
        }

        public void UpdateEntry(FoodCooldownEntry entry, DateTime now)
        {
            var remaining = entry.AvailableAt - now;
            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }

            lblCharacter.Text = entry.CharacterName;
            lblRemaining.Text = remaining <= TimeSpan.Zero
                ? "食事OK"
                : remaining.ToString(@"mm\:ss");
            lblRemaining.ForeColor = remaining <= TimeSpan.Zero
                ? Color.FromArgb(46, 125, 50)
                : Color.FromArgb(18, 93, 156);
            lblDetail.Text = remaining <= TimeSpan.Zero
                ? $"食事できる時刻: {entry.AvailableAt:HH:mm:ss}"
                : $"食事できる時刻: {entry.AvailableAt:HH:mm:ss}";

            currentRatio = Math.Max(0d, Math.Min(1d, remaining.TotalSeconds / FoodCooldownTracker.CooldownDuration.TotalSeconds));
            panelGaugeFill.BackColor = remaining <= TimeSpan.Zero
                ? Color.FromArgb(76, 175, 80)
                : (remaining.TotalSeconds <= 30 ? Color.FromArgb(239, 108, 0) : Color.FromArgb(30, 136, 229));

            LayoutContent();
        }

        void FoodTimerEntryControl_Resize(object sender, EventArgs e)
        {
            LayoutContent();
        }

        void UpdateGaugeFill()
        {
            int width = Math.Max(0, (int)Math.Round(panelGaugeTrack.Width * currentRatio));
            panelGaugeFill.Width = width;
            panelGaugeFill.Height = panelGaugeTrack.Height;
        }

        void LayoutContent()
        {
            int remainingX = Math.Max(HorizontalPadding, ClientSize.Width - lblRemaining.Width - HorizontalPadding);
            lblRemaining.Location = new Point(remainingX, TopPadding - 2);

            int characterWidth = Math.Max(120, lblRemaining.Left - (HorizontalPadding * 2));
            lblCharacter.Width = characterWidth;

            int topSectionBottom = Math.Max(lblCharacter.Bottom, lblRemaining.Bottom);
            int gaugeTop = topSectionBottom + VerticalGap;
            panelGaugeTrack.Location = new Point(HorizontalPadding, gaugeTop);
            panelGaugeTrack.Width = Math.Max(120, ClientSize.Width - (HorizontalPadding * 2));

            int detailTop = panelGaugeTrack.Bottom + VerticalGap;
            lblDetail.Location = new Point(HorizontalPadding, detailTop);

            Height = lblDetail.Bottom + TopPadding;
            UpdateGaugeFill();
        }
    }
}
