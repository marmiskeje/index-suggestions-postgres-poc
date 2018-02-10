using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexSuggestionsPOC
{
    class ActionDelegateCommand : ChainableCommand
    {
        private readonly Func<bool> action;
        public ActionDelegateCommand(Func<bool> action)
        {
            this.action = action;
        }
        protected override void OnExecute()
        {
            IsEnabledSuccessorCall = action();
        }
    }
}
