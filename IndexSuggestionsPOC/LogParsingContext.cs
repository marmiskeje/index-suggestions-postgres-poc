using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexSuggestionsPOC
{
    class LogParsingContext
    {
        public string[] InputColumns { get; set; }
        public LoggedEntryInfo LogEntry { get; private set; }
        public List<LoggedEntryInfo> LogEntries { get; set; }
        public LogParsingContext()
        {
            LogEntry = new LoggedEntryInfo();
        }
    }
}
