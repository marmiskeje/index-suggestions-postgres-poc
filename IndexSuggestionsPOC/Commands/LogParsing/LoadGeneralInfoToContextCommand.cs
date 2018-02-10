using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexSuggestionsPOC
{
    class LoadGeneralInfoToContextCommand : ChainableCommand
    {
        private readonly LogParsingContext context;
        public LoadGeneralInfoToContextCommand(LogParsingContext context)
        {
            this.context = context;
        }
        protected override void OnExecute()
        {
            context.LogEntry.ApplicationName = context.InputColumns[2];
            context.LogEntry.DatabaseName = context.InputColumns[4];
            context.LogEntry.ProcessID = context.InputColumns[1];
            context.LogEntry.RemoteHostAndPort = context.InputColumns[5];
            context.LogEntry.SessionID = context.InputColumns[6];
            if (!String.IsNullOrEmpty(context.InputColumns[7]))
            {
                context.LogEntry.SessionLineNumber = long.Parse(context.InputColumns[7]); 
            }
            context.LogEntry.Timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)(double.Parse(context.InputColumns[0], System.Globalization.CultureInfo.InvariantCulture) * 1000)).LocalDateTime;
            context.LogEntry.TransactionID = context.InputColumns[9];
            context.LogEntry.UserName = context.InputColumns[3];
            context.LogEntry.VirtualTransactionIdentifier = new LoggedVirtualTransactionIdentifier(context.InputColumns[8]);
        }
    }
}
