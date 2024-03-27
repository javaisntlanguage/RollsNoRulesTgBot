using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using Database;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types.ReplyMarkups;
using Newtonsoft.Json;
using Database.Tables;
using System.Diagnostics.Eventing.Reader;
using Newtonsoft.Json.Linq;
using Command = MenuTgBot.Infrastructure.Models.Command;
using Helper;
using Telegram.Bot.Types.Enums;
using Telegram.Util.Core;
using MenuTgBot.Infrastructure.Conversations.Cart;

namespace MenuTgBot.Infrastructure.Conversations.Catalog
{
    internal class CatalogConversation : IConversation
    {
        private readonly long _chatId;
        private readonly ApplicationContext _dataSource;
        private readonly StateManager _stateManager;

        public CatalogConversation(ApplicationContext dataSource, StateManager statesManager)
        {
            _dataSource = dataSource;
            _stateManager = statesManager;
            _chatId = _stateManager.ChatId;
        }

        public async Task<Trigger?> TryNextStepAsync(Message message)
        {
            switch (_stateManager.CurrentState)
            {
                case State.CommandShopCatalog:
                    {
                        await ShowCategoriesAsync();
                        return Trigger.Ignore;
                    }
            }

            return null;
        }

        public async Task<Trigger?> TryNextStepAsync(CallbackQuery query)
        {
            JObject data = JObject.Parse(query.Data);
            Command command = (Command)data["Cmd"].Value<int>();

            switch (_stateManager.CurrentState)
            {
                case State.CommandShopCatalog:
                    {
                        switch (command)
                        {
                            case Command.CategoryProducts:
                            case Command.ReturnToCategory:
                                {
                                    int categoryId = data["CategoryId"].Value<int>();
                                    await ShowCategoryProductsAsync(categoryId);
                                    return Trigger.Ignore;
                                }
                            case Command.ReturnToCatalog:
                                {
                                    await ShowCategoriesAsync();
                                    return Trigger.Ignore;
                                }
                            case Command.MovePagination:
                                {
                                    int productId = data["ProductId"].Value<int>();
                                    await ShowProductDetailsWithProductIdAsync(productId);
                                    return Trigger.Ignore;
                                }
                            case Command.Ignore:
                            case Command.ProductCount:
                                {
                                    return Trigger.Ignore;
                                }
                        }
                        break;
                    }
                case State.CatalogCartActions:
                    {
                        switch(command)
                        {
                            case Command.AddToCartFromCategory:
                                {
                                    int productId = data["ProductId"].Value<int>();
                                    await ShowProductDetailsWithProductIdAsync(productId);
                                    return Trigger.ToCatalogState;
                                }
                            case Command.DecreaseCount:
                                {
                                    int productId = data["ProductId"].Value<int>();
                                    await ShowProductDetailsWithProductIdAsync(productId);
                                    return Trigger.ToCatalogState;
                                }
                            case Command.IncreaseCount:
                                {
                                    int productId = data["ProductId"].Value<int>();
                                    await ShowProductDetailsWithProductIdAsync(productId);
                                    return Trigger.ToCatalogState;
                                }
                        }
                        break;
                    }

            }

            return null;
        }

        private async Task ShowProductDetailsWithProductIdAsync(int productId)
        {
            Product product = await _dataSource.Products
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product.IsNull())
            {
                return;
            }

            await ShowProductDetailsAsync(product);
        }

        private async Task ShowProductDetailsAsync(Product product)
        {
            InlineKeyboardButton[] cartActions = GetCartButtons(product.Id);
            InlineKeyboardButton[] pagination = await GetPagination(product.Id);
            InlineKeyboardButton[] returnToCatalog = GetReturnToCatalogButton();


            List<InlineKeyboardButton[]> keyboard = new List<InlineKeyboardButton[]>
            {
                cartActions,
                returnToCatalog
            };

            if (pagination.IsNotNull())
            {
                keyboard.Insert(1, pagination);
            }

            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(keyboard);

            string text = string.Format(CatalogText.ProductDetails,
                product.Name,
                product.Description,
                product.Price.ToString(TelegramHelper.PRICE_FORMAT));

            await _stateManager.SendMessageAsync(text, ParseMode.Html, markup, product.Photo);
        }

        private async Task ShowCategoryProductsAsync(int categoryId)
        {
            List<CartProduct> cart = _stateManager.GetHandler<CartConversation>()?.Cart ?? new List<CartProduct>();

            Product product = (await _dataSource.ProductCategories
                .Where(pc => pc.CategoryId == categoryId && pc.Category.IsVisible && pc.Product.IsVisible)
                .Include(pc => pc.Product)
                .FirstOrDefaultAsync())?.Product;

            if(product.IsNull())
            {
                InlineKeyboardButton[] returnToCatalog = GetReturnToCatalogButton();
                InlineKeyboardMarkup markup = new InlineKeyboardMarkup(returnToCatalog);

                await _stateManager.SendMessageAsync(CatalogText.ProductsEmpty, replyMarkup: markup);
                
                return;
            }

            await ShowProductDetailsAsync(product);
        }

