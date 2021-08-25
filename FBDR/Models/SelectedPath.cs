using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBDR.Models
{
    public enum PathType : byte
    {
        File,
        Directory,
        Archive
    }

    public class SelectedPath
    {

        public string Path { get; }

        public string Name { get; }

        public SelectedPath(string path)
        {
            Path = path;
            Name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (System.IO.File.Exists(path))
            {
                if (System.IO.Path.GetExtension(path).ToLower() == ".zip")
                {
                    Type = PathType.Archive;
                }
                else
                {
                    Type = PathType.File;
                }
            }
            else
            {
                Type = PathType.Directory;
            }
        }



        public PathType Type { get; }
    }
}
