using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuTgBot.Infrastructure.Models
{
    internal enum Command
    {
        AddToCartFromProduct,
        ClearCart,
        DeleteFromCartFromProduct,
        Pay,
        GoodsDetails,
        BuyNow,
        EditCart,
        GoodsDetailsCart,
        CategoryProducts,
        ProductDetails,
        ReturnToCategory,
        ReturnToCatalog,
        AddToCartFromCategory,
        DeleteFromCartFromCategory,
        Ignore,
        ProductNotAvaliable,
        MovePagination,
        DecreaseCount,
        IncreaseCount,
        TakeOrder,
        ProductCount,
        AddToCartFromCart
    }
}