        /// <summary>
        /// получение пагинации
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        private async Task<InlineKeyboardButton[]> GetPagination(int productId)
        {

            IQueryable<int> categoryProducts = _dataSource.ProductCategories
                .Where(pc => pc.CategoryId == _dataSource.ProductCategories
                        .Select(pc2 => new { pc2.ProductId, pc2.CategoryId })
                        .First(pcpid => pcpid.ProductId == productId)
                        .CategoryId)
                .Select(pc => pc.ProductId);

            if(categoryProducts.Count() == 1)
            {
                return null;
            }

            InlineKeyboardButton buttonPrevious = GetButtonPrevious(productId, categoryProducts);
            InlineKeyboardButton buttonNext = GetButtonNext(productId, categoryProducts);

            InlineKeyboardButton[] result = new InlineKeyboardButton[] 
            { 
                buttonPrevious, 
                buttonNext 
            };

            return result;
        }

        /// <summary>
        /// получение кнопки для пагинации вперед
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="categoryProducts"></param>
        /// <returns></returns>
        private InlineKeyboardButton GetButtonNext(int productId, IQueryable<int> categoryProducts)
        {
            InlineKeyboardButton result = null;
            IQueryable<int> nextProductIds = categoryProducts
                .Where(pcid => pcid > productId);

            if (nextProductIds.Any())
            {
                int nextId = nextProductIds
                    .Min();

                result = new InlineKeyboardButton(CartText.PaginationNext)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.MovePagination,
                        ProductId = nextId
                    })
                };
            }
            else
            {
                result = GetNoPaginationButton();
            }

            return result;
        }

        /// <summary>
        /// кнопка пустой пагинации
        /// </summary>
        /// <returns></returns>
        private InlineKeyboardButton GetNoPaginationButton()
        {
            InlineKeyboardButton result = new InlineKeyboardButton(CartText.NoPagination)
            {
                CallbackData = JsonConvert.SerializeObject(new
                {
                    Cmd = Command.Ignore
                })
            };

            return result;
        }

        /// <summary>
        /// получение кнопки для пагинации назад
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="categoryProducts"></param>
        /// <returns></returns>
        private InlineKeyboardButton GetButtonPrevious(int productId, IQueryable<int> categoryProducts)
        {
            InlineKeyboardButton result = null;
            IQueryable<int> previousProductIds = categoryProducts
                .Where(pcid => pcid < productId);

            if (previousProductIds.Any())
            {
                int previousId = previousProductIds
                    .Max();

                result = new InlineKeyboardButton(CartText.PaginationPrevious)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.MovePagination,
                        ProductId = previousId
                    })
                };
            }
            else
            {
                result = GetNoPaginationButton();
            }

            return result;
        }

        private InlineKeyboardButton[] GetCartButtons(int productId)
        {
            InlineKeyboardButton[] result = null;
            List<CartProduct> cart = _stateManager.GetHandler<CartConversation>()?.Cart ?? new List<CartProduct>();
            CartProduct cartProduct = cart.FirstOrDefault(cartProduct => cartProduct.Id == productId);
            if (cartProduct.IsNull())
            {
                result = GetAddToCartButton(productId);
            }
            else
            {
                result = new InlineKeyboardButton[]
                {
                    new InlineKeyboardButton(CartText.DecreaseCount)
                    {
                        CallbackData = JsonConvert.SerializeObject(new
                        {
                            Cmd = Command.DecreaseCount,
                            ProductId = productId,
                        })
                    },
                    new InlineKeyboardButton(cartProduct.Count.ToString())
                    {
                        CallbackData = JsonConvert.SerializeObject(new
                        {
                            Cmd = Command.ProductCount
                        })
                    },
                    new InlineKeyboardButton(CartText.IncreaseCount)
                    {
                        CallbackData = JsonConvert.SerializeObject(new
                        {
                            Cmd = Command.IncreaseCount,
                            ProductId = cartProduct.Id,
                        })
                    }
                };
            }

            return result;
        }

        private InlineKeyboardButton[]? GetAddToCartButton(int productId)
        {
            InlineKeyboardButton[] result = new InlineKeyboardButton[]
            {
                new InlineKeyboardButton(CartText.AddToCart)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.AddToCartFromCategory,
                        ProductId = productId,
                    })
                }
            };

            return result;
        }

        /// <summary>
        /// получение кнопки возврата в выбор категории
        /// </summary>
        /// <returns></returns>
        private InlineKeyboardButton[] GetReturnToCatalogButton()
        {
            InlineKeyboardButton[] result = new InlineKeyboardButton[]{
                        new InlineKeyboardButton(CatalogText.ReturnToCatalog)
                        {
                            CallbackData = JsonConvert.SerializeObject(new
                            {
                                Cmd = Command.ReturnToCatalog,
                            })
                        }
                    };

            return result;
        }

        private async Task ShowCategoriesAsync()
        {
            InlineKeyboardMarkup markup = new(await _dataSource.Categories
                .Where(category => category.IsVisible)
                .Select(category => new InlineKeyboardButton[]
                {
                    new(category.Name)
                    {
                        CallbackData = JsonConvert.SerializeObject(new
                        {
                            Cmd = Command.CategoryProducts,
                            CategoryId = category.Id,
                        })
                    }
                })
                .ToArrayAsync());

            if (markup.InlineKeyboard.Any())
            {
                await _stateManager.SendMessageAsync(CatalogText.ChooseCategory, replyMarkup: markup);
            }
            else
            {
                await _stateManager.SendMessageAsync(CatalogText.ProductsEmpty);
            }
        }
    }
}
