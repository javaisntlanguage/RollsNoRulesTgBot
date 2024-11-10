using Database;
using Database.Tables;
using MenuTgBot.Infrastructure.Conversations.Catalog;
using MenuTgBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Util.Core;
using Telegram.Bot.Types.Enums;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Telegram.Util.Core.Interfaces;
using Telegram.Util.Core.Extensions;

namespace MenuTgBot.Infrastructure.Conversations.Cart
{
    internal class CartConversation : IConversation
    {
        private readonly long _chatId;
        private readonly MenuBotStateManager _stateManager;
        private ApplicationContext _dataSource;

        public List<CartProduct> Cart {  get; set; }

        public CartConversation() { }
        public CartConversation(MenuBotStateManager statesManager)
        {
            _stateManager = statesManager;
            _chatId = _stateManager.ChatId;

            Cart = new List<CartProduct>();
        }

        public async Task<Trigger?> TryNextStepAsync(ApplicationContext dataSource, Message message)
        {
            _dataSource = dataSource;
            switch (_stateManager.CurrentState)
            {
                case State.CommandCart:
                    {
                        if (message.Text == MessagesText.CommandCart)
                        {
                            await ShowCartAsync(1);
                            return Trigger.Ignore;
                        }
                        break;
                    }
            }

            return null;
        }

        public async Task<Trigger?> TryNextStepAsync(ApplicationContext dataSource, CallbackQuery query)
        {
            _dataSource = dataSource;
            JObject data = JObject.Parse(query.Data);
            Command command = (Command)data["Cmd"].Value<int>();

            switch (_stateManager.CurrentState)
            {
                case State.CommandShopCatalog:
                    {
                        switch (command)
                        {
                            case Command.AddToCartFromCategory:
                                {
                                    int productId = data["ProductId"].Value<int>();
                                    AddToCart(productId);
                                    return Trigger.AddToCartFromCategoryShowProduct;
                                }
                            case Command.DeleteFromCartFromProduct:
                                {
                                    int productId = data["ProductId"].Value<int>();
                                    await DeleteFromCartFromProductAsync(productId, query.Message);
                                    return Trigger.Ignore;
                                }
                            case Command.DeleteFromCartFromCategory:
                                {
                                    int productId = data["ProductId"].Value<int>();
                                    await DeleteFromCartFromCategoryAsync(productId, query.Message);
                                    return Trigger.Ignore;
                                }
                            case Command.ProductNotAvaliable:
                                {
                                    await _stateManager.SendMessageAsync(CartText.ProductNotAvaliable);
                                    return Trigger.Ignore;
                                }
                            case Command.DecreaseCount:
                                {
                                    int productId = data["ProductId"].Value<int>();
                                    DecreaseCountCart(productId);
                                    return Trigger.DecreasedCountCart;
                                }
                            case Command.IncreaseCount:
                                {
                                    int productId = data["ProductId"].Value<int>();
                                    IncreaseCountCart(productId);
                                    return Trigger.IncreasedCountCart;
                                }
                        }

                        break;
                    }
                case State.CommandCart:
                    {
                        switch (command)
                        {
                            case Command.DecreaseCount:
                                {
                                    int productId = data["ProductId"].Value<int>();
                                    int page = data["Page"].Value<int>();
                                    await DecreaseCountAsync(productId, page);
                                    return Trigger.Ignore;
                                }
                            case Command.IncreaseCount:
                                {
                                    int productId = data["ProductId"].Value<int>();
                                    int page = data["Page"].Value<int>();
                                    await IncreaseCountAsync(productId, page);
                                    return Trigger.Ignore;
                                }
                            case Command.AddToCartFromCart:
                                {
                                    int productId = data["ProductId"].Value<int>();
                                    int position = data["Position"].Value<int>();
                                    await AddToCartFromCartAsync(productId, position, query.Message);
                                    return Trigger.Ignore;
                                }
                            case Command.MovePagination:
                                {
                                    int cartElement = data["CartElement"].Value<int>();
                                    await ShowCartAsync(cartElement);
                                    return Trigger.Ignore;
                                }
                            case Command.Ignore:
                            case Command.ProductCount:
                                {
                                    return Trigger.Ignore;
                                }
                            case Command.TakeOrder:
                                {
                                    return Trigger.ClientTookOrder;
                                }
                        }

                        break;
                    }
            }

            return null;
        }

