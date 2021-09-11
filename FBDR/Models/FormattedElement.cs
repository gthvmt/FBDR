using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBDR.Models
{
    public enum FormattedElementType : byte
    {
        Bold,
        Italic,
        Link
    }
    public class FormattedElement
    {
        public FormattedElementType Type {  get; set; }
        public IElement Element {  get; set; }
        public int StartIndex { get; set; }
        public int Length { get; set; }
        public int EndIndex => StartIndex + Length;
    }
}
