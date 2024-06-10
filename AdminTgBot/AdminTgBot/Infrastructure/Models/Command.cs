using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdminTgBot.Infrastructure.Models
{
    internal enum Command
    {
        MovePaginationCategory,
        Ignore,
        ShowCategoryProducts,
        AddProduct,
        ReturnToCatalog,
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
		BackToOrders
	}
}
