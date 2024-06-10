using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Telegram.Util.Core.StateMachine.Graph;

namespace AdminTgBot.Infrastructure.Conversations.Orders.Models
{
	internal class OrdersFilter
	{
		public const string DATE_FORMAT = "dd.MM.yyyy";

		public int? Id { get; set; }
		public bool? IsNew { get; set; }
		public bool? IsApproved { get; set; }
		public bool? IsCompleted { get; set; }
		public bool? IsDeclined { get; set; }
		public bool? IsError { get; set; }
		public DateTimeOffset? DateFrom { get; set; }
		public DateTimeOffset? DateTo { get; set; }
		[JsonIgnore]
		public bool HasStateFilter => IsNew.HasValue ||
					IsApproved.HasValue ||
					IsCompleted.HasValue ||
					IsDeclined.HasValue ||
					IsError.HasValue;

		public override string ToString()
		{
			StringBuilder result = new StringBuilder();

			if (!Id.HasValue &&
				!HasStateFilter &&
				!DateFrom.HasValue &&
				!DateTo.HasValue)
			{
				result.AppendLine(OrdersText.FilterEmpty);
			}
			else
			{

				if (Id.HasValue)
				{
					result.AppendLine(string.Format(OrdersText.OrdersFilterId, Id));
				}
				if (DateFrom.HasValue || DateTo.HasValue)
				{
					result.Append(OrdersText.OrdersFilterDate);
					if (DateFrom.HasValue)
					{
						result.Append($" {string.Format(OrdersText.OrdersFilterDateFrom, DateFrom.Value.ToString(DATE_FORMAT))}");

						if (DateTo.HasValue)
						{
							result.Append($" {string.Format(OrdersText.OrdersFilterDateTo, DateTo.Value.ToString(DATE_FORMAT))}");
						}
					}
					else
					{
						result.Append($" {string.Format(OrdersText.OrdersFilterDateTo, DateTo.Value.ToString(DATE_FORMAT))}");
					}
					result.AppendLine();
				}
				if (HasStateFilter)
				{
					StringBuilder states = new StringBuilder();
					states.AppendLine($"{OrdersText.OrdersStates}:");

					if (IsNew ?? false)
					{
						states.AppendLine($"- {OrdersText.OrderStateNew}");
					}
					if (IsApproved ?? false)
					{
						states.AppendLine($"- {OrdersText.OrderStateDeclined}");
					}
					if (IsCompleted ?? false)
					{
						states.AppendLine($"- {OrdersText.OrderStateCompleted}");
					}
					if (IsDeclined ?? false)
					{
						states.AppendLine($"- {OrdersText.OrderStateDeclined}");
					}
					if (IsError ?? false)
					{
						states.AppendLine($"- {OrdersText.OrderStateError}");
					}

					result.AppendLine(states.ToString());
				}
			}

			return result.ToString();
		}
	}
}
