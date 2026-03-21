using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CgLogViewer
{
    public enum LogViewMode
    {
        Full,
        Simple
    }

    public class BufferedFlowLayoutPanel : ScrollableControl
    {
        static readonly Regex UrlRegex = new Regex(@"(?:https?://|www\.)[^\s]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly char[] WrapCharacters = { ' ', '\t', '　', '、', '。', '，', '．', '！', '？', ',', '.', '!', '?', '/', ')', ']' };

        const int CardGap = 12;
        const int SimpleCardGap = 5;
        const int AccentWidth = 5;
        const int CardPaddingLeft = 14;
        const int CardPaddingTop = 12;
        const int CardPaddingRight = 14;
        const int CardPaddingBottom = 12;
        const int HeaderBottomGap = 8;
        const int HeaderItemGap = 10;
        const int HeaderButtonGap = 8;
        const int TranslateButtonWidth = 92;
        const int CopyButtonWidth = 84;
        const int CopyButtonHeight = 32;
        const int ChipHeight = 23;
        const int ChipHorizontalPadding = 8;
        const int ContentBottomPadding = 8;
        const int SimpleRowHeight = 40;
        const int SimpleMessageRightPadding = 6;
        const int SimpleExpandedTopPadding = 5;
        const int SimpleExpandedBottomPadding = 5;

        readonly Font timeFont = new Font("Yu Gothic UI Semibold", 10F, FontStyle.Bold);
        readonly Font categoryFont = new Font("Yu Gothic UI", 8F, FontStyle.Bold);
        readonly Font countFont = new Font("Yu Gothic UI", 8F, FontStyle.Bold);
        readonly Font messageFont = new Font("MingLiU", 11F, FontStyle.Bold, GraphicsUnit.Point, 136);
        readonly Font messageLinkFont = new Font("MingLiU", 11F, FontStyle.Bold | FontStyle.Underline, GraphicsUnit.Point, 136);
        readonly Font copyFont = new Font("Yu Gothic UI", 8.5F, FontStyle.Regular);
        readonly Font simpleMessageFont = new Font("MingLiU", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 136);
        readonly List<DisplayedLogGroup> entries = new List<DisplayedLogGroup>();
        readonly List<RowLayout> rowLayouts = new List<RowLayout>();
        readonly Dictionary<DisplayedLogGroup, int> rowIndices = new Dictionary<DisplayedLogGroup, int>();
        readonly List<InteractiveRegion> interactiveRegions = new List<InteractiveRegion>();
        readonly Size infiniteTextBox = new Size(int.MaxValue, int.MaxValue);
        readonly ContextMenuStrip rowMenu = new ContextMenuStrip();
        readonly ToolStripMenuItem menuTranslate = new ToolStripMenuItem();
        readonly ToolStripMenuItem menuCopy = new ToolStripMenuItem();

        HoverState hoverState = HoverState.None;
        bool translationConfigured;
        LogViewMode viewMode = LogViewMode.Full;
        DisplayedLogGroup contextMenuEntry;

        public event EventHandler<TranslateRequestedEventArgs> TranslateRequested;

        public BufferedFlowLayoutPanel()
        {
            AutoScroll = true;
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.FromArgb(243, 246, 251);

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);

            rowMenu.ShowImageMargin = false;
            rowMenu.Items.AddRange(new ToolStripItem[] { menuTranslate, menuCopy });
            menuTranslate.Click += MenuTranslate_Click;
            menuCopy.Text = "コピー";
            menuCopy.Click += MenuCopy_Click;
        }

        public LogViewMode ViewMode
        {
            get { return viewMode; }
            set
            {
                if (viewMode == value)
                {
                    return;
                }

                viewMode = value;
                hoverState = HoverState.None;
                RefreshLayoutMetrics();
            }
        }

        public void SetEntries(IReadOnlyList<DisplayedLogGroup> newEntries)
        {
            bool stickToBottom = IsNearBottom();
            int scrollOffset = GetScrollOffset();

            entries.Clear();
            if (newEntries != null)
            {
                entries.AddRange(newEntries);
            }

            RebuildLayouts();
            RestoreScrollOffset(stickToBottom ? int.MaxValue : scrollOffset);
            Invalidate();
        }

        public void AppendEntry(DisplayedLogGroup entry)
        {
            if (entry == null)
            {
                return;
            }

            bool stickToBottom = IsNearBottom();

            entries.Add(entry);
            AppendLayout(entry);
            RestoreScrollOffset(stickToBottom ? int.MaxValue : GetScrollOffset());
            Invalidate();
        }

        public bool TranslationConfigured
        {
            get { return translationConfigured; }
            set
            {
                if (translationConfigured == value)
                {
                    return;
                }

                translationConfigured = value;
                Invalidate();
            }
        }

        public void RefreshEntry(DisplayedLogGroup entry)
        {
            if (!rowIndices.ContainsKey(entry))
            {
                return;
            }

            bool stickToBottom = IsNearBottom();
            int scrollOffset = GetScrollOffset();

            RebuildLayouts();
            RestoreScrollOffset(stickToBottom ? int.MaxValue : scrollOffset);
            Invalidate();
        }

        public void RefreshLayoutMetrics()
        {
            bool stickToBottom = IsNearBottom();
            int scrollOffset = GetScrollOffset();

            RebuildLayouts();
            RestoreScrollOffset(stickToBottom ? int.MaxValue : scrollOffset);
            Invalidate();
        }

        public void ScrollToEnd()
        {
            RestoreScrollOffset(int.MaxValue);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timeFont.Dispose();
                categoryFont.Dispose();
                countFont.Dispose();
                messageFont.Dispose();
                messageLinkFont.Dispose();
                copyFont.Dispose();
                simpleMessageFont.Dispose();
                rowMenu.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.Clear(BackColor);

            interactiveRegions.Clear();

            if (rowLayouts.Count == 0)
            {
                return;
            }

            int visibleTop = GetScrollOffset();
            int visibleBottom = visibleTop + ClientSize.Height;

            foreach (var row in rowLayouts)
            {
                if (row.Bounds.Bottom < visibleTop)
                {
                    continue;
                }

                if (row.Bounds.Top > visibleBottom)
                {
                    break;
                }

                if (viewMode == LogViewMode.Simple)
                {
                    DrawSimpleRow(e.Graphics, row);
                }
                else
                {
                    DrawRow(e.Graphics, row);
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (viewMode == LogViewMode.Simple)
            {
                Cursor = Cursors.Default;
                return;
            }

            var region = HitTest(e.Location);
            var nextState = region == null ? HoverState.None : new HoverState(region.Kind, region.Entry, region.Value);

            if (!hoverState.Equals(nextState))
            {
                hoverState = nextState;
                Cursor = region == null ? Cursors.Default : Cursors.Hand;
                Invalidate();
            }
            else if (region != null)
            {
                Cursor = Cursors.Hand;
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (!hoverState.IsEmpty)
            {
                hoverState = HoverState.None;
                Invalidate();
            }

            Cursor = Cursors.Default;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (e.Button == MouseButtons.Right && viewMode == LogViewMode.Simple)
            {
                ShowRowMenu(e.Location);
                return;
            }

            if (e.Button == MouseButtons.Left && viewMode == LogViewMode.Simple)
            {
                var row = FindRowAt(e.Location);
                if (row != null && CanExpandSimpleRow(row))
                {
                    row.Entry.ToggleSimpleExpanded();
                    RefreshEntry(row.Entry);
                }

                return;
            }

            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            var region = HitTest(e.Location);
            if (region == null)
            {
                return;
            }

            switch (region.Kind)
            {
                case InteractiveKind.Copy:
                    TryCopy(region.Entry.DisplayLine);
                    break;
                case InteractiveKind.Link:
                    TryOpenUrl(region.Value);
                    break;
                case InteractiveKind.Translate:
                    OnTranslateRequested(region.Entry);
                    break;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            Invalidate();
        }

        void MenuTranslate_Click(object sender, EventArgs e)
        {
            if (contextMenuEntry == null || contextMenuEntry.TranslationState == TranslationState.InProgress)
            {
                return;
            }

            OnTranslateRequested(contextMenuEntry);
        }

        void MenuCopy_Click(object sender, EventArgs e)
        {
            if (contextMenuEntry == null)
            {
                return;
            }

            TryCopy(contextMenuEntry.DisplayLine);
        }

        void ShowRowMenu(Point location)
        {
            var row = FindRowAt(location);
            if (row == null)
            {
                return;
            }

            contextMenuEntry = row.Entry;
            menuTranslate.Text = GetTranslateButtonText(row.Entry);
            menuTranslate.Enabled = row.Entry.TranslationState != TranslationState.InProgress;
            rowMenu.Show(this, location);
        }

        void RebuildLayouts()
        {
            rowLayouts.Clear();
            rowIndices.Clear();

            int y = 0;
            foreach (var entry in entries)
            {
                var layout = CreateRowLayout(entry, y);
                rowIndices[entry] = rowLayouts.Count;
                rowLayouts.Add(layout);
                y = layout.Bounds.Bottom + GetCardGap();
            }

            AutoScrollMinSize = new Size(0, Math.Max(0, y - GetCardGap() + ContentBottomPadding));
        }

        void AppendLayout(DisplayedLogGroup entry)
        {
            int nextY = rowLayouts.Count == 0 ? 0 : rowLayouts[rowLayouts.Count - 1].Bounds.Bottom + GetCardGap();
            var layout = CreateRowLayout(entry, nextY);
            rowIndices[entry] = rowLayouts.Count;
            rowLayouts.Add(layout);
            AutoScrollMinSize = new Size(0, Math.Max(0, layout.Bounds.Bottom + ContentBottomPadding));
        }

        int GetCardGap()
        {
            return viewMode == LogViewMode.Simple ? SimpleCardGap : CardGap;
        }

        RowLayout CreateRowLayout(DisplayedLogGroup entry, int y)
        {
            int cardWidth = GetCardWidth();
            if (viewMode == LogViewMode.Simple)
            {
                int simpleMessageWidth = Math.Max(220, cardWidth - AccentWidth - CardPaddingLeft - CardPaddingRight - SimpleMessageRightPadding);
                int simpleHeight = SimpleRowHeight;
                if (entry.IsExpandedInSimpleView && SimpleMessageNeedsExpansion(entry, simpleMessageWidth))
                {
                    int simpleLineCount = Math.Max(1, BuildWrappedLines(NormalizeSingleLine(entry.VisibleMessage), simpleMessageWidth, simpleMessageFont).Count);
                    simpleHeight = Math.Max(SimpleRowHeight, SimpleExpandedTopPadding + (simpleLineCount * GetSimpleMessageLineHeight()) + SimpleExpandedBottomPadding);
                }

                return new RowLayout(entry, new Rectangle(0, y, cardWidth, simpleHeight), simpleMessageWidth, 0);
            }

            int messageWidth = Math.Max(220, cardWidth - AccentWidth - CardPaddingLeft - CardPaddingRight);
            int lineCount = Math.Max(1, BuildWrappedLines(entry.VisibleMessage, messageWidth).Count);
            int messageHeight = lineCount * GetMessageLineHeight();
            int headerHeight = Math.Max(CopyButtonHeight, Math.Max(GetTextHeight(timeFont), ChipHeight));
            int totalHeight = CardPaddingTop + headerHeight + HeaderBottomGap + messageHeight + CardPaddingBottom;

            return new RowLayout(entry, new Rectangle(0, y, cardWidth, totalHeight), messageWidth, headerHeight);
        }

        void DrawSimpleRow(Graphics graphics, RowLayout row)
        {
            var cardBounds = ToClientRectangle(row.Bounds);
            var accentColor = LogCategoryPalette.GetAccentColor(row.Entry.Category);
            var surfaceColor = LogCategoryPalette.GetSurfaceColor(row.Entry.Category);
            var borderColor = ControlPaint.Light(accentColor);
            var textColor = Color.FromArgb(41, 47, 54);

            using (var backBrush = new SolidBrush(surfaceColor))
            using (var accentBrush = new SolidBrush(accentColor))
            using (var borderPen = new Pen(borderColor))
            {
                var outline = cardBounds;
                outline.Width -= 1;
                outline.Height -= 1;

                graphics.FillRectangle(backBrush, cardBounds);
                graphics.FillRectangle(accentBrush, new Rectangle(cardBounds.Left, cardBounds.Top, AccentWidth, cardBounds.Height));
                graphics.DrawRectangle(borderPen, outline);

                var textRect = new Rectangle(
                    cardBounds.Left + AccentWidth + 8,
                    cardBounds.Top + (row.Entry.IsExpandedInSimpleView ? SimpleExpandedTopPadding : 7),
                    row.MessageWidth,
                    row.Entry.IsExpandedInSimpleView
                        ? cardBounds.Height - (SimpleExpandedTopPadding + SimpleExpandedBottomPadding)
                        : cardBounds.Height - 12);

                if (row.Entry.IsExpandedInSimpleView && CanExpandSimpleRow(row))
                {
                    DrawSimpleExpandedText(graphics, row, textRect, textColor);
                }
                else
                {
                    TextRenderer.DrawText(
                        graphics,
                        NormalizeSingleLine(row.Entry.VisibleMessage),
                        simpleMessageFont,
                        textRect,
                        textColor,
                        TextFormatFlags.NoPadding |
                        TextFormatFlags.NoPrefix |
                        TextFormatFlags.EndEllipsis |
                        TextFormatFlags.VerticalCenter |
                        TextFormatFlags.SingleLine);
                }
            }
        }

        void DrawSimpleExpandedText(Graphics graphics, RowLayout row, Rectangle bounds, Color color)
        {
            string message = NormalizeSingleLine(row.Entry.VisibleMessage);
            var lines = BuildWrappedLines(message, bounds.Width, simpleMessageFont);
            int lineHeight = GetSimpleMessageLineHeight();
            int y = bounds.Top;

            foreach (var line in lines)
            {
                string lineText = message.Substring(line.StartIndex, line.Length);
                var lineRect = new Rectangle(bounds.Left, y, bounds.Width, lineHeight);
                TextRenderer.DrawText(
                    graphics,
                    lineText,
                    simpleMessageFont,
                    lineRect,
                    color,
                    TextFormatFlags.NoPadding |
                    TextFormatFlags.NoPrefix |
                    TextFormatFlags.Left |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine);
                y += lineHeight;
            }
        }

        void DrawRow(Graphics graphics, RowLayout row)
        {
            var cardBounds = ToClientRectangle(row.Bounds);
            var accentColor = LogCategoryPalette.GetAccentColor(row.Entry.Category);
            var surfaceColor = LogCategoryPalette.GetSurfaceColor(row.Entry.Category);
            var borderColor = ControlPaint.Light(accentColor);
            var countBackColor = ControlPaint.LightLight(accentColor);
            var textColor = Color.FromArgb(41, 47, 54);
            var timeColor = accentColor;
            var categoryLabel = LogCategoryPalette.GetLabel(row.Entry.Category);

            using (var backBrush = new SolidBrush(surfaceColor))
            using (var accentBrush = new SolidBrush(accentColor))
            using (var borderPen = new Pen(borderColor))
            using (var countBrush = new SolidBrush(countBackColor))
            using (var copyBrush = new SolidBrush(countBackColor))
            using (var copyHoverBrush = new SolidBrush(ControlPaint.Light(accentColor)))
            using (var textBrush = new SolidBrush(textColor))
            {
                var outline = cardBounds;
                outline.Width -= 1;
                outline.Height -= 1;

                graphics.FillRectangle(backBrush, cardBounds);
                graphics.FillRectangle(accentBrush, new Rectangle(cardBounds.Left, cardBounds.Top, AccentWidth, cardBounds.Height));
                graphics.DrawRectangle(borderPen, outline);

                int contentLeft = cardBounds.Left + AccentWidth + CardPaddingLeft;
                int contentTop = cardBounds.Top + CardPaddingTop;

                var timeSize = MeasureText(row.Entry.DisplayTimestamp.ToString("HH:mm:ss"), timeFont);
                var timeRect = new Rectangle(contentLeft, contentTop + Math.Max(0, (row.HeaderHeight - timeSize.Height) / 2), timeSize.Width, timeSize.Height);
                DrawText(graphics, row.Entry.DisplayTimestamp.ToString("HH:mm:ss"), timeFont, timeRect, timeColor);

                int chipX = timeRect.Right + HeaderItemGap;
                var categoryRect = new Rectangle(chipX, contentTop + Math.Max(0, (row.HeaderHeight - ChipHeight) / 2), MeasureChipWidth(categoryLabel, categoryFont), ChipHeight);
                DrawFilledChip(graphics, categoryRect, accentColor, Color.White, categoryLabel, categoryFont);
                chipX = categoryRect.Right + HeaderItemGap;

                if (row.Entry.Count > 1)
                {
                    string countText = $"x{row.Entry.Count}";
                    var countRect = new Rectangle(chipX, contentTop + Math.Max(0, (row.HeaderHeight - ChipHeight) / 2), MeasureChipWidth(countText, countFont), ChipHeight);
                    DrawFilledChip(graphics, countRect, countBackColor, accentColor, countText, countFont);
                }

                var copyRect = new Rectangle(cardBounds.Right - CardPaddingRight - CopyButtonWidth, contentTop, CopyButtonWidth, CopyButtonHeight);
                var translateRect = new Rectangle(copyRect.Left - HeaderButtonGap - TranslateButtonWidth, contentTop, TranslateButtonWidth, CopyButtonHeight);
                bool translateHovered = hoverState.Matches(InteractiveKind.Translate, row.Entry, null);
                bool translateActive = row.Entry.IsShowingTranslation && row.Entry.CanToggleTranslation;
                string translateText = GetTranslateButtonText(row.Entry);
                var translateBaseColor = translationConfigured ? countBackColor : Color.FromArgb(245, 247, 250);
                var translateBackColor = translateActive
                    ? accentColor
                    : (translateHovered ? ControlPaint.Light(accentColor) : translateBaseColor);
                var translateTextColor = translateActive
                    ? Color.White
                    : (translationConfigured ? accentColor : Color.FromArgb(120, 130, 142));

                DrawButtonChip(
                    graphics,
                    translateRect,
                    translateBackColor,
                    borderColor,
                    translateTextColor,
                    translateText,
                    copyFont);
                interactiveRegions.Add(new InteractiveRegion(translateRect, InteractiveKind.Translate, row.Entry, null));

                bool copyHovered = hoverState.Matches(InteractiveKind.Copy, row.Entry, null);
                DrawButtonChip(
                    graphics,
                    copyRect,
                    copyHovered ? ControlPaint.Light(accentColor) : countBackColor,
                    borderColor,
                    accentColor,
                    "コピー",
                    copyFont);
                interactiveRegions.Add(new InteractiveRegion(copyRect, InteractiveKind.Copy, row.Entry, null));

                var messageRect = new Rectangle(contentLeft, contentTop + row.HeaderHeight + HeaderBottomGap, row.MessageWidth, row.Bounds.Height - CardPaddingTop - row.HeaderHeight - HeaderBottomGap - CardPaddingBottom);
                DrawMessage(graphics, row.Entry, messageRect, textBrush);
            }
        }

        void DrawMessage(Graphics graphics, DisplayedLogGroup entry, Rectangle bounds, Brush textBrush)
        {
            var lineHeight = GetMessageLineHeight();
            string visibleMessage = entry.VisibleMessage;
            var lines = BuildWrappedLines(visibleMessage, bounds.Width);
            var matches = UrlRegex.Matches(visibleMessage).Cast<Match>().ToList();
            int y = bounds.Top;

            foreach (var line in lines)
            {
                int x = bounds.Left;
                foreach (var fragment in BuildFragments(visibleMessage, line, matches))
                {
                    if (string.IsNullOrEmpty(fragment.Text))
                    {
                        continue;
                    }

                    var font = fragment.IsLink ? messageLinkFont : messageFont;
                    var color = fragment.IsLink ? Color.FromArgb(0, 102, 182) : Color.FromArgb(41, 47, 54);
                    var size = MeasureText(fragment.Text, font);
                    var drawRect = new Rectangle(x, y, size.Width, lineHeight);

                    DrawText(graphics, fragment.Text, font, drawRect, color);

                    if (fragment.IsLink)
                    {
                        interactiveRegions.Add(new InteractiveRegion(drawRect, InteractiveKind.Link, entry, fragment.Url));
                    }

                    x += size.Width;
                }

                y += lineHeight;
            }
        }

        IEnumerable<TextFragment> BuildFragments(string message, WrappedLine line, List<Match> matches)
        {
            int current = line.StartIndex;
            int lineEnd = line.StartIndex + line.Length;

            foreach (var match in matches)
            {
                int matchStart = match.Index;
                int matchEnd = match.Index + match.Length;
                if (matchEnd <= line.StartIndex || matchStart >= lineEnd)
                {
                    continue;
                }

                int textEnd = Math.Min(matchStart, lineEnd);
                if (current < textEnd)
                {
                    yield return new TextFragment(message.Substring(current, textEnd - current), false, null);
                    current = textEnd;
                }

                int linkStart = Math.Max(matchStart, line.StartIndex);
                int linkEnd = Math.Min(matchEnd, lineEnd);
                if (current < linkStart)
                {
                    yield return new TextFragment(message.Substring(current, linkStart - current), false, null);
                    current = linkStart;
                }

                if (current < linkEnd)
                {
                    yield return new TextFragment(message.Substring(current, linkEnd - current), true, match.Value);
                    current = linkEnd;
                }
            }

            if (current < lineEnd)
            {
                yield return new TextFragment(message.Substring(current, lineEnd - current), false, null);
            }
        }

        List<WrappedLine> BuildWrappedLines(string text, int maxWidth)
        {
            return BuildWrappedLines(text, maxWidth, messageFont);
        }

        List<WrappedLine> BuildWrappedLines(string text, int maxWidth, Font font)
        {
            var lines = new List<WrappedLine>();
            if (string.IsNullOrEmpty(text))
            {
                lines.Add(new WrappedLine(0, 0));
                return lines;
            }

            int index = 0;
            while (index < text.Length)
            {
                if (text[index] == '\r')
                {
                    index++;
                    continue;
                }

                if (text[index] == '\n')
                {
                    lines.Add(new WrappedLine(index, 0));
                    index++;
                    continue;
                }

                int rawLineEnd = index;
                while (rawLineEnd < text.Length && text[rawLineEnd] != '\r' && text[rawLineEnd] != '\n')
                {
                    rawLineEnd++;
                }

                while (index < rawLineEnd)
                {
                    int remainingLength = rawLineEnd - index;
                    int length = FindFittingLength(text, index, rawLineEnd, maxWidth, font);
                    int wrappedLength = length >= remainingLength
                        ? remainingLength
                        : AdjustWrapLength(text, index, length);
                    string lineText = text.Substring(index, wrappedLength).TrimEnd();

                    if (lineText.Length == 0 && wrappedLength > 0)
                    {
                        lineText = text.Substring(index, wrappedLength);
                    }

                    lines.Add(new WrappedLine(index, lineText.Length));
                    index += wrappedLength;

                    while (index < rawLineEnd && char.IsWhiteSpace(text[index]) && text[index] != '　')
                    {
                        index++;
                    }
                }

                if (index < text.Length && text[index] == '\r')
                {
                    index++;
                }

                if (index < text.Length && text[index] == '\n')
                {
                    index++;
                }
            }

            if (lines.Count == 0)
            {
                lines.Add(new WrappedLine(0, 0));
            }

            return lines;
        }

        int FindFittingLength(string text, int start, int lineEnd, int maxWidth, Font font)
        {
            int low = 1;
            int high = Math.Max(1, lineEnd - start);
            int best = 1;

            while (low <= high)
            {
                int mid = low + ((high - low) / 2);
                string candidate = text.Substring(start, mid);
                int width = MeasureText(candidate, font).Width;

                if (width <= maxWidth)
                {
                    best = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return best;
        }

        int AdjustWrapLength(string text, int start, int fittedLength)
        {
            if (fittedLength <= 1)
            {
                return 1;
            }

            for (int i = start + fittedLength - 1; i > start; i--)
            {
                if (WrapCharacters.Contains(text[i]))
                {
                    return (i - start) + 1;
                }
            }

            return fittedLength;
        }

        string NormalizeSingleLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return text.Replace("\r", " ").Replace("\n", " ").Trim();
        }

        bool CanExpandSimpleRow(RowLayout row)
        {
            return row != null && SimpleMessageNeedsExpansion(row.Entry, row.MessageWidth);
        }

        bool SimpleMessageNeedsExpansion(DisplayedLogGroup entry, int width)
        {
            return MeasureText(NormalizeSingleLine(entry.VisibleMessage), simpleMessageFont).Width > width;
        }

        string GetTranslateButtonText(DisplayedLogGroup entry)
        {
            if (entry.TranslationState == TranslationState.InProgress)
            {
                return "翻訳中";
            }

            if (entry.TranslationState == TranslationState.Failed)
            {
                return "再試行";
            }

            if (entry.CanToggleTranslation)
            {
                return entry.IsShowingTranslation ? "原文" : "翻訳";
            }

            return translationConfigured ? "翻訳" : "翻訳";
        }

        void DrawFilledChip(Graphics graphics, Rectangle rect, Color backColor, Color textColor, string text, Font font)
        {
            using (var backBrush = new SolidBrush(backColor))
            {
                graphics.FillRectangle(backBrush, rect);
            }

            DrawText(graphics, text, font, rect, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        void DrawButtonChip(Graphics graphics, Rectangle rect, Color backColor, Color borderColor, Color textColor, string text, Font font)
        {
            using (var backBrush = new SolidBrush(backColor))
            using (var borderPen = new Pen(borderColor))
            {
                graphics.FillRectangle(backBrush, rect);
                var borderRect = rect;
                borderRect.Width -= 1;
                borderRect.Height -= 1;
                graphics.DrawRectangle(borderPen, borderRect);
            }

            DrawText(graphics, text, font, rect, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        void DrawText(Graphics graphics, string text, Font font, Rectangle bounds, Color color, TextFormatFlags alignmentFlags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter)
        {
            TextRenderer.DrawText(
                graphics,
                text,
                font,
                bounds,
                color,
                TextFormatFlags.NoPadding |
                TextFormatFlags.NoPrefix |
                TextFormatFlags.PreserveGraphicsClipping |
                TextFormatFlags.SingleLine |
                alignmentFlags);
        }

        int MeasureChipWidth(string text, Font font)
        {
            return MeasureText(text, font).Width + (ChipHorizontalPadding * 2);
        }

        Size MeasureText(string text, Font font)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Size.Empty;
            }

            return TextRenderer.MeasureText(
                text,
                font,
                infiniteTextBox,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        }

        int GetTextHeight(Font font)
        {
            return TextRenderer.MeasureText("Hg", font, infiniteTextBox, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Height;
        }

        int GetMessageLineHeight()
        {
            return TextRenderer.MeasureText("測試", messageFont, infiniteTextBox, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Height;
        }

        int GetSimpleMessageLineHeight()
        {
            return TextRenderer.MeasureText("測試", simpleMessageFont, infiniteTextBox, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Height;
        }

        int GetCardWidth()
        {
            int minWidth = viewMode == LogViewMode.Simple ? 120 : 360;
            int scrollbarAllowance = viewMode == LogViewMode.Simple ? SystemInformation.VerticalScrollBarWidth + 4 : 2;
            return Math.Max(minWidth, ClientSize.Width - scrollbarAllowance);
        }

        int GetScrollOffset()
        {
            return Math.Max(0, -AutoScrollPosition.Y);
        }

        bool IsNearBottom()
        {
            if (AutoScrollMinSize.Height <= 0)
            {
                return true;
            }

            return GetScrollOffset() + ClientSize.Height >= AutoScrollMinSize.Height - 24;
        }

        void RestoreScrollOffset(int requestedOffset)
        {
            int maxOffset = Math.Max(0, AutoScrollMinSize.Height - ClientSize.Height);
            int target = requestedOffset == int.MaxValue ? maxOffset : Math.Max(0, Math.Min(maxOffset, requestedOffset));
            AutoScrollPosition = new Point(0, target);
        }

        Rectangle ToClientRectangle(Rectangle contentBounds)
        {
            return new Rectangle(
                contentBounds.Left + AutoScrollPosition.X,
                contentBounds.Top + AutoScrollPosition.Y,
                contentBounds.Width,
                contentBounds.Height);
        }

        InteractiveRegion HitTest(Point location)
        {
            for (int i = interactiveRegions.Count - 1; i >= 0; i--)
            {
                if (interactiveRegions[i].Bounds.Contains(location))
                {
                    return interactiveRegions[i];
                }
            }

            return null;
        }

        RowLayout FindRowAt(Point location)
        {
            foreach (var row in rowLayouts)
            {
                if (ToClientRectangle(row.Bounds).Contains(location))
                {
                    return row;
                }
            }

            return null;
        }

        void OnTranslateRequested(DisplayedLogGroup entry)
        {
            var handler = TranslateRequested;
            if (handler != null)
            {
                handler(this, new TranslateRequestedEventArgs(entry));
            }
        }

        static void TryCopy(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            try
            {
                Clipboard.SetText(text);
            }
            catch
            {
                // ignored
            }
        }

        static void TryOpenUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            try
            {
                string normalizedUrl = url.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                    ? $"https://{url}"
                    : url;
                Process.Start(new ProcessStartInfo(normalizedUrl) { UseShellExecute = true });
            }
            catch
            {
                // ignored
            }
        }

        sealed class RowLayout
        {
            public RowLayout(DisplayedLogGroup entry, Rectangle bounds, int messageWidth, int headerHeight)
            {
                Entry = entry;
                Bounds = bounds;
                MessageWidth = messageWidth;
                HeaderHeight = headerHeight;
            }

            public DisplayedLogGroup Entry { get; }
            public Rectangle Bounds { get; }
            public int MessageWidth { get; }
            public int HeaderHeight { get; }
        }

        sealed class WrappedLine
        {
            public WrappedLine(int startIndex, int length)
            {
                StartIndex = startIndex;
                Length = length;
            }

            public int StartIndex { get; }
            public int Length { get; }
        }

        sealed class TextFragment
        {
            public TextFragment(string text, bool isLink, string url)
            {
                Text = text;
                IsLink = isLink;
                Url = url;
            }

            public string Text { get; }
            public bool IsLink { get; }
            public string Url { get; }
        }

        enum InteractiveKind
        {
            Copy,
            Link,
            Translate
        }

        public sealed class TranslateRequestedEventArgs : EventArgs
        {
            public TranslateRequestedEventArgs(DisplayedLogGroup entry)
            {
                Entry = entry;
            }

            public DisplayedLogGroup Entry { get; }
        }

        sealed class InteractiveRegion
        {
            public InteractiveRegion(Rectangle bounds, InteractiveKind kind, DisplayedLogGroup entry, string value)
            {
                Bounds = bounds;
                Kind = kind;
                Entry = entry;
                Value = value;
            }

            public Rectangle Bounds { get; }
            public InteractiveKind Kind { get; }
            public DisplayedLogGroup Entry { get; }
            public string Value { get; }
        }

        struct HoverState
        {
            public static readonly HoverState None = new HoverState((InteractiveKind)(-1), null, null);

            public HoverState(InteractiveKind kind, DisplayedLogGroup entry, string value)
            {
                Kind = kind;
                Entry = entry;
                Value = value ?? string.Empty;
            }

            public InteractiveKind Kind { get; }
            public DisplayedLogGroup Entry { get; }
            public string Value { get; }
            public bool IsEmpty => Entry == null;

            public bool Matches(InteractiveKind kind, DisplayedLogGroup entry, string value)
            {
                return !IsEmpty &&
                    Kind == kind &&
                    ReferenceEquals(Entry, entry) &&
                    string.Equals(Value, value ?? string.Empty, StringComparison.Ordinal);
            }
        }
    }
}
