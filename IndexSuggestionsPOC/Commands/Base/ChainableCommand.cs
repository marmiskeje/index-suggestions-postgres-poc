using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexSuggestionsPOC
{
    public interface IExecutableCommand
    {
        void Execute();
    }

    public interface ICommandSuccessorInfo
    {
        IExecutableCommand Successor { get; }
    }

    public interface IChainableCommand : IExecutableCommand, ICommandSuccessorInfo
    {
        void SetSuccessor(IExecutableCommand successor);
    }

    public class CommandConnector
    {
        private IChainableCommand lastCommand;
        public IChainableCommand FirstCommand { get; private set; }

        public void Add(IChainableCommand command)
        {
            if (FirstCommand == null)
            {
                FirstCommand = command;
                lastCommand = command;
            }
            else
            {
                lastCommand.SetSuccessor(command);
                lastCommand = command;
            }
        }
    }

    public abstract class ChainableCommand : IChainableCommand
    {
        protected bool IsEnabledSuccessorCall { get; set; }

        public ChainableCommand()
        {
            IsEnabledSuccessorCall = true;
        }

        public IExecutableCommand Successor { get; private set; }

        protected abstract void OnExecute();

        public virtual void Execute()
        {
            OnExecute();
            TryCallSuccessor();
        }

        protected void TryCallSuccessor()
        {
            if ((Successor != null) && (IsEnabledSuccessorCall == true))
            {
                Successor.Execute();
            }
        }

        public void SetSuccessor(IExecutableCommand successor)
        {
            this.Successor = successor;
        }

    }
}
