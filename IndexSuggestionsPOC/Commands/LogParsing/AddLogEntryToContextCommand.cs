using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexSuggestionsPOC
{
    class AddLogEntryToContextCommand : ChainableCommand
    {
        private readonly LogParsingContext context;
        public AddLogEntryToContextCommand(LogParsingContext context)
        {
            this.context = context;
        }
        protected override void OnExecute()
        {
            context.LogEntries.Add(context.LogEntry);
        }
    }
}
