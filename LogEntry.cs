using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoLogParse
{
    public class LogEntry
    {
        public DateTime DateTime { get; set; }
        public string Connection { get; set; }
        public string Command { get;  set; }
        public string Database { get;  set; }
        public string Collection { get;  set; }
        public bool CollScan { get; internal set; }
        public int Time { get;  set; }
        public string Line { get; internal set; }
        public long LineNumber;
    }
}
