﻿using Database;
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
using Telegram.Util.Core.Interfaces;

namespace MenuTgBot.Infrastructure.Conversations.Start
{
    internal class StartConversation : IConversation
    {
        private readonly long _chatId;
        private readonly MenuBotStateManager _stateManager;
        private ApplicationContext _dataSource;

        public StartConversation() { }

		public StartConversation(MenuBotStateManager statesManager)
        {
            _stateManager = statesManager;
            _chatId = _stateManager.ChatId;
        }


        public async Task<Trigger?> TryNextStepAsync(ApplicationContext dataSource, Message message)
        {
            _dataSource = dataSource;

            switch (_stateManager.CurrentState)
            {
                case State.CommandStart:
                    {
                        await SetMenuButtonsAsync();
                        return Trigger.CommandShopCatalogStarted;
                    }
            }

            return null;
        }

        public async Task<Trigger?> TryNextStepAsync(ApplicationContext dataSource, CallbackQuery query)
        {
            return null;
        }

        private async Task SetMenuButtonsAsync()
        {
            await _stateManager.ShowButtonMenuAsync(StartText.Welcome);
        }
    }
}
