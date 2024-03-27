using Database;
using Database.Enums;
using Database.Tables;
using Helper;
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

namespace AdminTgBot.Infrastructure.Conversations.Start
{
    internal class StartConversation : IConversation
    {
        private readonly long _chatId;
        private readonly ITelegramBotClient _clientBot;
        private readonly ApplicationContext _dataSource;
        private readonly StateManager _stateManager;

        public string Login { get; set; }

        public StartConversation() { }

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
                        await GetLoginAsync();
                        return Trigger.EnterLogin;
                    }
                case State.StartLogin:
                    {
                        await GetPassowrdAsync(message.Text);
                        return Trigger.EnterPassword;
                    }
                case State.StartPassword:
                    {
                        await AuthAsync(message.Text);
                        return Trigger.EndOfConversation;
                    }
            }

            return null;
        }

        public async Task<Trigger?> TryNextStepAsync(CallbackQuery query)
        {
            return null;
        }

        private async Task AuthAsync(string password)
        {
            string passwordHash = DBHelper.GetPasswordHash(password);
            AdminCredential admin = await _dataSource.AdminCredentials.FirstOrDefaultAsync(ac => ac.Login == Login && ac.PasswordHash == passwordHash);
            if (admin.IsNull())
            {
                Login = null;
                await _clientBot.SendTextMessageAsync(_chatId, StartText.AuthFail);
            }
            else
            {
                _stateManager.IsAuth = true;
                SetRole();
                await SetMenuButtonsAsync(admin.Name);
            }
        }

        private async Task GetPassowrdAsync(string login)
        {
            Login = login;
            await _clientBot.SendTextMessageAsync(_chatId, StartText.EnterPassword);
        }
        private async Task GetLoginAsync()
        {
            _stateManager.IsAuth = false;
            await _clientBot.SendTextMessageAsync(_chatId, StartText.EnterLogin);
        }

        private async Task SetMenuButtonsAsync(string name)
        {
            string text = string.Format(StartText.AuthSuccess, name);

            await _stateManager.CommandsManager.ShowButtonMenuAsync(_chatId,_stateManager.UserId, text);
        }

        private void SetRole()
        {
            _stateManager.Roles = _dataSource
                .GetUserRoles(_stateManager.UserId)
                .ToHashSet();
            
            if(!_stateManager.Roles.Contains(RolesList.Admin))
            {
                _stateManager.Roles.Add(RolesList.Admin);
            }
        }
    }
}
