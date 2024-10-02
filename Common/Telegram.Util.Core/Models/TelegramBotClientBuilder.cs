using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Util.Core.Interfaces;

namespace Telegram.Util.Core.Models
{
	public class TelegramBotClientBuilder : ITelegramBotClientBuilder
	{
		private TelegramSettings _config;
		private HttpClient _httpClient;

		public TelegramBotClientBuilder(IOptions<TelegramSettings> options, IHttpClientFactory httpClientFactory) 
		{
			_config = options.Value;
			_httpClient = httpClientFactory.CreateClient("TelegramBotClient");
		}
		public ITelegramBotClient Build()
		{
			return new TelegramBotClient(_config.TelegramBotToken, _httpClient);
		}
	}
}
