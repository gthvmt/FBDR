using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FBDR.Utilities
{
    public static class HtmlUtils
    {
        private const string STYLE = "style";
        private const string BLOB_URL_PREFIX = "data:";

        public static string GetBackGroundImageUrl(IElement element)
            => ParseStyle(element)["background-image"].Split("(")[1][..^1];

        public static Dictionary<string, string> ParseStyle(IElement element)
            => Regex.Matches(element.GetAttribute(STYLE), @"([\w|-]+?):\s+?(\w+\(.+\)|[^;]+)")
            .ToDictionary(r => r.Groups[1].Value.ToLower(), r => r.Groups[2].Value);

        public static bool IsBlobUrl(string url) => url.StartsWith(BLOB_URL_PREFIX);

        public static async Task<byte[]> GetImageBytes(string url)
        {
            byte[] imageBytes = null;
            if (IsBlobUrl(url))
            {
                imageBytes = Convert.FromBase64String(url.Split("base64,")[1]);
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
                            //await System.IO.File.WriteAllBytesAsync("test.jpeg", imageBytes);
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
    }
}
