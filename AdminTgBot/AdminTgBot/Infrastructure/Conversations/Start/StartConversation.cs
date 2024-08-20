using Database;
using Database.Classes;
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
        private ApplicationContext _dataSource;
        private readonly AdminBotStateManager _stateManager;

        public string? Login { get; set; }
        public int? AdminId { get; set; }

        public StartConversation() { }

        public StartConversation(AdminBotStateManager statesManager)
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
                        await GetLoginAsync();
                        return Trigger.EnterLogin;
                    }
                case State.StartLogin:
                    {
                        await GetPassowrdAsync(message.Text!);
                        return Trigger.EnterPassword;
                    }
                case State.StartPassword:
                    {
                        await AuthAsync(message);
                        return Trigger.EndOfConversation;
                    }
            }

            return null;
        }

        public Task<Trigger?> TryNextStepAsync(ApplicationContext dataSource, CallbackQuery query)
        {
            return null;
        }

        private async Task AuthAsync(Message message)
        {
            string? messageText = message.Text;
            _stateManager.CheckText(messageText);

            await _stateManager.DeleteMessageAsync(message);

            string passwordHash = DBHelper.GetPasswordHash(messageText!);
            AdminCredential? admin = await _dataSource.AdminCredentials.FirstOrDefaultAsync(ac => ac.Login == Login && ac.PasswordHash == passwordHash);
            if (admin == null)
            {
                Login = null;
                await _stateManager.SendMessageAsync(StartText.AuthFail);
            }
            else
            {
                AdminId = admin.Id;
                _stateManager.IsAuth = true;
                _stateManager.AdminId = AdminId;
                await SetMenuButtonsAsync(admin.Name);
            }
        }

        private async Task GetPassowrdAsync(string login)
        {
            Login = login;
            await _stateManager.SendMessageAsync(StartText.EnterPassword);
        }
        private async Task GetLoginAsync()
        {
            _stateManager.IsAuth = false;
            await _stateManager.SendMessageAsync(StartText.EnterLogin);
        }

        private async Task SetMenuButtonsAsync(string name)
        {
            string text = string.Format(StartText.AuthSuccess, name);

            await _stateManager.ShowButtonMenuAsync(text);
        }
    }
}
