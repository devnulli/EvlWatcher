using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvlWatcher.DTOs
{
    public class ExtractedEventRecord
    {
        public DateTime TimeCreated { get; internal set; }
        public string Xml { get; internal set; }
    }
}
