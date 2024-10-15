using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Util.Core.Interfaces;
using Telegram.Util.Core.StateMachine;

namespace Telegram.Util.Core.Models
{
	public abstract class StateMachineBuilderBase<TState, TTrigger> : IStateMachineBuilder<TState, TTrigger>
	{
		public StateMachine<TState, TTrigger> Build(Func<TState> stateAccessor, Action<TState> stateMutator, Func<Task> messageHandler)
		{
			StateMachine<TState, TTrigger> result = new(stateAccessor, stateMutator);

			ConfigureMachine(result, messageHandler);

			return result;
		}

		protected abstract void ConfigureMachine(StateMachine<TState, TTrigger> result, Func<Task> messageHandler);
	}
}
