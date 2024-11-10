using DependencyInjection.Inferfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Util.Core.Interfaces;

namespace MenuTgBotConsole.Models
{
	public class ApplicationRunner : IApplicationRunner
	{
		private readonly ITelegramWorker _telegramWorker;

		public ApplicationRunner(ITelegramWorker telegramWorker)
		{
			_telegramWorker = telegramWorker;
		}

		public void Run()
		{
			_telegramWorker.Start();
		}
	}
}
