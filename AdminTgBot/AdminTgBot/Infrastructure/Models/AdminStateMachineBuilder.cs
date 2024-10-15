using AdminTgBot.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Telegram.Util.Core.Interfaces;
using Telegram.Util.Core.Models;
using Telegram.Util.Core.StateMachine;

namespace AdminTgBot.Infrastructure.Models
{
	public class AdminStateMachineBuilder : StateMachineBuilderBase<State, Trigger>, IAdminStateMachineBuilder
	{
		protected override void ConfigureMachine(StateMachine<State, Trigger> machine, Func<Task> messageHandler)
		{
			machine.Configure(State.New)
			.Permit(Trigger.CommandButtonsStarted, State.CommandStart)
			.Permit(Trigger.CommandStartStarted, State.CommandStart)
			.Permit(Trigger.CommandCatalogEditorStarted, State.CommandCatalogEditor)
			.Permit(Trigger.CommandOrdersStarted, State.CommandOrders)
			.Permit(Trigger.CommandBotOwnerStarted, State.CommandBotOwner)
			.Permit(Trigger.CommandLkkStarted, State.CommandLkk)
			.Permit(Trigger.CommandAdministrationStarted, State.CommandAdministration)
			.Ignore(Trigger.Ignore);

			machine.Configure(State.CommandButtons)
			.SubstateOf(State.New)
			.OnEntryFromAsync(Trigger.CommandButtonsStarted, messageHandler)
			.Ignore(Trigger.Ignore);

			machine.Configure(State.CommandStart)
			.SubstateOf(State.New)
			.Permit(Trigger.EnterLogin, State.StartLogin)
			.Permit(Trigger.EnterPassword, State.StartPassword)
			.OnEntryFromAsync(Trigger.CommandStartStarted, messageHandler);

			machine.Configure(State.StartLogin)
			.SubstateOf(State.CommandStart);

			machine.Configure(State.StartPassword)
			.SubstateOf(State.StartLogin)
			.Permit(Trigger.EndOfConversation, State.New);

			machine.Configure(State.CommandCatalogEditor)
			.SubstateOf(State.New)
			.Permit(Trigger.EditProductName, State.ProductNameEditor)
			.Permit(Trigger.EditProductDescription, State.ProductDescriptionEditor)
			.Permit(Trigger.EditProductPrice, State.ProductPriceEditor)
			.Permit(Trigger.EditProductPhoto, State.ProductPhotoEditor)
			.Permit(Trigger.EnterProductName, State.NewProductNameEditor)
			.Permit(Trigger.EnterProductDescription, State.NewProductDescriptionEditor)
			.Permit(Trigger.EnterProductPrice, State.NewProductPriceEditor)
			.Permit(Trigger.EnterProductPhoto, State.NewProductPhotoEditor)
			.Permit(Trigger.EditCategoryName, State.CategoryNameEditor)
			.Permit(Trigger.EnterCategoryName, State.NewCategoryNameEditor)
			.OnEntryFromAsync(Trigger.CommandCatalogEditorStarted, messageHandler);

			machine.Configure(State.ProductNameEditor)
			.SubstateOf(State.CommandCatalogEditor)
			.Permit(Trigger.ReturnToProductAttributeEditor, State.CommandCatalogEditor);

			machine.Configure(State.ProductDescriptionEditor)
			.SubstateOf(State.CommandCatalogEditor)
			.Permit(Trigger.ReturnToProductAttributeEditor, State.CommandCatalogEditor);

			machine.Configure(State.ProductPriceEditor)
			.SubstateOf(State.CommandCatalogEditor)
			.Permit(Trigger.ReturnToProductAttributeEditor, State.CommandCatalogEditor);

			machine.Configure(State.ProductPhotoEditor)
			.SubstateOf(State.CommandCatalogEditor)
			.Permit(Trigger.ReturnToProductAttributeEditor, State.CommandCatalogEditor);

			machine.Configure(State.NewProductNameEditor)
			.SubstateOf(State.CommandCatalogEditor);

			machine.Configure(State.NewProductDescriptionEditor)
			.SubstateOf(State.CommandCatalogEditor);

			machine.Configure(State.NewProductPriceEditor)
			.SubstateOf(State.CommandCatalogEditor);

			machine.Configure(State.NewProductPhotoEditor)
			.SubstateOf(State.CommandCatalogEditor)
			.Permit(Trigger.ReturnToCatalog, State.CommandCatalogEditor);

			machine.Configure(State.CategoryNameEditor)
			.SubstateOf(State.CommandCatalogEditor)
			.Permit(Trigger.ReturnToCatalogEditor, State.CommandCatalogEditor);

			machine.Configure(State.NewCategoryNameEditor)
			.SubstateOf(State.CommandCatalogEditor)
			.Permit(Trigger.ReturnToCatalogEditor, State.CommandCatalogEditor);

			machine.Configure(State.CommandOrders)
			.SubstateOf(State.New)
			.OnEntryFromAsync(Trigger.CommandOrdersStarted, messageHandler)
			.Permit(Trigger.WaitForDateFrom, State.FilterDateFrom)
			.Permit(Trigger.WaitForDateTo, State.FilterDateTo)
			.Permit(Trigger.WaitForOrderFilterId, State.FilterId);

			machine.Configure(State.FilterDateFrom)
			.SubstateOf(State.CommandOrders)
			.Permit(Trigger.BackToOrderFilter, State.CommandOrders);

			machine.Configure(State.FilterDateTo)
			.SubstateOf(State.CommandOrders)
			.Permit(Trigger.BackToOrderFilter, State.CommandOrders);

			machine.Configure(State.FilterId)
			.SubstateOf(State.CommandOrders)
			.Permit(Trigger.BackToOrderFilter, State.CommandOrders);

			machine.Configure(State.CommandBotOwner)
			.SubstateOf(State.New)
			.OnEntryFromAsync(Trigger.CommandBotOwnerStarted, messageHandler)
			.Permit(Trigger.SelectMenu, State.BotOwnerSelectMenu);

			machine.Configure(State.BotOwnerSelectMenu)
			.SubstateOf(State.CommandBotOwner)
			.OnEntryFromAsync(Trigger.SelectMenu, messageHandler)
			.Permit(Trigger.EnterLogin, State.BotOwnerEnterLogin)
			.PermitReentry(Trigger.SelectMenu);

			machine.Configure(State.BotOwnerEnterLogin)
			.SubstateOf(State.CommandBotOwner)
			.Permit(Trigger.EnterPassword, State.BotOwnerEnterPassword)
			.Permit(Trigger.SelectMenu, State.BotOwnerSelectMenu);

			machine.Configure(State.BotOwnerEnterPassword)
			.SubstateOf(State.CommandBotOwner)
			.Permit(Trigger.EnterAdminName, State.BotOwnerEnterAdminName)
			.Permit(Trigger.SelectMenu, State.BotOwnerSelectMenu);

			machine.Configure(State.BotOwnerEnterAdminName)
			.SubstateOf(State.CommandBotOwner)
			.Permit(Trigger.SelectMenu, State.BotOwnerSelectMenu);

			machine.Configure(State.CommandLkk)
			.SubstateOf(State.New)
			.OnEntryFromAsync(Trigger.CommandLkkStarted, messageHandler)
			.Permit(Trigger.SuggestEditName, State.LkkEnterName)
			.Permit(Trigger.SuggestEditPassword, State.LkkEnterOldPassword);

			machine.Configure(State.LkkEnterName)
			.SubstateOf(State.CommandLkk)
			.Permit(Trigger.BackToLkk, State.CommandLkk);

			machine.Configure(State.LkkEnterOldPassword)
			.SubstateOf(State.CommandLkk)
			.Permit(Trigger.EnterNewPassword, State.LkkEnterNewPassword);

			machine.Configure(State.LkkEnterNewPassword)
			.SubstateOf(State.LkkEnterOldPassword)
			.Permit(Trigger.SuggestConfirmNewAdminPassword, State.LkkConfirmNewPassword);

			machine.Configure(State.LkkConfirmNewPassword)
			.SubstateOf(State.LkkEnterNewPassword)
			.Permit(Trigger.BackToLkk, State.CommandLkk);

			machine.Configure(State.CommandAdministration)
			.SubstateOf(State.New)
			.OnEntryFromAsync(Trigger.CommandAdministrationStarted, messageHandler);
		}
	}
}
