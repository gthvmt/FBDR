using AngleSharp.Dom;
using FBDR.Utilities;
using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.Layout;
using PdfSharpCore.Pdf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace FBDR.Models
{
    public class HtmlTextFormatter
    {
        private const string FACEBOOK_LINK_PREFIX = "l.facebook.com/l.php?u=";
        private const string BULLET_POINT = "•";

        private XSolidBrush _LinkBrush;
        private XFont _DefaultFont;

        public XTextFormatterEx2 Formatter { get; }
        public Options Options { get; }
        public XGraphics Renderer { get; }
        public XSolidBrush LinkBrush
        {
            get => _LinkBrush ??= new XSolidBrush(PdfUtils.ColorToPdfColor(Options.LinkColor));
            set => _LinkBrush = value;
        }
        public XFont DefaultFont
        {
            get => _DefaultFont ??= PdfUtils.FontToPdfFont(Options.DefaultFont);
            set => _DefaultFont = value;
        }

        public HtmlTextFormatter(XGraphics gfx, Options options)
        {
            Options = options;
            Renderer = gfx;
            Formatter = new XTextFormatterEx2(gfx, new XTextFormatterEx2.LayoutOptions
            {
                Spacing = Options.LineSpacing,
                SpacingMode = XTextFormatterEx2.SpacingMode.Relative,
                TrimWhiteSpaces = true,
            });
        }

        public async Task<XRect> DrawHtmlElement(XPoint location, double width, IElement element)
        {
            double y = location.Y;
            double yIndent = y;
            var type = element.NodeName.ToLower();
            var parser = new HtmlElementParser(element);
            var text = parser.InnerText;
            var elementsToFormat = GetFormattedElements(element);
            var curIndex = 0;
            var defaultFontBrush = PdfUtils.FontToBrush(Options.DefaultFont);
            var fontStyle = DefaultFont.Style;
            var fontBrush = defaultFontBrush;

            var rect = new XRect(location, new XSize(width, double.MaxValue));
            var font = DefaultFont;

            Formatter.BoundingBoxes.Clear();

            if (type == "ol" || type == "ul")
            {
                double bulletPointIndent = 5;
                double textIndent = 3;
                double bulletPointWidth =
                    type == "ul" ?
                    Renderer.MeasureString(BULLET_POINT, DefaultFont).Width :
                    Renderer.MeasureString(element.Children.Length.ToString() + ".", DefaultFont).Width;

                var extraWidth = bulletPointIndent + bulletPointWidth + textIndent;

                for (int i = 0; i < element.Children.Length; i++)
                {
                    var child = element.Children[i];
                    var bulletPoint = type == "ol" ? i + 1 + "." : BULLET_POINT;
                    var currentBulletPointWidth = type == "ul" ? bulletPointWidth :
                        Renderer.MeasureString(bulletPoint, DefaultFont).Width;
                    Formatter.DrawString(bulletPoint, DefaultFont, defaultFontBrush,
                        new XRect(location.X + bulletPointIndent + bulletPointWidth - currentBulletPointWidth,
                        y, width, 100));
                    var bb = await DrawHtmlElement(new XPoint(location.X + extraWidth, y), width - extraWidth, child);
                    //Renderer.DrawRectangle(new XSolidBrush(XColor.FromArgb(100, XColors.Pink)), bb);
                    if (i < element.Children.Length - 1)
                    {
                        y += bb.Height + Formatter.GetLineSpace(DefaultFont) * .5 - Options.ParagraphSpacing;
                    }
                }
                y += Options.ParagraphSpacing;
            }
            else if (type == "figure")
            {
                byte[] imageBytes = null;
                var content = element.FirstChild as IElement;
                var imgElement = content.FirstChild as IElement;
                var isDivImage = imgElement.TagName.ToLower() == "div";

                imageBytes = isDivImage
                    ? await HtmlUtils.GetImageBytes(HtmlUtils.GetBackGroundImageUrl(imgElement))
                    : await HtmlUtils.GetImageBytes(imgElement.GetAttribute("src"));


                if (imageBytes is not null && imageBytes.Length > 0)
                {
                    //TODO Options for image max size
                    var image = XImage.FromStream(() => new MemoryStream(imageBytes));

                    double imageHeight = image.PointHeight;
                    double imageWidth = image.PointWidth;

                    if (imageWidth > width)
                    {
                        double ratio = width / imageWidth;
                        imageWidth = width;
                        imageHeight = imageHeight * ratio;
                    }
                    if (imageHeight > width)
                    {
                        double ratio = width / imageHeight;
                        imageHeight = width;
                        imageWidth = imageWidth * ratio;
                    }
                    Renderer.DrawImage(image, new XRect(location.X + width / 2 - imageWidth / 2, y, imageWidth, imageHeight));
                    //Renderer.DrawImage(image, new XRect(location.X, y, imageWidth, imageHeight));
                    y += imageHeight;

                    if (content.Children.Length > 1)
                    {
                        var subTextElement = content.Children[1];
                        var subTextFont = PdfUtils.FontToPdfFont(Options.ImageFont);
                        var subTextBrush = PdfUtils.FontToBrush(Options.ImageFont);
                        var subText = subTextElement.TextContent;
                        var subTextFormatter = new XTextFormatterEx2(Renderer,
                            new XTextFormatterEx2.LayoutOptions
                            {
                                Spacing = Options.LineSpacing,
                                SpacingMode = XTextFormatterEx2.SpacingMode.Relative,
                                TrimWhiteSpaces = true,
                            })
                        {
                            Alignment = XParagraphAlignment.Center
                        };
                        y += subTextFormatter.GetLineSpace(subTextFont);
                        subTextFormatter.DrawString(subText, subTextFont, subTextBrush,
                            new XRect(location.X, y, width, double.MaxValue));
                        y = subTextFormatter.LastLineRect.Value.Bottom;
                    }
                }
            }
            else if (type == "div" && element.Children.Any(x => new List<string> { "div", "ul", "ol" }.Contains(x.NodeName.ToLower())))
            {
                foreach (var child in element.Children)
                {
                    var bb = await DrawHtmlElement(new XPoint(location.X, y), width, child);
                    y += bb.Height;
                    //y += Options.ParagraphSpacing;
                }
            }
            else
            {
                if (type == "li")
                {
                    var lastLineBreakIndex = text.LastIndexOf(Environment.NewLine);
                    if (lastLineBreakIndex >= 0)
                    {
                        text = text.Remove(lastLineBreakIndex, Environment.NewLine.Length);
                    }
                }
                //TODO handle h3 as well (h1-h6?)
                else if (Regex.IsMatch(type, @"h[1-6]"))
                {
                    y += Options.HeaderSpacing;
                    rect.Y += Options.HeaderSpacing;
                    var h = char.GetNumericValue(type[^1]);
                    var factor = 2 / Math.Sqrt(h);
                    font = new XFont(font.Name, DefaultFont.Size * factor);
                }
                if (elementsToFormat.Count > 0)
                {
                    foreach (var elementToFormat in elementsToFormat)
                    {
                        var elements = elementsToFormat.Where(x => x.StartIndex >= elementToFormat.StartIndex && x.EndIndex <= elementToFormat.EndIndex);
                        var currentTypes = elements.Select(x => x.Type);

                        var len = elementToFormat.StartIndex - curIndex;

                        if (len < 0)
                        {
                            continue;
                        }

                        var preText = text.Substring(curIndex, len);
                        var elementText = text.Substring(elementToFormat.StartIndex, elementToFormat.Length);

                        if (!string.IsNullOrEmpty(preText))
                        {
                            Formatter.PrepareDrawString(preText, font, new XRect(
                                  new XPoint(rect.X, Formatter.LastLineRect.HasValue ? Formatter.LastLineRect.Value.Y : rect.Y), rect.Size), out _, out _);
                            Formatter.DrawString(defaultFontBrush);
                            Formatter.InitialXIndent = Formatter.LastLineRect.HasValue ? Formatter.LastLineRect.Value.Right - rect.X : 0;
                        }

                        fontStyle = XFontStyle.Regular;
                        fontBrush = defaultFontBrush;


                        if (currentTypes.Contains(FormattedElementType.Bold))
                        {
                            fontStyle |= XFontStyle.Bold;
                        }
                        if (currentTypes.Contains(FormattedElementType.Italic))
                        {
                            fontStyle |= XFontStyle.Italic;
                        }

                        if (currentTypes.Contains(FormattedElementType.Link))
                        {
                            fontBrush = LinkBrush;
                        }

                        var elementFont = new XFont(font.Name, font.Size, fontStyle);
                        Formatter.PrepareDrawString(elementText, elementFont, new XRect(
                            new XPoint(rect.X, Formatter.LastLineRect.HasValue ? Formatter.LastLineRect.Value.Y : rect.Y), rect.Size), out _, out _);
                        Formatter.DrawString(fontBrush);
                        if (currentTypes.Contains(FormattedElementType.Link) && Formatter.LastLineRect.HasValue)
                        {
                            var links = elements.Where(x => x.Element.HasAttribute("href"))
                                .Select(x => ConvertFacebookLink(x.Element.GetAttribute("href")));
                            foreach (var link in links)
                            {
                                foreach (var bb in Formatter.BoundingBoxes)
                                {
                                    Renderer.PdfPage.AddWebLink(new PdfRectangle(Renderer.Transformer.WorldToDefaultPage(bb)), link);
                                }
                            }
                        }
                        Formatter.InitialXIndent = Formatter.LastLineRect.HasValue ? Formatter.LastLineRect.Value.Right - rect.X : 0;
                        curIndex += preText.Length + elementText.Length;
                    }
                    if (curIndex < text.Length)
                    {
                        var missingText = text.Substring(curIndex);
                        Formatter.PrepareDrawString(missingText, font, new XRect(
                         new XPoint(rect.X, Formatter.LastLineRect.HasValue ? Formatter.LastLineRect.Value.Y : rect.Y), rect.Size), out _, out _);
                        Formatter.DrawString(defaultFontBrush);
                    }
                    Formatter.InitialXIndent = 0;
                    if (Formatter.LastLineRect.HasValue)
                    {
                        y = Formatter.LastLineRect.Value.Bottom;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(text))
                    {
                        Formatter.DrawString(text, DefaultFont, defaultFontBrush, rect);
                        y = Formatter.LastLineRect.Value.Bottom;
                    }
                }
            }
            y += Options.ParagraphSpacing;
            var height = y - yIndent;
            //return new XRect(location, new XPoint(width, height));
            return new XRect(location.X, location.Y, width, height);
        }

        private static string ConvertFacebookLink(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }
            return url.Contains(FACEBOOK_LINK_PREFIX) ? HttpUtility.UrlDecode(url.Split(FACEBOOK_LINK_PREFIX)[1]) : url;
        }

        private List<FormattedElement> GetFormattedElements(IElement element)
        {
            var parser = new HtmlElementParser(element);

            var elementsToFormat = new List<FormattedElement>();

            var querySelectors = new Dictionary<FormattedElementType, string>
            {
                [FormattedElementType.Link] = "a[href]",
                [FormattedElementType.Bold] = "span._4yxo",
                [FormattedElementType.Italic] = "span._4yxp",
            };

            foreach (var selector in querySelectors)
            {
                foreach (var indexes in parser.GetElementIndexes(x => x.QuerySelectorAll(selector.Value)))
                {
                    elementsToFormat.Add(new FormattedElement()
                    {
                        Element = indexes.Key,
                        StartIndex = indexes.Value.Item1,
                        Length = indexes.Value.Item2 - indexes.Value.Item1,
                        Type = selector.Key
                    });
                }
            }

            //elementsToFormat = elementsToFormat.OrderBy(x => x.StartIndex).ToList();
            elementsToFormat.Sort((x, y) =>
            {
                var comparison = x.StartIndex.CompareTo(y.StartIndex);
                return comparison == 0 ? x.Length.CompareTo(y.Length) : comparison;
            });
            return elementsToFormat;
        }
    }
}
