using FBDR.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace FBDR.Converter
{
    public class PathTypeToIconConverter : IValueConverter
    {
        public Canvas FileIcon { get; set; }
        public Canvas DirectoryIcon { get; set; }
        public Canvas ArchiveIcon { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var pathType = (PathType)value;
            if (pathType == PathType.File)
            {
                return FileIcon;
            }
            else if (pathType == PathType.Directory)
            {
                return DirectoryIcon;
            }
            return ArchiveIcon;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
