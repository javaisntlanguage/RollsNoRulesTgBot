using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.Util.Core.Interfaces
{
	public interface IStateManagerFactory<out TStateManager> where TStateManager : StateManager
	{
		TStateManager Create(long chatId);
	}
}
