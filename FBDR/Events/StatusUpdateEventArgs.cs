using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBDR.Events
{
    public class StatusUpdateEventArgs
    {
        public string Status { get; set; }
        public int? Progress { get; set; }
        public int? ProgressMax { get; set; }
        public int? DisplayTime { get; set; }
    }
}
