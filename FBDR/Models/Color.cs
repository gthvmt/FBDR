using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBDR.Models
{
    public class Color
    {
        public byte Alpha { get; set; }
        public byte Red { get; set; }
        public byte Green { get; set; }
        public byte Blue { get; set; }

        public Color Invert()
        {
            byte alpha = Alpha;
            byte red = Convert.ToByte(Math.Abs(255 - Red));
            byte green = Convert.ToByte(Math.Abs(255 - Green));
            byte blue = Convert.ToByte(Math.Abs(255 - Blue));
            return new Color
            {
                Alpha = alpha,
                Red = red,
                Green = green,
                Blue = blue
            };
        }
    }
}
