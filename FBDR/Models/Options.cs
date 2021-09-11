using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Windows.Media;
//using Color = System.Windows.Media.Color;

namespace FBDR.Models
{
    [Serializable]
    public class Options
    {
        #region Properties
        #region General
        public bool OverwriteFiles { get; set; }
        public Color BackgroundColor { get; set; }
        public string DateFormat { get; set; }
        public double AuthorImageSize { get; set; }
        #endregion
        #region Fonts
        public Font DefaultFont { get; set; }
        public Font TitleFont { get; set; }
        public Font AuthorFont { get; set; }
        public Font DateFont { get; set; }
        public Font ImageFont { get; set; }
        public Color LinkColor { get; set; }
        //public double TitleFontSize { get; set; }
        #endregion
        #region Margins
        public float LineSpacing { get; set; }
        public double ParagraphSpacing { get; set; }
        public double MarginLR { get; set; }
        public double TitleMargin { get; set; }
        public double AuthorMargin { get; set; }
        public double ContentMargin { get; set; }
        public double BottomMargin { get; set; }
        public double HeaderSpacing { get; set; }
        #endregion

        #region Misc
        internal string FilePath =>
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FBDR", "Options.dat");
        #endregion
        #endregion

        #region Methods
        public void ResetMargins()
        {
            MarginLR = 80;
            LineSpacing = 0;
            ParagraphSpacing = 15;
            TitleMargin = 30;
            AuthorMargin = 10;
            ContentMargin = 20;
            BottomMargin = 40;
            HeaderSpacing = 12;
        }

        public void ResetGeneral()
        {
            OverwriteFiles = false;
            BackgroundColor = new Color() { Alpha = 255, Red = 255, Green = 255, Blue = 255 };
            DateFormat = "D";
            AuthorImageSize = 18;
        }

        public void ResetFonts()
        {
            DefaultFont = new Font()
            {
                FontFamily = "Verdana",
                Size = 10,
                Color = new Color()
                {
                    Alpha = 255,
                    Red = 0,
                    Green = 0,
                    Blue = 0,
                }
            };
            TitleFont = new Font
            {
                FontFamily = DefaultFont.FontFamily,
                Size = 12,
                Color = DefaultFont.Color,
                Style = Style.Bold
            };
            AuthorFont = new Font
            {
                FontFamily = DefaultFont.FontFamily,
                Size = DefaultFont.Size,
                Color = DefaultFont.Color,
                Style = Style.Bold
            };
            DateFont = new Font
            {
                FontFamily = DefaultFont.FontFamily,
                Size = 9,
                Color = DefaultFont.Color,
            };
            ImageFont = new Font
            {
                FontFamily = DefaultFont.FontFamily,
                Size = 9,
                Color = new Color()
                {
                    Alpha = 255,
                    Red = 169,
                    Green = 169,
                    Blue = 169
                },
            };
            LinkColor = new Color()
            {
                Alpha = 255,
                Red = 56,
                Green = 88,
                Blue = 152
            };
        }

        public void ResetAll()
        {
            ResetGeneral();
            ResetMargins();
            ResetFonts();
        }
        #endregion
    }
}
