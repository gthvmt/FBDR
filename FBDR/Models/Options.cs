using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBDR.Models
{
    [Serializable]
    public class Options
    {
        #region Properties
        #region General
        public bool OverwriteFiles { get; set; }
        #endregion
        #region Fonts
        public Font DefaultFont { get; set; }
        //public double TitleFontSize { get; set; }
        #endregion
        #region Margins
        public float LineSpacing { get; set; }
        public double ParagraphSpacing { get; set; }
        public double MarginLR { get; set; }
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
            ParagraphSpacing = 10;
        }

        public void ResetGeneral()
        {
            OverwriteFiles = false;
        }

        public void ResetAll()
        {
            ResetGeneral();
            ResetMargins();
        }
        #endregion
    }
}
