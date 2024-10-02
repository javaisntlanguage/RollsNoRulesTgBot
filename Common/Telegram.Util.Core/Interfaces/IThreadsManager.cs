using Telegram.Bot.Types;

namespace Telegram.Util.Core.Interfaces
{
	public interface IThreadsManager
	{
		Task<bool> ProcessUpdate(Update update);
	}
}