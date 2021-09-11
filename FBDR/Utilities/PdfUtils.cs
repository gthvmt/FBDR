using FBDR.Models;
using PdfSharpCore.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBDR.Utilities
{
    public static class PdfUtils
    {
        public static XFont FontToPdfFont(Font font)
        {
            var fontStyle = XFontStyle.Regular;
            if (font.Style.HasFlag(Style.Bold | Style.Italic))
            {
                fontStyle |= XFontStyle.BoldItalic;
            }
            else if (font.Style.HasFlag(Style.Bold))
            {
                fontStyle |= XFontStyle.Bold;
            }
            else if (font.Style.HasFlag(Style.Italic))
            {
                fontStyle |= XFontStyle.Italic;
            }
            return new XFont(font.FontFamily, font.Size, fontStyle);
        }

        public static XSolidBrush FontToBrush(Font font)
            => new XSolidBrush(ColorToPdfColor(font.Color));

        public static XColor ColorToPdfColor(Color color)
            => XColor.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
    }
}
