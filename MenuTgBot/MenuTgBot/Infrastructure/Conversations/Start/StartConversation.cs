using Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace MenuTgBot.Infrastructure.Conversations.Start
{
    internal class StartConversation : IConversation
    {
        private readonly long _chatId;
        private readonly ITelegramBotClient _clientBot;
        private readonly ApplicationContext _dataSource;
        private readonly StateManager _stateManager;

        public StartConversation(ITelegramBotClient botClient, ApplicationContext dataSource, StateManager statesManager)
        {
            _clientBot = botClient;
            _dataSource = dataSource;
            _stateManager = statesManager;
            _chatId = _stateManager.ChatId;
        }


        public async Task<Trigger?> TryNextStepAsync(Message message)
        {
            switch (_stateManager.GetState())
            {
                case State.CommandStart:
                    {
                        await SetRoleAsync();
                        await SetMenuButtonsAsync(message);
                        return Trigger.Ignore;
                    }
            }

            return null;
        }

        public async Task<Trigger?> TryNextStepAsync(CallbackQuery query)
        {
            return null;
        }

        private async Task SetMenuButtonsAsync(Message message)
        {
            await _stateManager.CommandsManager.ShowButtonMenuAsync(_chatId,_stateManager.UserId, StartText.Welcome);
        }

        private async Task SetRoleAsync()
        {
            _stateManager.Roles = await _dataSource.GetUserRoles(_stateManager.UserId);
        }
    }
}