        private CartProduct AddToCart(int productId)
        {
            CartProduct productInCart = Cart.FirstOrDefault(product => product.Id == productId);

            if (productInCart.IsNull())
            {
                productInCart = new CartProduct(productId);
                Cart.Add(productInCart);
            }
            else
            {
                productInCart.Count++;
            }

            return productInCart;
        }

        private void IncreaseCountCart(int productId)
        {
            CartProduct cartProduct = Cart.First(product => product.Id == productId);
            cartProduct.Count++;
        }

        private void DecreaseCountCart(int productId)
        {
            CartProduct cartProduct = Cart.First(product => product.Id == productId);

            if (cartProduct.Count == 1)
            {
                cartProduct.Count = 0;
                Cart.Remove(cartProduct);
            }
            else
            {
                cartProduct.Count--;
            }
        }

        private InlineKeyboardMarkup GetCartMarkup(CartProduct cartProduct, Product product, int? page = null)
        {
            bool forDelete = page.IsNotNull();
            page ??= Cart.IndexOf(cartProduct) + 1;
            InlineKeyboardButton[] pagination = GetPagination(page.Value, forDelete);
            InlineKeyboardButton[] productCountInfo = GetProductCountInfo(cartProduct, page.Value, product.IsVisible);
            InlineKeyboardButton[] takeOrderButton = GetTakeOrderButton(forDelete);

            List<InlineKeyboardButton[]> keyboard = new List<InlineKeyboardButton[]>
            {
                productCountInfo
            };

            if (pagination.IsNotNull())
            {
                keyboard.Add(pagination);
            }

            if (takeOrderButton.IsNotNull())
            {
                keyboard.Add(takeOrderButton);
            }

            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(keyboard);

            return markup;
        }

        private async Task AddToCartFromCartAsync(int productId, int position, Message message)
        {
            CartProduct cartProduct = new CartProduct(productId);
            Product product = await _dataSource.Products
                .FirstOrDefaultAsync(product => product.Id == productId);

            if (product.IsVisible)
            {
                Cart.Insert(position, cartProduct);
            }

            int page = position + 1;

            await ShowCartAsync(page);
        }

        private async Task DecreaseCountAsync(int productId, int page)
        {
            CartProduct cartProduct = Cart.First(product => product.Id == productId);

            if (cartProduct.Count == 1)
            {
                cartProduct.Count = 0;
                await ShowCartAsync(cartProduct, page);
                Cart.Remove(cartProduct);
            }
            else
            {
                await ChangeCountAsync(productId, page, increase: false, cartProduct: cartProduct);
            }
        }

        private async Task IncreaseCountAsync(int productId, int page)
        {
            await ChangeCountAsync(productId, page, increase: true);
        }

