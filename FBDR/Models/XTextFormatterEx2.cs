using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections;
using System.Text;
using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.Layout;
using PdfSharpCore.Pdf.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PdfSharpCore.Drawing.Layout
{
    /// <summary>
    /// Represents a very simple text formatter.
    /// If this class does not satisfy your needs on formatting paragraphs I recommend to take a look
    /// at MigraDoc Foundation. Alternatively you should copy this class in your own source code and modify it.
    /// </summary>
    public class XTextFormatterEx2
    {
        public enum SpacingMode
        {
            /// <summary>
            /// With Relative, the value of Spacing will be added to the default line space.
            /// With 0 you get the default behaviour.
            /// With 5 the line spacing will be 5 points larger than the default spacing.
            /// </summary>
            Relative,

            /// <summary>
            /// With Absolute you set the absolute line spacing.
            /// With 0 all the text will be written at the same line.
            /// </summary>
            Absolute,

            /// <summary>
            /// With Percentage, you can specify larger or smaller line spacing.
            /// With 100 you get the default behaviour.
            /// With 200 you get double line spacing.
            /// With 90 you get 90% of the default line spacing.
            /// </summary>
            Percentage
        }

        public struct LayoutOptions
        {
            public float Spacing;
            public SpacingMode SpacingMode;
            public bool TrimWhiteSpaces;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XTextFormatter"/> class.
        /// </summary>
        public XTextFormatterEx2(XGraphics gfx)
            : this(gfx, new LayoutOptions { SpacingMode = SpacingMode.Relative, Spacing = 0 })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XTextFormatter"/> class.
        /// </summary>
        public XTextFormatterEx2(XGraphics gfx, LayoutOptions options)
        {
            if (gfx == null)
                throw new ArgumentNullException("gfx");
            _gfx = gfx;
            _layoutOptions = options;
        }
        readonly private XGraphics _gfx;
        readonly private LayoutOptions _layoutOptions;

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        public string Text
        {
            get { return _text; }
            set { _text = value; }
        }
        string _text;

        private const string BULLET_POINT = "•";

        /// <summary>
        /// Gets the <see cref="XRect"/> of the last written line
        /// </summary>
        public XRect? LastLineRect => BoundingBoxes.Count > 0 ? BoundingBoxes[^1] : null;
        public double InitialXIndent { get; set; }
        /// <summary>
        /// Gets a list of bounding boxes for each line drawn
        /// </summary>
        public List<XRect> BoundingBoxes => _BoundingBoxes ??= new List<XRect>();
        private List<XRect> _BoundingBoxes;

        /// <summary>
        /// Gets or sets the font.
        /// </summary>
        public XFont Font
        {
            get { return _font; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Font");
                _font = value;

                _lineSpace = _font.GetHeight();
                _cyAscent = _lineSpace * _font.CellAscent / _font.CellSpace;
                _cyDescent = _lineSpace * _font.CellDescent / _font.CellSpace;

                // HACK in XTextFormatter
                _spaceWidth = _gfx.MeasureString("x x", value).Width;
                _spaceWidth -= _gfx.MeasureString("xx", value).Width;

                CalculateLineSpace();
            }
        }
        XFont _font;

        double _lineSpace;
        double _effectiveLineSpace;
        double _cyAscent;
        double _cyDescent;
        double _spaceWidth;

        private bool _preparedText;

        private double GetLineSpace()
        {
            return _effectiveLineSpace;
        }

        public double GetLineSpace(XFont font)
        {
            Font = font;
            return GetLineSpace();
        }

        void CalculateLineSpace()
        {
            switch (_layoutOptions.SpacingMode)
            {
                case SpacingMode.Absolute:
                    _effectiveLineSpace = _layoutOptions.Spacing;
                    break;
                case SpacingMode.Relative:
                    _effectiveLineSpace = _lineSpace + _layoutOptions.Spacing;
                    break;
                case SpacingMode.Percentage:
                    _effectiveLineSpace = _lineSpace * _layoutOptions.Spacing / 100;
                    break;
            }
        }

        /// <summary>
        /// Gets or sets the bounding box of the layout.
        /// </summary>
        public XRect LayoutRectangle
        {
            get { return _layoutRectangle; }
            set { _layoutRectangle = value; }
        }
        XRect _layoutRectangle;

        /// <summary>
        /// Gets or sets the alignment of the text.
        /// </summary>
        public XParagraphAlignment Alignment
        {
            get { return _alignment; }
            set { _alignment = value; }
        }
        XParagraphAlignment _alignment = XParagraphAlignment.Left;

        /// <summary>
        /// Prepares a given text for drawing, performs the layout, returns the index of the last fitting char and the needed height.
        /// </summary>
        /// <param name="text">The text to be drawn.</param>
        /// <param name="font">The font to be used.</param>
        /// <param name="layoutRectangle">The layout rectangle. Set the correct width.
        /// Either set the available height to find how many chars will fit.
        /// Or set height to double.MaxValue to find which height will be needed to draw the whole text.</param>
        /// <param name="lastFittingChar">Index of the last fitting character. Can be -1 if the character was not determined. Will be -1 if the whole text can be drawn.</param>
        /// <param name="neededHeight">The needed height - either for the complete text or the used height of the given rect.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void PrepareDrawString(string text, XFont font, XRect layoutRectangle, out int lastFittingChar, out double neededHeight)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            if (font == null)
                throw new ArgumentNullException("font");

            Text = text;
            Font = font;
            LayoutRectangle = layoutRectangle;

            lastFittingChar = -1;
            neededHeight = double.MinValue;

            if (text.Length == 0)
                return;

            CreateBlocks();

            CreateLayout();

            _preparedText = true;

            double dy = _cyDescent + _cyAscent;
            int count = _blocks.Count;
            for (int idx = 0; idx < count; idx++)
            {
                Block block = _blocks[idx];
                if (block.Stop)
                {
                    // We have a Stop block, so only part of the text will fit. We return the index of the last fitting char (and the height of the block, if available).
                    lastFittingChar = 0;
                    int idx2 = idx - 1;
                    while (idx2 >= 0)
                    {
                        Block block2 = _blocks[idx2];
                        if (block2.EndIndex >= 0)
                        {
                            lastFittingChar = block2.EndIndex;
                            neededHeight = dy + block2.Location.Y; // Test this!!!!!
                            return;
                        }
                        --idx2;
                    }
                    return;
                }
                if (block.Type == BlockType.LineBreak)
                    continue;
                //gfx.DrawString(block.Text, font, brush, dx + block.Location.x, dy + block.Location.y);
                neededHeight = dy + block.Location.Y; // Test this!!!!! Performance optimization?
            }
        }

        /// <summary>
        /// Draws the text that was previously prepared by calling PrepareDrawString or by passing a text to DrawString.
        /// </summary>
        /// <param name="brush">The brush used for drawing the text.</param>
        public void DrawString(XBrush brush)
        {
            DrawString(brush, XStringFormats.TopLeft);
        }

        /// <summary>
        /// Draws the text that was previously prepared by calling PrepareDrawString or by passing a text to DrawString.
        /// </summary>
        /// <param name="brush">The brush used for drawing the text.</param>
        /// <param name="format">Not yet implemented.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void DrawString(XBrush brush, XStringFormat format)
        {
            if (!_preparedText)
                throw new ArgumentException("PrepareDrawString must be called first.");
            if (brush == null)
                throw new ArgumentNullException("brush");
            if (format.Alignment != XStringAlignment.Near || format.LineAlignment != XLineAlignment.Near)
                throw new ArgumentException("Only TopLeft alignment is currently implemented.");

            if (_text.Length == 0)
                return;

            double dx = _layoutRectangle.Location.X;
            double dy = _layoutRectangle.Location.Y + _cyAscent;
            int count = _blocks.Count;
            for (int idx = 0; idx < count; idx++)
            {
                Block block = _blocks[idx];
                if (block.Stop)
                    break;
                if (block.Type == BlockType.LineBreak)
                    continue;
                _gfx.DrawString(block.Text, _font, brush, dx + block.Location.X, dy + block.Location.Y);
            }
        }

        /// <summary>
        /// Draws the text.
        /// </summary>
        /// <param name="text">The text to be drawn.</param>
        /// <param name="font">The font.</param>
        /// <param name="brush">The text brush.</param>
        /// <param name="layoutRectangle">The layout rectangle.</param>
        public void DrawString(string text, XFont font, XBrush brush, XRect layoutRectangle)
        {
            DrawString(text, font, brush, layoutRectangle, XStringFormats.TopLeft);
        }

        /// <summary>
        /// Draws the text.
        /// </summary>
        /// <param name="text">The text to be drawn.</param>
        /// <param name="font">The font.</param>
        /// <param name="brush">The text brush.</param>
        /// <param name="layoutRectangle">The layout rectangle.</param>
        /// <param name="format">The format. Must be <c>XStringFormat.TopLeft</c></param>
        public void DrawString(string text, XFont font, XBrush brush, XRect layoutRectangle, XStringFormat format)
        {
            int dummy1;
            double dummy2;
            PrepareDrawString(text, font, layoutRectangle, out dummy1, out dummy2);

            DrawString(brush);
        }

        void CreateBlocks()
        {
            _blocks.Clear();
            var matches = Regex.Matches(_text, @"[\s\-?]");
            var startIndex = 0;
            Block block = null;
            bool @return = false;
            foreach (Match match in matches)
            {
                bool handled = false;
                var text = _text.Substring(startIndex - (@return ? 1 : 0), match.Index - startIndex);
                if (match.Value == "\r")
                {
                    @return = true;
                    startIndex++;
                    continue;
                }
                else if (match.Value == "\n" || match.Value == "\r\n")
                {
                    block = new Block(BlockType.LineBreak);
                    handled = true;
                }
                //else if (text.Length == 0)
                //        continue;
                else if (string.IsNullOrWhiteSpace(match.Value))
                {
                    block = new Block(BlockType.Space);
                    handled = true;
                }
                @return = false;
                if (!handled)
                {
                    text += match.Value;
                }
                if (!string.IsNullOrEmpty(text))
                {
                    _blocks.Add(new Block(text, BlockType.Text, _gfx.MeasureString(text, _font).Width, startIndex, startIndex + text.Length));
                }
                if (handled)
                {
                    _blocks.Add(block);
                }
                startIndex = match.Index + match.Length;
            }
            if (startIndex < _text.Length)
            {
                var text = _text.Substring(startIndex);
                _blocks.Add(new Block(text, BlockType.Text, _gfx.MeasureString(text, _font).Width, startIndex, startIndex + text.Length));
            }
        }

        void CreateBlocks2()
        {
            _blocks.Clear();
            int length = _text.Length;
            bool inNonWhiteSpace = false;
            int startIndex = 0, blockLength = 0;
            for (int idx = 0; idx < length; idx++)
            {
                char ch = _text[idx];

                // Treat CR and CRLF as LF
                if (ch == Chars.CR)
                {
                    if (idx < length - 1 && _text[idx + 1] == Chars.LF)
                        idx++;
                    ch = Chars.LF;
                }
                if (ch == Chars.LF)
                {
                    if (blockLength != 0)
                    {
                        string token = _text.Substring(startIndex, blockLength);
                        _blocks.Add(new Block(token, BlockType.Text,
                          _gfx.MeasureString(token, _font).Width,
                          startIndex, startIndex + blockLength - 1));
                    }
                    startIndex = idx + 1;
                    blockLength = 0;
                    _blocks.Add(new Block(BlockType.LineBreak));
                }
                else if (Char.IsWhiteSpace(ch))
                {
                    if (inNonWhiteSpace)
                    {
                        string token = _text.Substring(startIndex, blockLength + 1);
                        _blocks.Add(new Block(token, BlockType.Text,
                          _gfx.MeasureString(token, _font).Width,
                          startIndex, startIndex + blockLength - 1));
                        startIndex = idx + 1;
                        blockLength = 0;
                    }
                    else
                    {
                        blockLength++;
                    }
                }
                else
                {
                    inNonWhiteSpace = true;

                    blockLength++;
                }
            }
            if (blockLength != 0)
            {
                string token = _text.Substring(startIndex, blockLength);
                _blocks.Add(new Block(token, BlockType.Text,
                                _gfx.MeasureString(token, _font).Width,
                                startIndex, startIndex + blockLength - 1));
            }
        }

        void CreateLayout()
        {
            double rectWidth = _layoutRectangle.Width;
            double rectHeight = _layoutRectangle.Height - _cyAscent - _cyDescent /*- lineSpace*/;
            int firstIndex = 0;
            double x = InitialXIndent, y = 0;
            int count = _blocks.Count;
            BoundingBoxes.Clear();
            var currentLineRect = new XRect();
            var bboxIndent = InitialXIndent;
            var numLines = 1;
            //var lines = new List<string>();
            //var currentLine = new List<string>();
            for (int idx = 0; idx < count; idx++)
            {
                Block block = _blocks[idx];
                //currentLine.Add(block.Text);

                if (block.Width + InitialXIndent > rectWidth)
                {
                    //_blocks.InsertRange(idx, SplitBlock(block, LayoutRectangle.Width - x));
                    _blocks.InsertRange(idx, SplitBlock(block, rectWidth - InitialXIndent));
                    _blocks.Insert(idx, new Block(BlockType.LineBreak));
                    _blocks.Remove(block);
                    idx--;
                    continue;
                }

                if (block.Type == BlockType.LineBreak)
                {
                    CreateNewLine();
                    if (Alignment == XParagraphAlignment.Justify)
                        _blocks[firstIndex].Alignment = XParagraphAlignment.Left;
                    AlignLine(firstIndex, idx - 1, rectWidth);
                    firstIndex = idx + 1;
                    x = 0;
                    y += GetLineSpace();
                    if (y > rectHeight)
                    {
                        block.Stop = true;
                        break;
                    }
                }
                else
                {
                    if (block.Type == BlockType.Space)
                    {
                        //_gfx.DrawRectangle(XBrushes.Yellow, new XRect(x+_layoutRectangle.X, y+_layoutRectangle.Y, _spaceWidth, GetLineSpace()));
                        x += _spaceWidth;
                    }
                    //else
                    //{
                    //    x = Math.Max(0, x - _spaceWidth);
                    //}
                    if ((x + block.Width <= rectWidth || x == 0) && block.Type != BlockType.LineBreak)
                    {
                        block.Location = new XPoint(x, y);
                        x += block.Width; //!!!modTHHO 19.11.09 add this.spaceWidth here                        

                        currentLineRect =
                            new XRect(currentLineRect.X == 0 ? LayoutRectangle.Left + InitialXIndent : currentLineRect.X,
                            y + LayoutRectangle.Top,
                            x, GetLineSpace());
                    }
                    else
                    {
                        CreateNewLine();
                        AlignLine(firstIndex, idx - 1, rectWidth);
                        firstIndex = idx;
                        y += GetLineSpace();
                        if (y > rectHeight)
                        {
                            block.Stop = true;
                            break;
                        }
                        block.Location = new XPoint(0, y);
                        x = block.Width; //!!!modTHHO 19.11.09 add this.spaceWidth here
                    }
                }
                if (idx == 0)
                {
                    InitialXIndent = 0;
                }

                void CreateNewLine()
                {
                    //lines.Add(string.Join(" ", currentLine));
                    //currentLine.Clear();
                    if (_layoutOptions.TrimWhiteSpaces)
                    {
                        var lastBlockInLineId = Math.Max(0,idx - 1);
                        while (block.Type == BlockType.Space && idx > lastBlockInLineId)
                        {
                            _blocks.RemoveAt(idx);
                            idx = Math.Min(_blocks.Count - 1, idx);
                            if (idx < 0)
                            {
                                break;
                            }
                            block = _blocks[idx];
                        }
                        count = _blocks.Count;
                    }

                    numLines++;
                    AddLineToBoundingBoxes();
                }
            }

            if (firstIndex < count && Alignment != XParagraphAlignment.Justify)
                AlignLine(firstIndex, count - 1, rectWidth);

            if (BoundingBoxes.Count < numLines)
            {
                //lines.Add(string.Join(" ", currentLine));

                currentLineRect =
                    new XRect(LayoutRectangle.Left + bboxIndent,
                    y + LayoutRectangle.Top,
                    x, GetLineSpace());
            }
            AddLineToBoundingBoxes();

            void AddLineToBoundingBoxes()
            {
                if (currentLineRect.Width != 0 || currentLineRect.Height != 0)
                //if (currentLineRect.Width != 0)
                {
                    BoundingBoxes.Add(new XRect(currentLineRect.X, currentLineRect.Y,
                          BoundingBoxes.Count == 0 ? Math.Max(currentLineRect.Width - bboxIndent, 0) : currentLineRect.Width,
                          currentLineRect.Height));
                    currentLineRect = new XRect();
                }
                bboxIndent = 0;
            }
        }

        private IEnumerable<Block> SplitBlock(Block block, double remainingWidth = -1)
        {
            if (remainingWidth == -1)
            {
                remainingWidth = LayoutRectangle.Width;
            }
            var text = block.Text;
            Block lastLine = null;
            var curIndex = 0;
            while (curIndex <= text.Length)
            {
                var remainingText = text.Substring(curIndex);
                for (int i = 0; i <= remainingText.Length; i++)
                {
                    var line = remainingText.Substring(0, i);
                    var width = _gfx.MeasureString(line, _font).Width;
                    if (width > remainingWidth)
                    {
                        yield return lastLine;
                        curIndex += i - 1;
                        lastLine = null;
                        break;
                    }
                    else
                    {
                        lastLine = new Block(line, BlockType.Text, width, block.StartIndex, block.EndIndex - (block.Text.Length - remainingText.Length));
                    }
                }
                //remainingWidth = remainingWidth > -1 && remainingWidth < LayoutRectangle.Width ? remainingWidth : LayoutRectangle.Width;
                if (lastLine is not null)
                {
                    yield return lastLine;
                    break;
                }
                remainingWidth = LayoutRectangle.Width;
            }
        }

        /// <summary>
        /// Align center, right or justify.
        /// </summary>
        void AlignLine(int firstIndex, int lastIndex, double layoutWidth)
        {
            if (firstIndex < 0 || firstIndex >= _blocks.Count)
                return;
            XParagraphAlignment blockAlignment = _blocks[firstIndex].Alignment;
            //if (_alignment == XParagraphAlignment.Left || blockAlignment == XParagraphAlignment.Left)
            if (_alignment == XParagraphAlignment.Left)
                return;

            int count = lastIndex - firstIndex + 1;
            if (count == 0)
                return;

            double totalWidth = -_spaceWidth;
            for (int idx = firstIndex; idx <= lastIndex; idx++)
                totalWidth += _blocks[idx].Width + _spaceWidth;

            double dx = Math.Max(layoutWidth - totalWidth, 0);
            //Debug.Assert(dx >= 0);
            if (_alignment != XParagraphAlignment.Justify)
            {
                if (_alignment == XParagraphAlignment.Center)
                    dx /= 2;
                for (int idx = firstIndex; idx <= lastIndex; idx++)
                {
                    Block block = _blocks[idx];
                    block.Location += new XSize(dx, 0);
                }
            }
            else if (count > 1) // case: justify
            {
                dx /= count - 1;
                for (int idx = firstIndex + 1, i = 1; idx <= lastIndex; idx++, i++)
                {
                    Block block = _blocks[idx];
                    block.Location += new XSize(dx * i, 0);
                }
            }
        }

        public void DrawList(XFont font, XBrush brush, XRect rect, IEnumerable<string> items, double rowSpacing = 0, bool numeric = false)
        {
            Font = font;
            LayoutRectangle = rect;
            double bulletPointIndent = 5;
            double textIndent = 5;
            var bulletPointWidth = _gfx.MeasureString("M", _font).Width;
            var extraWidth = bulletPointIndent + bulletPointWidth + textIndent;
            var textRectangle = new XRect(LayoutRectangle.X + extraWidth, LayoutRectangle.Y,
                LayoutRectangle.Width - extraWidth, LayoutRectangle.Height);
            var position = LayoutRectangle.Location;
            int i = 1;
            foreach (var item in items)
            {
                //_gfx.DrawString(numeric ? i++.ToString() : BULLET_POINT, _font, brush, new XPoint(position.X + bulletPointIndent, position.Y));
                DrawString(numeric ? $"{i++}." : BULLET_POINT, _font, brush, new XRect(position.X + bulletPointIndent, position.Y, bulletPointWidth + textIndent, rowSpacing));
                DrawString(item, _font, brush, textRectangle);
                if (LastLineRect.HasValue)
                {
                    position = new XPoint(position.X, LastLineRect.Value.Bottom + rowSpacing);
                    textRectangle = new XRect(new XPoint(textRectangle.X, position.Y), textRectangle.Size);
                }
            }
        }

        readonly List<Block> _blocks = new List<Block>();

        enum BlockType
        {
            Text, Space, Hyphen, LineBreak,
        }

        /// <summary>
        /// Represents a single word.
        /// </summary>
        class Block
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Block"/> class.
            /// </summary>
            /// <param name="text">The text of the block.</param>
            /// <param name="type">The type of the block.</param>
            /// <param name="width">The width of the text.</param>
            /// <param name="startIndex"></param>
            /// <param name="endIndex"></param>
            public Block(string text, BlockType type, double width, int startIndex, int endIndex)
            {
                Text = text;
                Type = string.IsNullOrWhiteSpace(text) ? BlockType.Space : type;
                Width = width;
                StartIndex = startIndex;
                EndIndex = endIndex;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Block"/> class.
            /// </summary>
            /// <param name="type">The type.</param>
            public Block(BlockType type)
            {
                Type = type;
                Text = type == BlockType.Space ? " " : null;
            }

            /// <summary>
            /// The text represented by this block.
            /// </summary>
            public readonly string Text;

            public readonly int StartIndex = -1;
            public readonly int EndIndex = -1;

            /// <summary>
            /// The type of the block.
            /// </summary>
            public readonly BlockType Type;

            /// <summary>
            /// The width of the text.
            /// </summary>
            public readonly double Width;

            /// <summary>
            /// The location relative to the upper left corner of the layout rectangle.
            /// </summary>
            public XPoint Location;

            /// <summary>
            /// The alignment of this line.
            /// </summary>
            public XParagraphAlignment Alignment;

            /// <summary>
            /// A flag indicating that this is the last block that fits in the layout rectangle.
            /// </summary>
            public bool Stop;
        }
        // - more XStringFormat variations
        // - left and right indent
        // - margins and paddings
        // - background color
        // - text background color
        // - border style
        // - hyphens, soft hyphens, hyphenation
        // - kerning
        // - change font, size, text color etc.
        // - line spacing
        // - underline and strike-out variation
        // - super- and sub-script
        // - ...
    }
}