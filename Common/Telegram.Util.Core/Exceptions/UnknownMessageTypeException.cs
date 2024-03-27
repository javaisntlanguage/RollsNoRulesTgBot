using Telegram.Bot.Types.Enums;

namespace Telegram.Util.Core.Exceptions
{
    public class UnknownMessageTypeException : Exception
    {
        public UnknownMessageTypeException(long chatId, UpdateType updateType)
            : base($"Незивестный тип сообщения. Номер чата: {chatId.ToString()}. Тип: {updateType.ToString()}")
        {

        }
    }
}
