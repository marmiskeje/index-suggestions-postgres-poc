using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexSuggestionsPOC
{
    class LogParsingChainFactory
    {
        private LogParsingDetailCommandFactory logParsingDetailCommandFactory = new LogParsingDetailCommandFactory();
        public IExecutableCommand ProcessLine(LogParsingContext context)
        {
            CommandConnector connector = new CommandConnector();
            connector.Add(new ValidateInputLogLineColumnsCommand(context));
            connector.Add(new LoadGeneralInfoToContextCommand(context));
            // TODO filter by username, database, etc.
            connector.Add(new ActionDelegateCommand(() =>
            {
                return context.LogEntry.DatabaseName == "test" && context.LogEntry.ApplicationName != "[unknown]";
            }));
            connector.Add(new ActionDelegateCommand(() =>
            {
                var command = logParsingDetailCommandFactory.GetDetailCommand(context);
                if (command != null)
                {
                    try
                    {
                        command.Execute();
                    }
                    catch (Exception)
                    {
                        //todo
                    }
                    return true;
                }
                return false;
            }));
            connector.Add(new AddLogEntryToContextCommand(context));
            return connector.FirstCommand;
        }
    }
}
