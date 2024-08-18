using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdminTgBot.Infrastructure.Models
{
    internal enum Command
    {
        Ignore,
        ShowCategoryProducts,
        AddProduct,
		BackToCategory,
        EditProduct,
        MovePaginationProduct,
        EditProductAttribute,
        ReturnToProduct,
        EditProductAttributeYes,
        EditProductVisibility,
        AcceptOrder,
        DeclineOrder,
		OrderDetails,
		MovePaginationOrders,
		ChangeFilter,
		ChangeFilterState,
		ChangeFilterDateFrom,
		ChangeFilterDateTo,
		ChangeFilterId,
		BackToOrders,
		ChangeFilterStateValue,
		ChangeFilterDateReset,
		ChangeFilterIdReset,
		ChangeFilterDateSuggestion,
		ShowCategoryActions,
		EditCategory,
		AddCategory,
		MovePaginationCategories,
		BackToCategories,
		EditCategoryAttribute,
		EditCategoryVisibility,
		EditCategoryAttributeYes,
		AddSuperAdmin,
		ResetPasswordSuperAdmin,
		AddSuperAdminConfirm,
		BackToMenuActions,
		ResetPasswordSuperAdminConcrete,
		EditName,
		EditPassword,
		ForgotPassword
	}
}
