using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.Layout;
using PdfSharpCore.Pdf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace FBDR.Models
{
    public class FacebookDocumentRenderer
    {
        #region Constants
        private const string ZIP_EXTENSION = ".zip";
        private readonly static string[] HTML_EXTENSIONS = new string[] { ".html", ".htm" };
        private const string DEFAULT_DIR_NAME = "{0} (PDF)";
        private const string DEFAULT_FONT_PATH = @"C:\WINDOWS\FONTS\VERDANA.TTF";
        private const string DEFAULT_FONT_NAME = "Verdana";

        private const float TITLE_FONT_SIZE = 12;
        private const float DEFAULT_FONT_SIZE = 10;
        private const float LINE_SPACING = 0;
        /// <summary>
        /// spacing that gets inserted before each paragraph
        /// </summary>
        private const float PARAGRAPH_SPACING = 10;
        private const float MARGIN = 80;
        private const float AUTHOR_IMAGE_SIZE = 18;

        #region HTML Constants
        private const string STYLE = "style";
        private const string BLOB_URL_PREFIX = "data:image";
        private const string BACKGROUND_IMAGE = "background-image";
        private const string BACKGROUND_POSITION = "background-position";

        #region Facebook Document Div Classes
        private const string DIV = "div";
        private const string DOCUMENT_CONTENT_DIV = "_4lmi";
        private const string DOCUMENT_MAIN_TEXT_DIV = "_39k5 _5s6c";
        private const string TITLE_DIV = "_4lmk _5s6c";
        private const string HEADER_IMAGE_DIV = "_30q-";
        private const string AUTHOR_IMAGE_DIV = "_2yuf";
        private const string AUTHOR_NAME_DIV = "_2yug";
        private const string BACKGROUND_IMAGE_URL_PREFIX = "background-image: url(";
        private const string FACEBOOK_LINK_PREFIX = "l.facebook.com/l.php?u=";
        private const string LINK_URL = "href";

        #endregion
        #endregion
        #endregion

        //async Task Main(string[] args)
        //{
        //    if (args.Length < 1)
        //    {
        //        args = new string[2];
        //        Console.WriteLine("Please provide a source path:");
        //        args[0] = Console.ReadLine();
        //        Console.WriteLine("Optionally provide a path for the output:");
        //        args[1] = Console.ReadLine();
        //    }

        //    var path = args[0].Trim('"');
        //    var outputPath = args.Length > 1 ? args[1].Trim('"') : null;
        //    var pathIsFile = File.Exists(path);
        //    var pathIsDirectory = Directory.Exists(path);

        //    if (!pathIsDirectory && !pathIsFile)
        //    {
        //        Console.WriteLine("provided path does not lead to a file or directory");
        //        return;
        //    }

        //    if (!Directory.Exists(outputPath))
        //    {
        //        if (string.IsNullOrWhiteSpace(outputPath))
        //        {
        //            outputPath = pathIsFile ? Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path)) + ".pdf"
        //                : Path.Combine(Path.GetDirectoryName(path), string.Format(DEFAULT_DIR_NAME, Path.GetFileName(path)));
        //            if (pathIsDirectory)
        //            {
        //                if (!Directory.Exists(outputPath))
        //                {
        //                    Directory.CreateDirectory(outputPath);
        //                }
        //            }
        //        }
        //        else
        //        {
        //            //Check if path is a valid windows path
        //            try
        //            {
        //                Path.GetFullPath(outputPath);
        //            }
        //            catch
        //            {
        //                Console.WriteLine("output path is not a path");
        //                return;
        //            }

        //            if (pathIsDirectory)
        //            {
        //                if (!Directory.Exists(outputPath))
        //                {
        //                    Directory.CreateDirectory(outputPath);
        //                }
        //                outputPath = Path.Combine(outputPath, string.Format(DEFAULT_DIR_NAME, Path.GetFileName(path)));
        //            }
        //            else
        //            {
        //                var dirPath = Path.GetDirectoryName(outputPath);
        //                if (!Directory.Exists(dirPath))
        //                {
        //                    Directory.CreateDirectory(dirPath);
        //                }
        //                outputPath = Path.Combine(dirPath, string.Format(DEFAULT_DIR_NAME, Path.GetFileNameWithoutExtension(path))) + ".pdf";
        //            }
        //        }
        //    }

        //    if (pathIsFile)
        //    {
        //        if (Path.GetExtension(path).ToLower() == ZIP_EXTENSION)
        //        {
        //            ZipFile.ExtractToDirectory(path, "");
        //        }
        //        else if (HTML_EXTENSIONS.Contains(Path.GetExtension(path).ToLower()))
        //        {
        //            var html = await File.ReadAllTextAsync(path);
        //            var doc = await ConvertToPdf(html);
        //            await File.WriteAllBytesAsync("test.pdf", doc);
        //            //await ConvertToPdf(path, outputPath);
        //        }
        //        else
        //        {
        //            Console.WriteLine("provided file-path does not lead to a .zip- or html-file");
        //            return;
        //        }
        //    }
        //    else if (pathIsDirectory)
        //    {
        //        foreach (var filePath in Directory.GetFiles(path))
        //        {
        //            if (Path.GetExtension(filePath).StartsWith(".htm"))
        //            {
        //                Console.WriteLine(filePath);
        //                var html = await File.ReadAllTextAsync(filePath);
        //                var doc = await ConvertToPdf(html);
        //                await File.WriteAllBytesAsync(Path.Combine(outputPath, Path.GetFileNameWithoutExtension(filePath) + ".pdf"), doc);
        //            }
        //        }
        //    }
        //    //Console.ReadKey();
        //}

        public Options Options { get; }

        public FacebookDocumentRenderer(Options options)
        {
            Options = options;
        }

        static Dictionary<string, string> ParseStyle(IElement element)
            => Regex.Matches(element.GetAttribute(STYLE), @"([\w|-]+?):\s+?(\w+\(.+\)|[^;]+)")
            .ToDictionary(r => r.Groups[1].Value.ToLower(), r => r.Groups[2].Value);

        private XFont FontToXFont(Font font)
        {
            var fontStyle = XFontStyle.Regular;
            if (font.Bold && font.Italic)
            {
                fontStyle = fontStyle | XFontStyle.BoldItalic;
            }
            else if (font.Bold)
            {
                fontStyle = fontStyle | XFontStyle.Bold;
            }
            else if (font.Italic)
            {
                fontStyle = fontStyle | XFontStyle.Italic;
            }
            if (font.Underline)
            {
                fontStyle = fontStyle | XFontStyle.Underline;
            }
            if (font.Strikeout)
            {
                fontStyle = fontStyle | XFontStyle.Strikeout;
            }
            return new XFont(font.Name, font.SizeInPoints, fontStyle);
        }

        public async Task<byte[]> RenderAsPdf(string html)
        {
            var linksToAdd = new Dictionary<PdfRectangle, string>();
            using var outputStream = new MemoryStream();
            var parser = new HtmlParser();
            var htmlDocument = await parser.ParseDocumentAsync(html);
            var contentHeight = 0.0;
            using var pdfDocument = new PdfDocument();

            //var a4Size = PageSizeConverter.ToSize(PageSize.A4);
            var page = new PdfPage();
            pdfDocument.AddPage(page);
            var defaultFont = new XFont(DEFAULT_FONT_NAME, DEFAULT_FONT_SIZE, XFontStyle.Regular);

            //var defaultFont = FontToXFont(Options.DefaultFont);

            var defaultFontBrush = XBrushes.Black;
            using var gfx = XGraphics.FromPdfPage(page);

            var fmt = new XTextFormatterEx2(gfx, new XTextFormatterEx2.LayoutOptions
            {
                Spacing = Options.LineSpacing,
                SpacingMode = XTextFormatterEx2.SpacingMode.Relative
            });

            //var rect = new XRect(MARGIN, MARGIN, dummyPage.Width - (MARGIN * 2), dummyPage.Height - (MARGIN * 2));
            //formatter.DrawString(html, defaultFont, XBrushes.Black, rect, XStringFormats.TopLeft);
            var contentDiv = htmlDocument.GetElementsByClassName(DOCUMENT_CONTENT_DIV)[0];
            var title = contentDiv.GetElementsByClassName("_4lmk _5s6c")[0].TextContent;
            var headerDivs = htmlDocument.GetElementsByClassName(HEADER_IMAGE_DIV);
            if (headerDivs.Length > 0)
            {
                var headerDiv = headerDivs[0];
                var headerStyle = ParseStyle(headerDiv);
                var headerUrl = GetUrlFromBackgroundImage(headerStyle[BACKGROUND_IMAGE]);
                var headerIndent = Convert.ToInt32(headerStyle[BACKGROUND_POSITION].Split(" ")[1][..^1]) / 100.0;
                var imageBytes = await GetImageBytes(headerUrl);
                contentHeight = DrawHeader(gfx, page, imageBytes, headerIndent).Height;
            }
            //htmlDocument.DefaultView.GetComputedStyle(imageDiv);

            DrawText(title, 0, 20, new XFont(DEFAULT_FONT_NAME, TITLE_FONT_SIZE, XFontStyle.Bold));

            contentHeight += 10;

            var authorImageUrl = GetUrlFromBackgroundImage(ParseStyle(contentDiv.GetElementsByClassName(AUTHOR_IMAGE_DIV)[0])[BACKGROUND_IMAGE]);

            var authorImageRect = DrawAuthorImage(gfx, page, await GetImageBytes(authorImageUrl), Options.MarginLR, contentHeight);
            var authorDiv = contentDiv.GetElementsByClassName(AUTHOR_NAME_DIV)[0];
            var authorName = authorDiv.TextContent;
            var authorFont = new XFont(DEFAULT_FONT_NAME, DEFAULT_FONT_SIZE, XFontStyle.Bold);
            var authorNameRect = new XRect(authorImageRect.Right + 5, contentHeight, gfx.MeasureString(authorName, authorFont).Width, authorImageRect.Height);
            var authorLink = authorDiv.GetAttribute(LINK_URL);
            page.AddWebLink(new PdfRectangle(gfx.Transformer.WorldToDefaultPage(authorNameRect)), authorLink);

            gfx.DrawString(authorName, authorFont, defaultFontBrush,
                 authorNameRect, XStringFormats.CenterLeft);
            contentHeight += authorImageRect.Height;
            contentDiv = contentDiv.GetElementsByClassName(DOCUMENT_MAIN_TEXT_DIV)[0];
            var linkBrush = new XSolidBrush(XColor.FromArgb(56, 88, 152));

            foreach (var child in contentDiv.Children)
            {
                var type = child.NodeName.ToLower();
                var text = GetInnerText(child);
                if (type == "h2")
                {
                    DrawText(text, 0, 0, new XFont(DEFAULT_FONT_NAME, TITLE_FONT_SIZE));
                }
                else
                {
                    var links = child.QuerySelectorAll("a[href]");
                    if (links.Length > 0)
                    {
                        var curPos = 0;

                        var textStartHeight = contentHeight;
                        var baseSize = new XSize(page.Width - Options.MarginLR * 2, contentHeight);

                        foreach (var link in links)
                        {
                            var yIndent = curPos == 0 ? Options.ParagraphSpacing + fmt.GetLineSpace(defaultFont) : 0;

                            var linkText = GetInnerText(link);
                            var linkUrl = ConvertFacebookLink(link.GetAttribute(LINK_URL));

                            var linkStartIndex = text.IndexOf(linkText, curPos);
                            var textLeadingUpToLink = text.Substring(curPos, linkStartIndex - curPos);

                            if (curPos > 0)
                            {
                                fmt.InitialXIndent = fmt.LastLineRect.Right - Options.MarginLR;
                            }

                            fmt.PrepareDrawString(textLeadingUpToLink, defaultFont,
                                new XRect(new XPoint(Options.MarginLR, fmt.LastLineRect.Y + yIndent), baseSize), out _, out _);
                            fmt.DrawString(defaultFontBrush);

                            if (!string.IsNullOrEmpty(textLeadingUpToLink))
                            {
                                yIndent = 0;
                                fmt.InitialXIndent = fmt.LastLineRect.Right - Options.MarginLR;
                            }

                            fmt.PrepareDrawString(linkText, defaultFont,
                                new XRect(new XPoint(Options.MarginLR, fmt.LastLineRect.Y + yIndent), baseSize), out _, out _);
                            fmt.DrawString(linkBrush);

                            foreach (var bb in fmt.BoundingBoxes)
                            {
                                page.AddWebLink(new PdfRectangle(gfx.Transformer.WorldToDefaultPage(bb)), linkUrl);
                                //gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb((int)(0.2 * 255), 255, 0, 0)), bb);
                            }

                            curPos += textLeadingUpToLink.Length + linkText.Length;
                        }
                        if (curPos < text.Length - 1)
                        {
                            var missingText = text.Substring(curPos, text.Length - curPos);
                            fmt.InitialXIndent = fmt.LastLineRect.Width + (fmt.LastLineRect.X - Options.MarginLR);
                            fmt.PrepareDrawString(missingText, defaultFont,
                                new XRect(Options.MarginLR, fmt.LastLineRect.Y, baseSize.Width, baseSize.Height),
                                out _, out var h2);
                            fmt.DrawString(defaultFontBrush);
                        }
                        contentHeight += fmt.LastLineRect.Bottom - textStartHeight;
                    }
                    else
                    {
                        DrawText(text);
                    }
                }
            }
            contentHeight += 40;

            //fit page height to content
            page.MediaBox = new PdfRectangle(new XRect(0, page.Height - contentHeight, page.Width, contentHeight));

            pdfDocument.Save(outputStream);
            return outputStream.ToArray();

            void DrawText(string text, double indentX = 0, double indentY = 0, XFont font = null)
            {
                font = font ?? defaultFont;
                indentY += Options.ParagraphSpacing;
                var rect = new XRect(Options.MarginLR + indentX, contentHeight + indentY, page.Width - Options.MarginLR * 2, 10000);
                //gfx.DrawRectangle(XBrushes.LightGray, rect);
                fmt.PrepareDrawString(text, font, rect, out _, out double textHeight);
                if (double.MinValue == textHeight)
                {
                    textHeight = gfx.MeasureString("|", font).Height;
                }
                fmt.DrawString(defaultFontBrush);
                contentHeight += textHeight + indentY;
            }
        }

        private static string ConvertFacebookLink(string url)
            => url.Contains(FACEBOOK_LINK_PREFIX) ? HttpUtility.UrlDecode(url.Split(FACEBOOK_LINK_PREFIX)[1]) : url;

        private static string GetInnerText(IElement element) =>
            HttpUtility.HtmlDecode(Regex.Replace(element.InnerHtml.Replace(
                "<br>", Environment.NewLine, StringComparison.CurrentCultureIgnoreCase), @"<.+?>", string.Empty));


        static string GetUrlFromBackgroundImage(string style) => style.Split("(")[1][..^1];

        async static Task<byte[]> GetImageBytes(string url)
        {
            byte[] imageBytes = null;
            if (url.StartsWith(BLOB_URL_PREFIX))
            {
                imageBytes = Convert.FromBase64String(url.Split("base64,")[1]);
                //image = new Image(ImageDataFactory.Create("https://upload.wikimedia.org/wikipedia/en/9/9a/Trollface_non-free.png"));
            }
            else
            {
                if (url.Contains(@"\/\/"))
                {
                    url = url.Replace("\\/", "/").Trim('"');
                }
                using (var client = new HttpClient())
                {
                    using (var response = await client.GetAsync(System.Net.WebUtility.HtmlDecode(url)))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            imageBytes = await response.Content.ReadAsByteArrayAsync();
                        }
                        else
                        {
                            Console.WriteLine("Couldn't resolve image for \"" + url + "\"");
                        }
                    }
                }
            }
            return imageBytes;
        }

        static XRect DrawHeader(XGraphics gfx, PdfPage page, byte[] imageBytes, double headerIndent)
        {
            var image = XImage.FromStream(() => new MemoryStream(imageBytes));

            gfx.Save();
            var absoulteHeight = 121.0 / 327.0 * page.Width;
            //var graphicRect = new XRect(1,1,dummyPage.Width-2, 121.0 / 327.0 * dummyPage.Width-2);
            //border
            var contentRect = new XRect(0, 0, page.Width, absoulteHeight);
            gfx.DrawRectangle(XBrushes.DarkGray, contentRect);

            var clip = new XGraphicsPath();
            clip.AddRectangle(new XRect(0, 0, page.Width, absoulteHeight - .5));
            gfx.IntersectClip(clip);
            //TODO: maybe fix placement?
            gfx.DrawImage(image, new XRect(0, -(headerIndent * absoulteHeight), page.Width, image.PointHeight / image.PointWidth * page.Width));
            gfx.Restore();
            return contentRect;
        }

        static XRect DrawAuthorImage(XGraphics gfx, PdfPage page, byte[] imageBytes, double x, double y)
        {
            var image = XImage.FromStream(() => new MemoryStream(imageBytes));

            gfx.Save();
            var clip = new XGraphicsPath();
            var outlineSize = .9;
            var rect = new XRect(x, y, AUTHOR_IMAGE_SIZE + outlineSize, AUTHOR_IMAGE_SIZE + outlineSize);
            gfx.DrawEllipse(XBrushes.Gray, rect);
            clip.AddEllipse(new XRect(x + outlineSize / 2, y + outlineSize / 2, AUTHOR_IMAGE_SIZE, AUTHOR_IMAGE_SIZE));
            gfx.IntersectClip(clip);
            gfx.DrawImage(image, rect);
            gfx.Restore();
            return rect;
        }

        //static HtmlNode GetDivByClass(HtmlNode node, string divClass)
        //    => node.Descendants(DIV).FirstOrDefault(n => divClass.Split(" ").All(c => n.HasClass(c)));

        static byte[] CropImage(byte[] input, double indent = 0)
        {
            var converter = new ImageConverter();
            var result = new byte[input.Length];
            var image = converter.ConvertFrom(input) as Bitmap;
            var height = (int)Math.Round(121.0 / 327.0 * image.Width);
            var bitmap = new Bitmap(image.Width, height);
            using (System.Drawing.Graphics g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(image, 0, -(int)Math.Round(indent * height));
            }
            return converter.ConvertTo(bitmap, typeof(byte[])) as byte[];
        }

        //static float ConvertPointToPixel(float point)
        //    => point / 0.75f;
    }
}
