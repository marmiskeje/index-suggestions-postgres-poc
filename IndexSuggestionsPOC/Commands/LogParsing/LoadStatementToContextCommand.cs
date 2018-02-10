using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexSuggestionsPOC
{
    class LoadStatementToContextCommand : ChainableCommand
    {
        private readonly LogParsingContext context;
        private readonly string statement;

        public LoadStatementToContextCommand(LogParsingContext context, string statement)
        {
            this.context = context;
            this.statement = statement;
        }
        protected override void OnExecute()
        {
            context.LogEntry.Statement = statement.Trim();
        }
    }
}
