using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexSuggestionsPOC
{
    class ValidateInputLogLineColumnsCommand : ChainableCommand
    {
        private readonly LogParsingContext context;
        public ValidateInputLogLineColumnsCommand(LogParsingContext context)
        {
            this.context = context;
        }
        protected override void OnExecute()
        {
            IsEnabledSuccessorCall = context.InputColumns != null && context.InputColumns.Length >= 11;
        }
    }
}
