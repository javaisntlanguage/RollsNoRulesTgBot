using MenuTgBot.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Telegram.Util.Core.Models;
using Telegram.Util.Core.StateMachine;

namespace MenuTgBot.Infrastructure.Models
{
	internal class MenuBotStateMachineBuilder : StateMachineBuilderBase<State, Trigger>, IMenuBotStateMachineBuilder
	{
		protected override void ConfigureMachine(StateMachine<State, Trigger> machine, Func<Task> messageHandler, Func<Task> queryHandler)
		{
			machine.Configure(State.New)
			.Permit(Trigger.CommandStartStarted, State.CommandStart)
			.Permit(Trigger.CommandShopCatalogStarted, State.CommandShopCatalog)
			.Permit(Trigger.CommandCartStarted, State.CommandCart)
			.Permit(Trigger.CommandOrdersStarted, State.CommandOrders)
			.Ignore(Trigger.Ignore);

			machine.Configure(State.CommandStart)
			.SubstateOf(State.New)
			.OnEntryFromAsync(Trigger.CommandStartStarted, messageHandler);

			machine.Configure(State.CommandShopCatalog)
			.SubstateOf(State.New)
			.PermitReentry(Trigger.ProductAddedToCart)
			.Permit(Trigger.DecreasedCountCart, State.CatalogCartActions)
			.Permit(Trigger.IncreasedCountCart, State.CatalogCartActions)
			.Permit(Trigger.AddToCartFromCategoryShowProduct, State.CatalogCartActions)
			.OnEntryFromAsync(Trigger.CommandShopCatalogStarted, messageHandler);

			machine.Configure(State.CatalogCartActions)
			.SubstateOf(State.CommandShopCatalog)
			.Permit(Trigger.ToCatalogState, State.CommandShopCatalog)
			.OnEntryFromAsync(Trigger.AddToCartFromCategoryShowProduct, queryHandler)
			.OnEntryFromAsync(Trigger.DecreasedCountCart, queryHandler)
			.OnEntryFromAsync(Trigger.IncreasedCountCart, queryHandler);

			machine.Configure(State.CommandCart)
			.SubstateOf(State.New)
			.Permit(Trigger.ClientTookOrder, State.CommandOrders)
			.OnEntryFromAsync(Trigger.CommandCartStarted, messageHandler);

			machine.Configure(State.CommandOrders)
			.SubstateOf(State.New)
			.OnEntryFromAsync(Trigger.ClientTookOrder, queryHandler)
			.OnEntryFromAsync(Trigger.CommandOrdersStarted, messageHandler)
			.Permit(Trigger.EnterAddressCity, State.OrderNewAddressCityEditor)
			.Permit(Trigger.EnterAddressStreet, State.OrderNewAddressStreetEditor)
			.Permit(Trigger.EnterAddressHouseNumber, State.OrderNewAddressHouseNumberEditor)
			.Permit(Trigger.EnterAddressBuilding, State.OrderNewAddressBuildingEditor)
			.Permit(Trigger.EnterAddressFlat, State.OrderNewAddressFlatEditor)
			.Permit(Trigger.EnterAddressComment, State.OrderNewAddressCommentEditor)
			.Permit(Trigger.AddressAddedNextPhone, State.OrderPhone)
			.Permit(Trigger.EmptyPhone, State.OrderPhone)
			.Permit(Trigger.ChangePhone, State.OrderPhone);

			machine.Configure(State.OrderNewAddressCityEditor)
			.SubstateOf(State.CommandOrders);

			machine.Configure(State.OrderNewAddressStreetEditor)
			.SubstateOf(State.CommandOrders);

			machine.Configure(State.OrderNewAddressHouseNumberEditor)
			.SubstateOf(State.CommandOrders);

			machine.Configure(State.OrderNewAddressBuildingEditor)
			.SubstateOf(State.CommandOrders);

			machine.Configure(State.OrderNewAddressFlatEditor)
			.SubstateOf(State.CommandOrders);

			machine.Configure(State.OrderNewAddressCommentEditor)
			.SubstateOf(State.CommandOrders)
			.Permit(Trigger.AddressAddedReturnToDeliverySettings, State.CommandOrders)
			.Permit(Trigger.CancelAddNewAddress, State.CommandOrders);

			machine.Configure(State.OrderPhone)
			.SubstateOf(State.CommandOrders)
			.OnEntryFromAsync(Trigger.AddressAddedNextPhone, queryHandler)
			.OnEntryFromAsync(Trigger.EmptyPhone, queryHandler)
			.Permit(Trigger.SendedSms, State.SmsPhone)
			.Permit(Trigger.BackFromPhone, State.CommandOrders);

			machine.Configure(State.SmsPhone)
			.SubstateOf(State.OrderPhone)
			.Permit(Trigger.EnteredSms, State.CommandOrders);
		}
	}
}
