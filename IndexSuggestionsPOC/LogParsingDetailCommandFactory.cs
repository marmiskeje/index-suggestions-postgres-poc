using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IndexSuggestionsPOC
{
    class LogParsingDetailCommandFactory
    {
        public LogParsingDetailCommandFactory()
        {
            
        }

        public IChainableCommand GetDetailCommand(LogParsingContext context)
        {
            Regex queryRegex = new Regex(@"{QUERY[\s|\S]*}");
            Regex planRegex = new Regex(@"{PLANNEDSTMT[\s|\S]*}");
            string input = context.InputColumns[10];
            Match match = null;
            if (input.StartsWith("STATEMENT:"))
            {
                return new LoadStatementToContextCommand(context, input.Substring(10));
            }
            else if ((match = queryRegex.Match(input)) != null && match.Success)
            {
                return new LoadDebugTreeToContextCommand(() => match.Value, x => context.LogEntry.QueryTree = x);
            }
            else if ((match = planRegex.Match(input)) != null && match.Success)
            {
                return new LoadDebugTreeToContextCommand(() => match.Value, x => context.LogEntry.PlanTree = x);
            }
            // todo statement
            // todo duration
            return null;
        }
    }
}
