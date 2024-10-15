using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Util.Core.StateMachine;
using Telegram.Util.Core.StateMachine.Graph;

namespace Telegram.Util.Core.Interfaces
{
    public interface IStateMachineBuilder<TState, TTrigger>
    {
        StateMachine<TState, TTrigger> Build(Func<TState> stateAccessor, Action<TState> stateMutator, Func<Task> messageHandler);
    }
}
