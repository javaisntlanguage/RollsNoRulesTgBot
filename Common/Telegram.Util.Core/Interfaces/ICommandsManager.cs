using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Util.Core.Interfaces
{
    public interface ICommandsManager
    {
        Task<bool> ProcessMessageAsync(Message message);
        Task<bool> ProcessQueryAsync(CallbackQuery query);
    }
}