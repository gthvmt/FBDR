using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using FBDR.Utilities;
using PdfSharpCore;
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
        private const string SMALL_BULLET_POINT = "·";

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
        private const string LINK_URL = "href";

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
        private const string DATE = "a._39g5";

        #endregion
        #endregion
        #endregion

        public Options Options { get; }

        public FacebookDocumentRenderer(Options options)
        {
            Options = options;
        }

        static Dictionary<string, string> ParseStyle(IElement element)
            => Regex.Matches(element.GetAttribute(STYLE), @"([\w|-]+?):\s+?(\w+\(.+\)|[^;]+)")
            .ToDictionary(r => r.Groups[1].Value.ToLower(), r => r.Groups[2].Value);

        public async Task<byte[]> RenderAsPdf(string html)
        {
            var linksToAdd = new Dictionary<PdfRectangle, string>();
            using var outputStream = new MemoryStream();
            //var parser = new HtmlParser();
            var htmlDocument = await new HtmlParser().ParseDocumentAsync(html);
            var contentHeight = 0.0;
            using var pdfDocument = new PdfDocument();
            var a4Size = PageSizeConverter.ToSize(PageSize.A4);
            var page = pdfDocument.AddPage();
            page.Width = a4Size.Width;
            //page.Height = 10000000;
            //page.Height = 400;
            //page.Height = double.MaxValue;
            //page.MediaBox = 
            var defaultFont = PdfUtils.FontToPdfFont(Options.DefaultFont);
            //var defaultFont = FontToXFont(Options.DefaultFont);

            var defaultFontBrush = PdfUtils.FontToBrush(Options.DefaultFont);
            using var gfx = XGraphics.FromPdfPage(page);
            var fmt = new XTextFormatterEx2(gfx, new XTextFormatterEx2.LayoutOptions
            {
                Spacing = Options.LineSpacing,
                SpacingMode = XTextFormatterEx2.SpacingMode.Relative,
                TrimWhiteSpaces = true,
            });

            gfx.DrawRectangle(new XSolidBrush(PdfUtils.ColorToPdfColor(Options.BackgroundColor)),
                new XRect(0, 0, page.Width, 100000000));

            //var rect = new XRect(MARGIN, MARGIN, dummyPage.Width - (MARGIN * 2), dummyPage.Height - (MARGIN * 2));
            //formatter.DrawString(html, defaultFont, XBrushes.Black, rect, XStringFormats.TopLeft);
            var contentDiv = htmlDocument.GetElementsByClassName(DOCUMENT_CONTENT_DIV)[0];

            var title = contentDiv.GetElementsByClassName("_4lmk _5s6c")[0].TextContent;
            //title = Regex.Replace(title, @"\s+", " " );

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
            contentHeight += Options.TitleMargin;
            fmt.DrawString(title, PdfUtils.FontToPdfFont(Options.TitleFont), PdfUtils.FontToBrush(Options.TitleFont),
                new XRect(Options.MarginLR, contentHeight, page.Width - Options.MarginLR / 2, 1000));

            contentHeight = fmt.LastLineRect.Value.Bottom + Options.AuthorMargin;

            var authorImageUrl = GetUrlFromBackgroundImage(ParseStyle(contentDiv.GetElementsByClassName(AUTHOR_IMAGE_DIV)[0])[BACKGROUND_IMAGE]);
            var authorImageRect = DrawAuthorImage(gfx, page, await GetImageBytes(authorImageUrl), Options.MarginLR, contentHeight);
            var authorDiv = contentDiv.GetElementsByClassName(AUTHOR_NAME_DIV)[0];
            var authorName = authorDiv.TextContent;
            var authorFont = PdfUtils.FontToPdfFont(Options.AuthorFont);
            var authorNameRect = new XRect(authorImageRect.Right + 5, contentHeight, gfx.MeasureString(authorName, authorFont).Width, authorImageRect.Height);
            var authorLink = authorDiv.GetAttribute(LINK_URL);
            page.AddWebLink(new PdfRectangle(gfx.Transformer.WorldToDefaultPage(authorNameRect)), authorLink);

            gfx.DrawString(authorName, authorFont, PdfUtils.FontToBrush(Options.AuthorFont),
                 authorNameRect, XStringFormats.CenterLeft);

            var extractedDate = contentDiv.QuerySelector(DATE).InnerHtml;
            var date = DateTime.Parse(extractedDate);
            var parsedDate = date.ToString(Options.DateFormat);

            var dateFont = PdfUtils.FontToPdfFont(Options.DateFont);

            fmt.InitialXIndent = authorNameRect.Width;
            fmt.DrawString($" {SMALL_BULLET_POINT} {parsedDate}", dateFont, PdfUtils.FontToBrush(Options.DateFont),
                new XRect(new XPoint(authorNameRect.X, authorNameRect.Y + authorImageRect.Height / 2 - fmt.GetLineSpace(dateFont) / 2),
                new XSize(page.Width - Options.MarginLR - authorNameRect.Left, 100)));
            contentHeight += authorImageRect.Height;
            contentHeight = Math.Max(fmt.LastLineRect.Value.Bottom, contentHeight);
            contentDiv = contentDiv.GetElementsByClassName(DOCUMENT_MAIN_TEXT_DIV)[0];

            contentHeight += Options.ContentMargin;
            var bb = await new HtmlTextFormatter(gfx, Options).DrawHtmlElement(new XPoint(Options.MarginLR, contentHeight), page.Width - (Options.MarginLR * 2), contentDiv);
            contentHeight += bb.Height;
            contentHeight += Options.BottomMargin;

            //fit page height to content
            page.MediaBox = new PdfRectangle(new XRect(0, page.Height - contentHeight, page.Width, contentHeight));

            pdfDocument.Save(outputStream);
            return outputStream.ToArray();
        }

        private static string ConvertFacebookLink(string url)
            => url.Contains(FACEBOOK_LINK_PREFIX) ? HttpUtility.UrlDecode(url.Split(FACEBOOK_LINK_PREFIX)[1]) : url;


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

        private XRect DrawHeader(XGraphics gfx, PdfPage page, byte[] imageBytes, double headerIndent)
        {
            var image = XImage.FromStream(() => new MemoryStream(imageBytes));

            gfx.Save();
            var absoulteHeight = 121.0 / 327.0 * page.Width;
            var contentRect = new XRect(0, 0, page.Width, absoulteHeight);
            //gfx.DrawRectangle(XBrushes.Black, contentRect);

            var clip = new XGraphicsPath();
            clip.AddRectangle(contentRect);
            gfx.IntersectClip(clip);

            double imgHeight = image.PointHeight / image.PointWidth * page.Width;
            gfx.DrawImage(image, new XRect(0, -(headerIndent * (imgHeight - absoulteHeight)),
                page.Width, imgHeight));

            var borderPen = new XPen(XBrushes.Black, 1);
            borderPen.Color = XColor.FromArgb(50, XColors.Black);
            gfx.DrawLine(borderPen, contentRect.BottomLeft, contentRect.BottomRight);

            gfx.Restore();
            return contentRect;
        }

        private XRect DrawAuthorImage(XGraphics gfx, PdfPage page, byte[] imageBytes, double x, double y)
        {
            var image = XImage.FromStream(() => new MemoryStream(imageBytes));

            gfx.Save();

            var rect = new XRect(x, y, Options.AuthorImageSize, Options.AuthorImageSize);

            var clip = new XGraphicsPath();
            clip.AddEllipse(rect);
            gfx.IntersectClip(clip);

            gfx.DrawImage(image, rect);
            var borderPen = new XPen(XBrushes.Black, 1);
            borderPen.Color = XColor.FromArgb(50, XColors.Black);
            gfx.DrawEllipse(borderPen, rect);
            //clip.AddEllipse(new XRect(x + outlineSize / 2, y + outlineSize / 2, AUTHOR_IMAGE_SIZE, AUTHOR_IMAGE_SIZE));
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
