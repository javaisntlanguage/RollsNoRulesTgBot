using AdminTgBot.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Util.Core.Interfaces;

namespace AdminTgBot.Infrastructure.Interfaces
{
	internal interface IAdminStateMachineBuilder : IStateMachineBuilder<State, Trigger>
	{
	}
}
