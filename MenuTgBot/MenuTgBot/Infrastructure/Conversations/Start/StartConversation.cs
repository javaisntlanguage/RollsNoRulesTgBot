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
        private readonly ApplicationContext _dataSource;
        private readonly StateManager _stateManager;

        public StartConversation(ApplicationContext dataSource, StateManager statesManager)
        {
            _dataSource = dataSource;
            _stateManager = statesManager;
            _chatId = _stateManager.ChatId;
        }


        public async Task<Trigger?> TryNextStepAsync(Message message)
        {
            switch (_stateManager.CurrentState)
            {
                case State.CommandStart:
                    {
                        SetRole();
                        await SetMenuButtonsAsync();
                        return Trigger.Ignore;
                    }
            }

            return null;
        }

        public async Task<Trigger?> TryNextStepAsync(CallbackQuery query)
        {
            return null;
        }

        private async Task SetMenuButtonsAsync()
        {
            await _stateManager.ShowButtonMenuAsync(StartText.Welcome);
        }

        private void SetRole()
        {
            _stateManager.Roles = _dataSource
                .GetUserRoles(_stateManager.ChatId)
                .ToHashSet();
        }
    }
}