        private InlineKeyboardButton[] GetAddToCartButton(int productId, int index)
        {
            InlineKeyboardButton[] result = new InlineKeyboardButton[]
            {
                new InlineKeyboardButton(CartText.AddToCart)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.AddToCartFromCart,
                        ProductId = productId,
                        Position = index
                    })
                }
            };

            return result;
        }

        private async Task ChangeCountAsync(int productId, int page, bool increase, CartProduct cartProduct = null)
        {
            cartProduct ??= Cart.First(product => product.Id == productId);
            cartProduct.Count = increase ? cartProduct.Count + 1 : cartProduct.Count - 1;

            await ShowCartAsync(page);
        }

        private async Task ShowCartAsync(CartProduct cartProduct, int page)
        {
            Product product = await _dataSource.Products
                .FirstOrDefaultAsync(product => product.Id == cartProduct.Id);

            InlineKeyboardMarkup markup = GetCartMarkup(cartProduct, product, page);

            string totalSum = Cart
                .Sum(cp =>
                {
                    Product? product = _dataSource.Products
                    .FirstOrDefault();

                    if (product.IsNull())
                    {
                        return 0m;
                    }

                    return product.Price * cp.Count;
                })
                .ToString(TelegramHelper.PRICE_FORMAT);

            string text = string.Format(CartText.CartZeroCountProductDetails,
                product.Name,
                product.Description,
                product.Price.ToString(TelegramHelper.PRICE_FORMAT),
                totalSum);

            await _stateManager.SendMessageAsync(text, markup, ParseMode.Html, product.Photo);


        }
        private async Task ShowCartAsync(int page)
        {
            if(Cart.IsEmpty())
            {
                await _stateManager.SendMessageAsync(CartText.CartEmpty);
                return;
            }

            CartProduct cartProduct = Cart[page - 1];
            Product product = await _dataSource.Products
                .FirstOrDefaultAsync(product => product.Id == cartProduct.Id);

            InlineKeyboardMarkup markup = GetCartMarkup(cartProduct, product);

            string productSum = (product.Price * cartProduct.Count).ToString(TelegramHelper.PRICE_FORMAT);
            string totalSum = Cart
                .Sum(cp =>
                {
                    Product? product = _dataSource.Products
                    .FirstOrDefault(p => p.Id == cp.Id);

                    if(product.IsNull())
                    {
                        return 0m;
                    }

                    return product.Price * cp.Count;
                })
                .ToString(TelegramHelper.PRICE_FORMAT);

            string text = string.Format(CartText.CartProductDetails,
                product.Name,
                product.Description,
                product.Price.ToString(TelegramHelper.PRICE_FORMAT),
                cartProduct.Count,
                productSum,
                totalSum);

            await _stateManager.SendMessageAsync(text, markup, ParseMode.Html, product.Photo);
        }

        private InlineKeyboardButton[] GetTakeOrderButton(bool forDelete)
        {
            if (Cart.Count == 1 && forDelete)
            {
                return null;
            }

            InlineKeyboardButton[] result = new InlineKeyboardButton[]
                {
                    new InlineKeyboardButton(CartText.TakeOrder)
                    {
                        CallbackData = JsonConvert.SerializeObject(new
                        {
                            Cmd = Command.TakeOrder
                        })
                    },
                };

            return result;
        }

        private InlineKeyboardButton[] GetProductCountInfo(CartProduct cartProduct, int page, bool isVisible)
        {
            InlineKeyboardButton[] result = null;

            if (isVisible)
            {
                if (cartProduct.Count > 0)
                {
                    result = new InlineKeyboardButton[]
                    {
                    new InlineKeyboardButton(CartText.DecreaseCount)
                    {
                        CallbackData = JsonConvert.SerializeObject(new
                        {
                            Cmd = Command.DecreaseCount,
                            ProductId = cartProduct.Id,
                            Page = page,
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
                            Page = page,
                        })
                    }
                    };
                }
                else
                {
                    int index = page - 1;
                    result = GetAddToCartButton(cartProduct.Id, index);
                }
            }
            else
            {
                result = new InlineKeyboardButton[]
                {
                    new InlineKeyboardButton(CartText.ProductNotAvaliable)
                    {
                        CallbackData = JsonConvert.SerializeObject(new
                        {
                            Cmd = Command.Ignore
                        })
                    }
                };
            }

            return result;
        }

        private InlineKeyboardButton[] GetPagination(int currentPage, bool forDelete = false)
        {
            if(Cart.Count == 1)
            {
                return null;
            }

            int nextButtonPage = currentPage;

            InlineKeyboardButton buttonPrevious = GetButtonPrevious(currentPage);
            InlineKeyboardButton buttonNext = GetButtonNext(nextButtonPage, forDelete);

            InlineKeyboardButton[] result = new InlineKeyboardButton[]
            {
                buttonPrevious,
                buttonNext
            };

            return result;
        }

        private InlineKeyboardButton GetButtonNext(int currentPage, bool forDetele)
        {
            InlineKeyboardButton result = null;
            int nextPage = forDetele ? currentPage : currentPage + 1;

            if (currentPage == Cart.Count)
            {
                result = new InlineKeyboardButton(CartText.NoPagination)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.Ignore
                    })
                };
            }
            else
            {
                result = new InlineKeyboardButton(CartText.PaginationNext)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.MovePagination,
                        CartElement = nextPage
                    })
                };
            }

            return result;
        }

        private InlineKeyboardButton GetButtonPrevious(int currentPage)
        {
            InlineKeyboardButton result = null;

            if (currentPage == 1)
            {
                result = new InlineKeyboardButton(CartText.NoPagination)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.Ignore
                    })
                };
            }
            else
            {
                result = new InlineKeyboardButton(CartText.PaginationPrevious)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.MovePagination,
                        CartElement = currentPage - 1
                    })
                };
            }

            return result;
        }

        private async Task DeleteFromCartFromCategoryAsync(int productId, Message message)
        {
            RemoveFromCart(productId);

            var product = await _dataSource.Products
                .Select(product => new { product.Id, product.IsVisible, product.Price })
                .FirstOrDefaultAsync(product => product.Id == productId && product.IsVisible);

            InlineKeyboardButton clickedButton = message.ReplyMarkup.InlineKeyboard
                .SelectMany(buttons => buttons)
                .First(button => button.CallbackData.Contains($"\"Cmd\":{(int)Command.DeleteFromCartFromCategory}") &&
                                 button.CallbackData.Contains($"\"ProductId\":{product.Id.ToString()}"));

            if (product.IsNull())
            {
                clickedButton.Text = CartText.ProductNotAvaliable;
                clickedButton.CallbackData = JsonConvert.SerializeObject(new
                {
                    Cmd = Command.ProductNotAvaliable
                });

                await _stateManager.EditMessageReplyMarkupAsync(message.MessageId, message.ReplyMarkup);
                return;
            }

            clickedButton.Text = string.Format(CartText.AddToCartPrice, product.Price.GetPriceFormat());
            clickedButton.CallbackData = JsonConvert.SerializeObject(new
            {
                Cmd = Command.AddToCartFromCategory,
                ProductId = productId
            });

            await _stateManager.EditMessageReplyMarkupAsync(message.MessageId, message.ReplyMarkup);
        }

        private async Task DeleteFromCartFromProductAsync(int productId, Message message)
        {
            RemoveFromCart(productId);

            var product = await _dataSource.Products
                .Select(product => new { product.Id, product.IsVisible })
                .FirstOrDefaultAsync(product => product.Id == productId && product.IsVisible);

            if (product.IsNull())
            {
                await _stateManager.SendMessageAsync(CatalogText.ProductNotFound);
                return;
            }


            InlineKeyboardButton clickedButton = message.ReplyMarkup.InlineKeyboard
                .SelectMany(buttons => buttons)
                .First(button => button.CallbackData.Contains($"\"Cmd\":{(int)Command.DeleteFromCartFromProduct}") &&
                                 button.CallbackData.Contains($"\"ProductId\":{product.Id.ToString()}"));
            clickedButton.Text = CartText.AddToCart;
            clickedButton.CallbackData = JsonConvert.SerializeObject(new
            {
                Cmd = Command.AddToCartFromProduct,
                ProductId = productId
            });

            await _stateManager.EditMessageReplyMarkupAsync(message.MessageId, message.ReplyMarkup);
        }

        private async Task AddToCartFromProductAsync(int productId, Message message)
        {
            Product product = await _dataSource.Products
                .FirstOrDefaultAsync(product => product.Id == productId && product.IsVisible);

            if (product.IsNull())
            {
                await _stateManager.SendMessageAsync(CatalogText.ProductNotFound);
                return;
            }

            AddToCart(productId);

            InlineKeyboardButton clickedButton = message.ReplyMarkup.InlineKeyboard
                .SelectMany(buttons => buttons)
                .First(button => button.CallbackData.Contains($"\"Cmd\":{(int)Command.AddToCartFromProduct}") &&
                                 button.CallbackData.Contains($"\"ProductId\":{product.Id.ToString()}"));
            clickedButton.Text = CartText.DeleteFromCart;
            clickedButton.CallbackData = JsonConvert.SerializeObject(new
            {
                Cmd = Command.DeleteFromCartFromProduct,
                ProductId = productId
            });

            await _stateManager.EditMessageReplyMarkupAsync(message.MessageId, message.ReplyMarkup);
        }

        private void RemoveFromCart(int productId)
        {
            CartProduct productInCart = Cart.First(product => product.Id == productId);

            if(productInCart.Count == 1)
            {
                Cart.Remove(productInCart);
                return;
            }

            productInCart.Count--;
        }
    }
}
