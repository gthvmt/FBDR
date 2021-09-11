using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace FBDR.Models
{
    public class HtmlElementParser
    {
        #region Fields
        private string _InnerText;

        #endregion

        #region Properties
        public string InnerText => _InnerText ??= GetInnerText(InnerHtml);
        private string InnerHtml => RootElement.InnerHtml;
        public IElement RootElement { get; }
        #endregion

        #region Constructors
        public HtmlElementParser(IElement rootElement)
        {
            RootElement = rootElement;
        }
        #endregion

        #region Methods
        private string GetInnerText(IElement element) => GetInnerText(element.InnerHtml);
        private string GetInnerText(string html) =>
           HttpUtility.HtmlDecode(Regex.Replace(html.Replace(
               "<br>", Environment.NewLine, StringComparison.CurrentCultureIgnoreCase), @"<.+?>", string.Empty));

        /// <summary>
        /// Gets the relative indexes of elements from the <see cref="InnerText"/> of the root element
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public Dictionary<IElement,(int, int)> GetElementIndexes(Func<IElement,IEnumerable<IElement>> f)
        {
            var result = new Dictionary<IElement, (int, int)>();
            var elements = f.Invoke(RootElement);
            int curAbsoluteIndex = 0;
            foreach (var element in elements)
            {
                var absolute = element.OuterHtml;
                var relative = GetInnerText(element);
                var absoluteStartIndex = InnerHtml.IndexOf(absolute, curAbsoluteIndex);

                //need the length of the difference that the GetInnerText() result causes up to the current index
                var absolutePrefix = InnerHtml.Substring(0, absoluteStartIndex);
                var relativePrefix = GetInnerText(absolutePrefix);
                //var relativePrefixLength = relativePrefix.Length;
                //var relativeStartIndex = absoluteStartIndex - relativePrefixLength;
                //var relativeEndIndex = relativeStartIndex + relative.Length;
                //result.Add(element, (relativeStartIndex, relativeEndIndex));
                result.Add(element, (relativePrefix.Length, relativePrefix.Length+relative.Length));
                curAbsoluteIndex = absolutePrefix.Length + absolute.Length;
                //var children = RootElement.Children.Select(x => x.OuterHtml).ToList();
            }

            return result;
        }
        #endregion
    }
}
