using System;

namespace FBDR.Models
{
    public class Font
    {
        public string FontFamily { get; set; }
        public double Size { get; set; }
        public Color Color { get; set; }
        public Style Style { get; set; }
    }

    [Flags]
    public enum Style
    {
        Regular,
        Bold,
        Italic,
    }
}
