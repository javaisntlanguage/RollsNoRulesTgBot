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

namespace MenuTgBot.Infrastructure.Conversations.Cart
{
    internal class CartConversation : IConversation
    {
        private readonly long _chatId;
        private readonly ApplicationContext _dataSource;
        private readonly StateManager _stateManager;

        public List<CartProduct> Cart {  get; set; }

        public CartConversation() { }
        public CartConversation(ApplicationContext dataSource, StateManager statesManager)
        {
            _dataSource = dataSource;
            _stateManager = statesManager;
            _chatId = _stateManager.ChatId;

            Cart = new List<CartProduct>();
        }

        public async Task<Trigger?> TryNextStepAsync(Message message)
        {
            switch (_stateManager.CurrentState)
            {
                case State.CommandCart:
                    {
                        await ShowCartAsync(1);
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
                            //удалить?
                            case Command.AddToCartFromProduct:
                                {
                                    int productId = data["ProductId"].Value<int>();
                                    await AddToCartFromProductAsync(productId, query.Message);
                                    return Trigger.Ignore;
                                }
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
                                    await DecreaseCountAsync(productId, query.Message);
                                    return Trigger.Ignore;
                                }
                            case Command.IncreaseCount:
                                {
                                    int productId = data["ProductId"].Value<int>();
                                    await IncreaseCountAsync(productId, query.Message);
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
                Cart.Remove(cartProduct);
            }
            else
            {
                cartProduct.Count--;
            }
        }

        private InlineKeyboardMarkup GetCartMarkup(CartProduct cartProduct, Product product)
        {
            int page = Cart.IndexOf(cartProduct) + 1;
            InlineKeyboardButton[] pagination = GetPagination(page);
            InlineKeyboardButton[] productCountInfo = GetProductCountInfo(cartProduct, product.IsVisible);
            InlineKeyboardButton[] takeOrderButton = GetTakeOrderButton();

            List<InlineKeyboardButton[]> keyboard = new List<InlineKeyboardButton[]>
            {
                productCountInfo,
                takeOrderButton
            };

            if (pagination.IsNotNull())
            {
                keyboard.Insert(1, pagination);
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

            InlineKeyboardMarkup markup = GetCartMarkup(cartProduct, product);

            await _stateManager.EditMessageReplyMarkupAsync(message.MessageId, replyMarkup: markup);
        }

        private async Task DecreaseCountAsync(int productId, Message message)
        {
            CartProduct cartProduct = Cart.First(product => product.Id == productId);

            if (cartProduct.Count == 1)
            {
                int index = Cart.IndexOf(cartProduct);
                int page = index + 1;
                Cart.Remove(cartProduct);

                InlineKeyboardButton[] addToCartButton = GetAddToCartButton(productId, index);
                InlineKeyboardButton[] takeOrderButton = GetTakeOrderButton();
                InlineKeyboardButton[] pagination = GetPagination(page, forDelete: true);

                List<InlineKeyboardButton[]> keyboard = new List<InlineKeyboardButton[]>
                {
                    addToCartButton,
                    takeOrderButton
                };

                if (pagination.IsNotNull())
                {
                    keyboard.Insert(1, pagination);
                }

                InlineKeyboardMarkup markup = new InlineKeyboardMarkup(keyboard);

                if (Cart.Count == 0)
                {
                    markup = new(markup.InlineKeyboard.Take(1));
                }

                await _stateManager.EditMessageReplyMarkupAsync(message.MessageId, replyMarkup: markup);
            }
            else
            {
                await ChangeCountAsync(productId, message, increase: false, cartProduct);
            }
        }

        private async Task IncreaseCountAsync(int productId, Message message)
        {
            await ChangeCountAsync(productId, message, increase: true);
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

        private async Task ChangeCountAsync(int productId, Message message, bool increase, CartProduct cartProduct = null)
        {
            cartProduct ??= Cart.First(product => product.Id == productId);
            cartProduct.Count = increase ? cartProduct.Count + 1 : cartProduct.Count - 1;

            InlineKeyboardButton buttonCount = message.ReplyMarkup.InlineKeyboard
                .SelectMany(buttons => buttons)
                .First(button => button.CallbackData.Contains($"\"Cmd\":{(int)Command.ProductCount}"));

            buttonCount.Text = cartProduct.Count.ToString();

            await _stateManager.EditMessageReplyMarkupAsync(message.MessageId, message.ReplyMarkup);
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
                    .FirstOrDefault();

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

            await _stateManager.SendMessageAsync(CartText.Welcome);
            await _stateManager.SendMessageAsync(text, ParseMode.Html, markup, product.Photo);
        }

        private InlineKeyboardButton[] GetTakeOrderButton()
        {
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

        private InlineKeyboardButton[] GetProductCountInfo(CartProduct cartProduct, bool isVisible)
        {
            InlineKeyboardButton[] result = null;

            if (isVisible)
            {
                result = new InlineKeyboardButton[]
                {
                    new InlineKeyboardButton(CartText.DecreaseCount)
                    {
                        CallbackData = JsonConvert.SerializeObject(new
                        {
                            Cmd = Command.DecreaseCount,
                            ProductId = cartProduct.Id,
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
            if(Cart.Count == 1 && !forDelete)
            {
                return null;
            }

            int nextButtonPage = forDelete ? currentPage - 1 : currentPage;

            InlineKeyboardButton buttonPrevious = GetButtonPrevious(currentPage);
            InlineKeyboardButton buttonNext = GetButtonNext(nextButtonPage);

            InlineKeyboardButton[] result = new InlineKeyboardButton[]
            {
                buttonPrevious,
                buttonNext
            };

            return result;
        }

        private InlineKeyboardButton GetButtonNext(int currentPage)
        {
            InlineKeyboardButton result = null;

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
                        CartElement = currentPage + 1
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

        private async Task AddToCartFromCategoryAsync(int productId, Message message)
        {
            var product = await _dataSource.Products
                .Select(product => new { product.Id, product.IsVisible })
                .FirstOrDefaultAsync(product => product.Id == productId && product.IsVisible);

            if (product.IsNull())
            {
                await _stateManager.SendMessageAsync(CatalogText.ProductNotFound);
                return;
            }

            CartProduct cartProduct = AddToCart(productId);

            List<IEnumerable<InlineKeyboardButton>> keyboard = message.ReplyMarkup.InlineKeyboard
                .ToList();

            var buttonsLine = keyboard
                .Select((line, i) => new { line, i })
                .First(line => line.line
                    .Any(button => button.CallbackData.Contains($"\"Cmd\":{(int)Command.AddToCartFromCategory}") &&
                                   button.CallbackData.Contains($"\"ProductId\":{product.Id.ToString()}")));

            keyboard[buttonsLine.i] = new InlineKeyboardButton[]
                {
                    keyboard[buttonsLine.i].First(),
                    new InlineKeyboardButton(CartText.DecreaseCount)
                    {
                        CallbackData = JsonConvert.SerializeObject(new
                        {
                            Cmd = Command.DeleteFromCartFromCategory,
                            ProductId = productId
                        })
                    },
                    new InlineKeyboardButton(cartProduct.Count.ToString())
                    {
                        CallbackData = JsonConvert.SerializeObject(new
                        {
                            Cmd = Command.Ignore,
                        })
                    },
                    new InlineKeyboardButton(CartText.IncreaseCount)
                    {
                        CallbackData = JsonConvert.SerializeObject(new
                        {
                            Cmd = Command.AddToCartFromCategory,
                            ProductId = productId
                        })
                    },
                };

            await _stateManager.EditMessageReplyMarkupAsync(message.MessageId, new InlineKeyboardMarkup(keyboard));
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
