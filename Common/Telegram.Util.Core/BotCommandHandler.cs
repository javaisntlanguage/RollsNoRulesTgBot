using Telegram.Bot.Types;
using Database;

namespace Robox.Telegram.Util.Core
{
    public class BotCommandHandler
    {
        public string Name { get; }
        public string Command { get; }
        public IEnumerable<string> TriggerCommands { get; }
        public string Description { get; }
        public Type Conversation { get; }
        public IEnumerable<RolesList> Roles { get; }
        public Func<Message, Task> TriggerAction { get; set; }
        public CommandDisplay DisplayMode { get; set; }

        public BotCommandHandler() { }

        /// <summary>
        /// Для создания тг команд
        /// </summary>
        /// <param name="name"></param>
        /// <param name="conversation">класс-обработчик</param>
        /// <param name="description"></param>
        /// <param name="displayMode">режим отображения команды</param>
        /// <param name="triggerCommand">сообщения, вызывающие команду</param>
        public BotCommandHandler(
            string name,
            Type conversation,
            string? description = null,
            CommandDisplay displayMode = CommandDisplay.None,
            IEnumerable<string> triggerCommand = null,
            IEnumerable<RolesList> roles = null)
        {
            Name = name;
            Conversation = conversation;
            Description = description;
            Command = SetCommand();
            TriggerCommands = triggerCommand ?? new List<string>();
            DisplayMode = displayMode;
            Roles = roles;
        }

        public BotCommandHandler(string name, string description, Func<Message, Task> triggerAction)
        {
            Name = name;
            Description = description;
            TriggerAction = triggerAction;
            Command = SetCommand();
        }

        private string SetCommand()
        {
            return "/" + Name.ToLower();
        }
    }

    public enum CommandDisplay
    {
        Menu,
        ButtonMenu,
        None,
    }
}
