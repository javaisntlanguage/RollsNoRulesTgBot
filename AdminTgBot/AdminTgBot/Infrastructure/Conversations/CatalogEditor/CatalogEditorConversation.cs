using AdminTgBot.Infrastructure.Conversations.Start;
using AdminTgBot.Infrastructure.Models;
using Database;
using Database.Tables;
using Helper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Util.Core;
using static System.Net.Mime.MediaTypeNames;
using Image = System.Drawing.Image;

namespace AdminTgBot.Infrastructure.Conversations.CatalogEditor
{
    internal class CatalogEditorConversation : IConversation
    {
        private ApplicationContext _dataSource;
        private readonly AdminBotStateManager _stateManager;

        public int? ProductId { get; set; }
        public string AttributeValue { get; set; }
        public Product Product { get; set; }
        public int? CategoryId { get; set; }

        public CatalogEditorConversation() { }

        public CatalogEditorConversation(AdminBotStateManager statesManager)
        {
            _stateManager = statesManager;
        }
        public async Task<Trigger?> TryNextStepAsync(ApplicationContext dataSource, Message message)
        {
			_dataSource = dataSource;
			switch (_stateManager.GetState())
            {
                case State.CommandCatalogEditor:
                    {
                        if (message.Text == MessagesText.CommandCatalogEditor)
                        {
                            await ShowCategoryAsync();
                        }
                        return Trigger.Ignore;
                    }
                case State.ProductNameEditor:
                    {
                        await EditNameAsync(message.Text);
                        return Trigger.Ignore;
                    }
                case State.ProductDescriptionEditor:
                    {
                        await EditDescriptionAsync(message.Text);
                        return Trigger.Ignore;
                    }
                case State.ProductPriceEditor:
                    {
                        await EditPriceAsync(message.Text);
                        return Trigger.Ignore;
                    }
                case State.ProductPhotoEditor:
                    {
                        await EditPhotoAsync(message);
                        return Trigger.Ignore;
                    }
                case State.NewProductNameEditor:
                    {
                        return await EditNewProductAsync(message, ProductAttribute.Name);
                    }
                case State.NewProductDescriptionEditor:
                    {
                        return await EditNewProductAsync(message, ProductAttribute.Description);
                    }
                case State.NewProductPriceEditor:
                    {
                        return await EditNewProductAsync(message, ProductAttribute.Price);
                    }
                case State.NewProductPhotoEditor:
                    {
                        return await EditNewProductAsync(message, ProductAttribute.Photo);
                    }
            }

            return null;
        }

        public async Task<Trigger?> TryNextStepAsync(ApplicationContext dataSource, CallbackQuery query)
        {
			_dataSource = dataSource;
			JObject data = JObject.Parse(query.Data);
            Command command = data.GetEnumValue<Command>("Cmd");

            switch (_stateManager.GetState())
            {
                case State.CommandCatalogEditor:
                    {
                        switch (command)
                        {
                            case Command.MovePaginationCategory:
                                {
                                    int categoryId = data["CategoryId"].Value<int>();
                                    await ShowCategoryAsync(categoryId);
                                    return Trigger.Ignore;
                                }
                            case Command.Ignore:
                                {
                                    return Trigger.Ignore;
                                }
                            case Command.ShowCategoryProducts:
                                {
                                    int categoryId = data["CategoryId"].Value<int>();
                                    await ShowProductAsync(categoryId: categoryId);
                                    return Trigger.Ignore;
                                }
                            case Command.MovePaginationProduct:
                                {
                                    int categoryId = data["CategoryId"].Value<int>();
                                    int productId = data["ProductId"].Value<int>();
                                    await ShowProductAsync(categoryId: categoryId, productId: productId);
                                    return Trigger.Ignore;
                                }
                            case Command.ReturnToCatalog:
                                {
                                    await ShowCategoryAsync();
                                    return Trigger.Ignore;
                                }
                            case Command.EditProduct:
                                {
                                    int productId = data["ProductId"].Value<int>();
                                    ProductAttribute productAttribute = (ProductAttribute)data["ProductAttribute"].Value<int>();
                                    await SuggestEditProductAsync(productId, productAttribute);
                                    return Trigger.Ignore;
                                }
                            case Command.ReturnToProduct:
                                {
                                    int productId = data["ProductId"].Value<int>();
                                    await ShowProductAsync(productId: productId);
                                    return Trigger.Ignore;
                                }
                            case Command.EditProductAttribute:
                                {
                                    ProductAttribute productAttribute = (ProductAttribute)data["ProductAttribute"].Value<int>();
                                    return await GetEditProductAttributeTriggerAsync(productAttribute);
                                }
                            case Command.EditProductVisibility:
                                {
                                    bool isVisible = data["Visibility"].Value<bool>();
                                    await ChangeProductAttributeValueAsync(isVisible);
                                    return Trigger.Ignore;

                                }
                            case Command.AddProduct:
                                {
                                    int categoryId = data["CategoryId"].Value<int>();
                                    await EnterNewProductNameAsync(categoryId);
                                    return Trigger.EnterProductName;
                                }
                        }
                        break;
                    }
                case State.ProductNameEditor:
                case State.ProductDescriptionEditor:
                case State.ProductPriceEditor:
                case State.ProductPhotoEditor:
                    {
                        switch (command)
                        {
                            case Command.EditProduct:
                                {
                                    ProductAttribute productAttribute = (ProductAttribute)data["ProductAttribute"].Value<int>();
                                    await SuggestEditProductAsync(ProductId.Value, productAttribute);
                                    return Trigger.ReturnToProductAttributeEditor;
                                }
                            case Command.EditProductAttributeYes:
                                {
                                    ProductAttribute productAttribute = (ProductAttribute)data["ProductAttribute"].Value<int>();
                                    await ChangeProductAttributeValueAsync(productAttribute);
                                    return Trigger.ReturnToProductAttributeEditor;
                                }
                        }
                        break;
                    }
                case State.NewProductPhotoEditor:
                    {
                        switch (command)
                        {
                            case Command.AddProduct:
                                {
                                    await AddProductAsync();
                                    return Trigger.ReturnToCalatog;
                                }
                        }
                        break;
                    }
            }

            return null;
        }

        private async Task AddProductAsync()
        {
            if(Product.IsNull())
            {
                throw new Exception(" При попытке добавления товара он не найден");
            }
            if (CategoryId.IsNull() || !await _dataSource.Categories.AnyAsync(c => c.Id == CategoryId))
            {
                throw new Exception(" При попытке добавления товара категория не найдена");
            }

            Product.IsVisible = true;

            ProductCategories product = new ProductCategories();
            product.Product = Product;
            product.CategoryId = CategoryId.Value;

            await _dataSource.ProductCategories.AddAsync(product);
            await _dataSource.SaveChangesAsync();
        }

        private async Task<Trigger> EditNewProductAsync(Message message, ProductAttribute productAttribute)
        {
            Trigger? result = null;
            string nextAttributeText = null;
            bool isInvalidValue = false;

            if(Product.IsNull())
            {
                Product = new Product();
            }

            switch (productAttribute)
            {
                case ProductAttribute.Name:
                    {
                        Product.Name = message.Text;
                        nextAttributeText = CatalogEditorText.ProductDesciption;
                        result = Trigger.EnterProductDescription;
                        break;
                    }
                case ProductAttribute.Description:
                    {
                        Product.Description = message.Text;
                        nextAttributeText = CatalogEditorText.ProductPrice;
                        result = Trigger.EnterProductPrice;
                        break;
                    }
                case ProductAttribute.Price:
                    {
                        if (decimal.TryParse(message.Text, out decimal price))
                        {
                            Product.Price = price;
                            nextAttributeText = CatalogEditorText.ProductPhoto;
                            result = Trigger.EnterProductPhoto;
                        }
                        else
                        {
                            isInvalidValue = true;
                        }
                        break;
                    }
                case ProductAttribute.Photo:
                    {
                        if (message.Photo.IsNull())
                        {
                            isInvalidValue = true;
                        }
                        else
                        {
                            string fileId = message.GetPhotoFileId();
                            Product.Photo = await _stateManager.GetFileAsync(fileId);
                            result = Trigger.Ignore;
                        }
                        break;
                    }
            }

            if (isInvalidValue)
            {
                await _stateManager.SendMessageAsync(CatalogEditorText.WrongValue);
                result = Trigger.Ignore;
            }
            else if (nextAttributeText.IsNull())
            {
                string caption = string.Format(CatalogEditorText.ProductDetails,
                Product.Name,
                Product.Description,
                Product.Price.ToString(TelegramHelper.PRICE_FORMAT));

                InlineKeyboardButton[] AddProduct = GetAddNewProductButton();
                InlineKeyboardMarkup markup = new InlineKeyboardMarkup(AddProduct);

                await _stateManager.SendMessageAsync(caption, parseMode: ParseMode.Html, replyMarkup: markup, photo: Product.Photo);
            }
            else
            {
                string text = string.Format(CatalogEditorText.EnterProductAttribute, nextAttributeText);
                await _stateManager.SendMessageAsync(text);
            }

            return result.Value;
        }

        private InlineKeyboardButton[] GetAddNewProductButton()
        {
            InlineKeyboardButton[] result = new InlineKeyboardButton[]
            {
                new InlineKeyboardButton(CatalogEditorText.Add)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                            {
                                Cmd = Command.AddProduct,
                            })
                }
            };

            return result;
        }

        private async Task EnterNewProductNameAsync(int categoryId)
        {
            CategoryId = categoryId;
            string text = string.Format(CatalogEditorText.EnterProductAttribute, CatalogEditorText.ProductName);
            await _stateManager.SendMessageAsync(text);
        }

        private async Task ChangeProductAttributeValueAsync(bool isVisible)
        {
            Product product = await _dataSource.Products
                .FirstOrDefaultAsync(p => p.Id == ProductId);

            product.IsVisible = isVisible;

            await _dataSource.SaveChangesAsync();

            await SuggestEditProductAsync(ProductId.Value, ProductAttribute.Visibility);
        }
        private async Task ChangeProductAttributeValueAsync(ProductAttribute productAttribute)
        {
            Product product = await _dataSource.Products
                .FirstOrDefaultAsync(p => p.Id == ProductId);

            switch(productAttribute)
            {
                case ProductAttribute.Name:
                    {
                        product.Name = AttributeValue;
                        break;
                    }
                case ProductAttribute.Description:
                    {
                        product.Description = AttributeValue;
                        break;
                    }
                case ProductAttribute.Price:
                    {
                        product.Price = decimal.Parse(AttributeValue);
                        break;
                    }
                case ProductAttribute.Photo:
                    {
                        product.Photo = AttributeValue;
                        break;
                    }
                default:
                    {
                        ProductId = null;
                        AttributeValue = null;
                        throw new Exception("Неизвестный тип атрибута");
                    }
            }

            await _dataSource.SaveChangesAsync();

            AttributeValue = null;

            await SuggestEditProductAsync(ProductId.Value, productAttribute);
        }

        private async Task EditPhotoAsync(Message message)
        {
            string fileId = message.GetPhotoFileId();

            if (fileId.IsNullOrEmpty())
            {
                await _stateManager.SendMessageAsync(CatalogEditorText.WrongValue);
                return;
            }

            AttributeValue = await _stateManager.GetFileAsync(fileId);

            InlineKeyboardButton[] yesNo = GetYesNoChangeAttributeButtons(ProductAttribute.Photo);
            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(yesNo);

            await _stateManager.SendMessageAsync(CatalogEditorText.Approve, replyMarkup: markup);
        }

        private async Task EditPriceAsync(string text)
        {
            InlineKeyboardMarkup markup = null;
            if (!decimal.TryParse(text, out var _))
            {
                await _stateManager.SendMessageAsync(CatalogEditorText.WrongValue);
                return;
            }

            AttributeValue = text;

            InlineKeyboardButton[] yesNo = GetYesNoChangeAttributeButtons(ProductAttribute.Price);
            markup = new InlineKeyboardMarkup(yesNo);

            await _stateManager.SendMessageAsync(CatalogEditorText.Approve, replyMarkup: markup);
        }

        private async Task EditDescriptionAsync(string text)
        {
            AttributeValue = text;

            InlineKeyboardButton[] yesNo = GetYesNoChangeAttributeButtons(ProductAttribute.Description);

            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(yesNo);

            await _stateManager.SendMessageAsync(CatalogEditorText.Approve, replyMarkup: markup);
        }

        private async Task EditNameAsync(string text)
        {
            AttributeValue = text;

            InlineKeyboardButton[] yesNo = GetYesNoChangeAttributeButtons(ProductAttribute.Name);

            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(yesNo);

            await _stateManager.SendMessageAsync(CatalogEditorText.Approve, replyMarkup: markup);
        }

        private InlineKeyboardButton[] GetYesNoChangeAttributeButtons(ProductAttribute productAttribute)
        {
            InlineKeyboardButton[] result = new InlineKeyboardButton[]
            {
                new InlineKeyboardButton(CatalogEditorText.Yes)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                            {
                                Cmd = Command.EditProductAttributeYes,
                                ProductAttribute = productAttribute
                            })
                },
                new InlineKeyboardButton(CatalogEditorText.No)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                            {
                                Cmd = Command.EditProduct,
                                ProductAttribute = productAttribute
                            })
                },
            };

            return result;
        }

        private async Task<Trigger?> GetEditProductAttributeTriggerAsync(ProductAttribute productAttribute)
        {
            Trigger? result = null;
            switch (productAttribute)
            {
                case ProductAttribute.Name:
                    {
                        result = Trigger.EditProductName;
                        break;
                    }
                case ProductAttribute.Description:
                    {
                        result = Trigger.EditProductDescription;
                        break;
                    }
                case ProductAttribute.Price:
                    {
                        result = Trigger.EditProductPrice;
                        break;
                    }
                case ProductAttribute.Photo:
                    {
                        result = Trigger.EditProductPhoto;
                        break;
                    }
                default:
                    {
                        throw new Exception("Неизвестный тип атрибута");
                    }
            }

            InlineKeyboardButton[] returnToAttributeMenu = GetEditProductButton(productAttribute, CatalogEditorText.ReturnToProductEditor);

            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(returnToAttributeMenu);

            await _stateManager.SendMessageAsync(CatalogEditorText.EnterValue, replyMarkup: markup);

            return result;
        }

        private async Task SuggestEditProductAsync(int productId, ProductAttribute productAttribute)
        {
            switch (productAttribute)
            {
                case ProductAttribute.Name:
                    {
                        await SuggestEditProductNameAsync(productId);
                        break;
                    }
                case ProductAttribute.Description:
                    {
                        await SuggestEditProductDescriptionAsync(productId);
                        break;
                    }
                case ProductAttribute.Price:
                    {
                        await SuggestEditProductPriceAsync(productId);
                        break;
                    }
                case ProductAttribute.Photo:
                    {
                        await SuggestEditProductPhotoAsync(productId);
                        break;
                    }
                case ProductAttribute.Visibility:
                    {
                        await SuggestEditProductVisibilityAsync(productId);
                        break;
                    }
                default:
                    {
                        ProductId = null;
                        throw new Exception($"Незивестный тип аттрибута: {productAttribute}");
                    }
            }
        }

        private async Task SuggestEditProductVisibilityAsync(int productId)
        {
            bool? isVisible = _dataSource.Products.FirstOrDefault(product => product.Id == productId)?.IsVisible;

            string attributeValue = isVisible.HasValue && isVisible.Value ?
                CatalogEditorText.Yes :
                CatalogEditorText.No;

            InlineKeyboardButton[] editAttribute = GetEditProductVisibilityButton(productId, isVisible);
			InlineKeyboardButton[] nextAttribute = GetEditProductButton(ProductAttribute.Name, CatalogEditorText.Next, productId);
			InlineKeyboardButton[] returnToProduct = GetReturnToProductButton(productId);

            string text = string.Format(CatalogEditorText.EditProductAttributeTemplate,
                CatalogEditorText.ProductVisibility,
                attributeValue);

            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
            {
                editAttribute,
				nextAttribute,
				returnToProduct
            });


            await _stateManager.SendMessageAsync(text, ParseMode.Html, markup);
        }
        private async Task SuggestEditProductPhotoAsync(int productId)
        {
            string attributeValue = _dataSource.Products.FirstOrDefault(product => product.Id == productId)?.Photo;

            InlineKeyboardButton[] editAttribute = GetEditAttributeButton(productId, ProductAttribute.Photo);
            InlineKeyboardButton[] returnToProduct = GetReturnToProductButton(productId);

            string photoText = attributeValue.IsNullOrEmpty() ?
                CatalogEditorText.ProductAttributeNoValue :
                CatalogEditorText.LookPhoto;

            string text = string.Format(CatalogEditorText.EditProductAttributeTemplate,
                CatalogEditorText.ProductPhoto,
                photoText);

            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
            {
                editAttribute,
                returnToProduct
            });

            await _stateManager.SendMessageAsync(text, ParseMode.Html, markup, photo: attributeValue);
        }

        private async Task SuggestEditProductPriceAsync(int productId)
        {
            string attributeValue = _dataSource.Products.FirstOrDefault(product => product.Id == productId)?.Price.ToString();

            if (attributeValue.IsNullOrEmpty())
            {
                attributeValue = CatalogEditorText.ProductAttributeNoValue;
            }

            InlineKeyboardButton[] editAttribute = GetEditAttributeButton(productId, ProductAttribute.Price);
            InlineKeyboardButton[] nextAttribute = GetEditProductButton(ProductAttribute.Photo, CatalogEditorText.Next, productId);
            InlineKeyboardButton[] returnToProduct = GetReturnToProductButton(productId);

            string text = string.Format(CatalogEditorText.EditProductAttributeTemplate,
                CatalogEditorText.ProductPrice,
                attributeValue);

            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
            {
                editAttribute,
                nextAttribute,
                returnToProduct
            });

            await _stateManager.SendMessageAsync(text, parseMode: ParseMode.Html, replyMarkup: markup);
        }

        private async Task SuggestEditProductDescriptionAsync(int productId)
        {
            string attributeValue = _dataSource.Products.FirstOrDefault(product => product.Id == productId)?.Description;

            if (attributeValue.IsNullOrEmpty())
            {
                attributeValue = CatalogEditorText.ProductAttributeNoValue;
            }

            InlineKeyboardButton[] editAttribute = GetEditAttributeButton(productId, ProductAttribute.Description);
            InlineKeyboardButton[] nextAttribute = GetEditProductButton(ProductAttribute.Price, CatalogEditorText.Next, productId);
            InlineKeyboardButton[] returnToProduct = GetReturnToProductButton(productId);

            string text = string.Format(CatalogEditorText.EditProductAttributeTemplate,
                CatalogEditorText.ProductDesciption,
                attributeValue);

            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
            {
                editAttribute,
                nextAttribute,
                returnToProduct
            });

            await _stateManager.SendMessageAsync(text, parseMode: ParseMode.Html, replyMarkup: markup);
        }

        private async Task SuggestEditProductNameAsync(int productId)
        {
            string attributeValue = _dataSource.Products.FirstOrDefault(p => p.Id == productId)?.Name;

            if (attributeValue.IsNullOrEmpty())
            {
                attributeValue = CatalogEditorText.ProductAttributeNoValue;
            }

            InlineKeyboardButton[] editAttribute = GetEditAttributeButton(productId, ProductAttribute.Name);
            InlineKeyboardButton[] nextAttribute = GetEditProductButton(ProductAttribute.Description, CatalogEditorText.Next, productId);
            InlineKeyboardButton[] returnToProduct = GetReturnToProductButton(productId);

            string text = string.Format(CatalogEditorText.EditProductAttributeTemplate,
                CatalogEditorText.ProductName,
                attributeValue);

            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
            {
                editAttribute,
                nextAttribute,
                returnToProduct
            });

            await _stateManager.SendMessageAsync(text, parseMode: ParseMode.Html, replyMarkup: markup);
        }

        private InlineKeyboardButton[] GetReturnToProductButton(int productId)
        {
            InlineKeyboardButton[] result = new InlineKeyboardButton[]{
                        new InlineKeyboardButton(CatalogEditorText.ReturnToProduct)
                        {
                            CallbackData = JsonConvert.SerializeObject(new
                            {
                                Cmd = Command.ReturnToProduct,
                                ProductId = productId,
                            })
                        }
                    };

            return result;
        }

        private InlineKeyboardButton[] GetEditProductVisibilityButton(int productId, bool? isVisible)
        {
            InlineKeyboardButton[] result = null;
            if (isVisible.HasValue && isVisible.Value)
            {
                result = new InlineKeyboardButton[]{
                        new InlineKeyboardButton(CatalogEditorText.ProductVisibilityOff)
                        {
                            CallbackData = JsonConvert.SerializeObject(new
                            {
                                Cmd = Command.EditProductVisibility,
                                ProductId = productId,
                                ProductAttribute = ProductAttribute.Visibility,
                                Visibility = false
                            })
                        }
                    };
            }
            else
            {
                result = new InlineKeyboardButton[]{
                        new InlineKeyboardButton(CatalogEditorText.ProductVisibilityOn)
                        {
                            CallbackData = JsonConvert.SerializeObject(new
                            {
                                Cmd = Command.EditProductVisibility,
                                ProductId = productId,
                                ProductAttribute = ProductAttribute.Visibility,
                                Visibility = true
                            })
                        }
                    };
            }
            

            return result;
        }

        private InlineKeyboardButton[] GetEditAttributeButton(int productId, ProductAttribute productAttribute)
        {
            InlineKeyboardButton[] result = new InlineKeyboardButton[]{
                        new InlineKeyboardButton(CatalogEditorText.Change)
                        {
                            CallbackData = JsonConvert.SerializeObject(new
                            {
                                Cmd = Command.EditProductAttribute,
                                ProductId = productId,
                                ProductAttribute = productAttribute
                            })
                        }
                    };

            return result;
        }

        /// <summary>
        /// показать товар
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="messageId"></param>
        /// <param name="productId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private async Task ShowProductAsync(int? productId = null, int? categoryId = null)
        {
            Product product = null;
            List<InlineKeyboardButton[]> keyboard = null;
            string text = null;

            if(productId.IsNull() && categoryId.IsNull())
            {
                throw new Exception("не указан продукт и категория");
            }

            if (categoryId.IsNull())
            {
                categoryId = (await _dataSource.ProductCategories
                    .FirstOrDefaultAsync(pc => pc.ProductId == productId))
                    .CategoryId;
            }

            if (productId.IsNull())
            {
                product = (await _dataSource.ProductCategories
                    .Include(pc => pc.Product)
                    .FirstOrDefaultAsync(pc => pc.CategoryId == categoryId))
                    ?.Product;
            }
            else
            {
                product = await _dataSource.Products
                    .FirstOrDefaultAsync(p => p.Id == productId);
            }

            List<InlineKeyboardButton[]> actions = GetProductActions(categoryId.Value, product?.Id);
            InlineKeyboardButton[] returnToCatalog = GetReturnToCatalogButton();

            keyboard = actions;

            if (product.IsNull())
            {
                text = CatalogEditorText.ProductsNotFound;
            }
            else
            {
                ProductId = product.Id;

                text = string.Format(CatalogEditorText.ProductDetails,
                product.Name,
                product.Description,
                product.Price.ToString(TelegramHelper.PRICE_FORMAT));

                InlineKeyboardButton[] pagination = GetPaginationProduct(categoryId.Value, product.Id);

                if (pagination.IsNotNull())
                {
                    keyboard.Add(pagination);
                }
            }

            keyboard.Add(returnToCatalog);

            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(keyboard);

            await _stateManager.SendMessageAsync(text, ParseMode.Html, markup, photo: product.Photo);
        }

        /// <summary>
        /// получение кнопки возврата в выбор категории
        /// </summary>
        /// <returns></returns>
        private InlineKeyboardButton[] GetReturnToCatalogButton()
        {
            InlineKeyboardButton[] result = new InlineKeyboardButton[]{
                        new InlineKeyboardButton(CatalogEditorText.ReturnToCatalog)
                        {
                            CallbackData = JsonConvert.SerializeObject(new
                            {
                                Cmd = Command.ReturnToCatalog,
                            })
                        }
                    };

            return result;
        }

        /// <summary>
        /// получение кнопки добавления товара
        /// </summary>
        /// <param name="categoryId"></param>
        /// <returns></returns>
        private InlineKeyboardButton GetAddProductButton(int categoryId)
        {
            InlineKeyboardButton result = new InlineKeyboardButton(CatalogEditorText.AddProduct)
            {
                CallbackData = JsonConvert.SerializeObject(new
                {
                    Cmd = Command.AddProduct,
                    CategoryId = categoryId
                })
            };

            return result;
        }

        /// <summary>
        /// получение кнопки редактирования товара
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        private InlineKeyboardButton[] GetEditProductButton(ProductAttribute productAttribute, string text, int? productId = null)
        {
            InlineKeyboardButton[] result = new InlineKeyboardButton[]
            {
                new InlineKeyboardButton(text)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.EditProduct,
                        ProductId = productId,
                        ProductAttribute = productAttribute
                    })
                } 
            };

            return result;
        }


        /// <summary>
        /// показать категорию
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task ShowCategoryAsync(int? categoryId = null)
        {
            ProductId = null;

            Category category = null;
            if (categoryId.IsNull())
            {
                category = await _dataSource.Categories
                    .FirstOrDefaultAsync();
            }
            else
            {
                category = await _dataSource.Categories
                    .FirstOrDefaultAsync(c => c.Id == categoryId);
            }

            InlineKeyboardButton[] pagination = GetPaginationCategory(category.Id);
            InlineKeyboardButton[] actions = GetCategoryActions(category);

            List<InlineKeyboardButton[]> keyboard = new List<InlineKeyboardButton[]>
            {
                actions,
            };

            if (pagination.IsNotNull())
            {
                keyboard.Insert(1, pagination);
            }
            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(keyboard);

            await _stateManager.SendMessageAsync(CatalogEditorText.ChoosingCategory, replyMarkup: markup);
        }

        /// <summary>
        /// получение действий для категории
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private InlineKeyboardButton[] GetCategoryActions(Category category)
        {
            InlineKeyboardButton showProducts = GetShowProductsButton(category);

            InlineKeyboardButton[] result = new InlineKeyboardButton[]
            {
                showProducts
            };

            return result;
        }

        /// <summary>
        /// получение действий для товара
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        private List<InlineKeyboardButton[]> GetProductActions(int categoryId, int? productId)
        {
            InlineKeyboardButton addProduct = GetAddProductButton(categoryId);

            List<InlineKeyboardButton[]> result = new List<InlineKeyboardButton[]>
            {
                new InlineKeyboardButton[]
                {
                    addProduct
                }
            };

            if (productId.HasValue)
            {
                InlineKeyboardButton[] editProduct = GetEditProductButton(ProductAttribute.Visibility, CatalogEditorText.EditProduct, productId);
                result.Add(editProduct);
            }

            return result;
        }

        /// <summary>
        /// получение кнопки для показа товаров в категории
        /// </summary>
        /// <param name="categoryId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private InlineKeyboardButton GetShowProductsButton(Category category)
        {
            InlineKeyboardButton result = new InlineKeyboardButton(category.Name)
            {
                CallbackData = JsonConvert.SerializeObject(new
                {
                    Cmd = Command.ShowCategoryProducts,
                    CategoryId = category.Id
                })
            };

            return result;
        }

        /// <summary>
        /// получение пагинации для категории
        /// </summary>
        /// <param name="categoryId"></param>
        /// <returns></returns>
        private InlineKeyboardButton[] GetPaginationCategory(int categoryId)
        {

            IQueryable<int> products = _dataSource.Categories
                .Select(c => c.Id);

            if (products.Count() == 1)
            {
                return null;
            }

            InlineKeyboardButton buttonPrevious = GetButtonPreviousCategory(categoryId, products);
            InlineKeyboardButton buttonNext = GetButtonNextCategory(categoryId, products);

            InlineKeyboardButton[] result = new InlineKeyboardButton[]
            {
                buttonPrevious,
                buttonNext
            };

            return result;
        }

        /// <summary>
        /// получение пагинации для товара
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="productId"></param>
        /// <returns></returns>
        private InlineKeyboardButton[] GetPaginationProduct(int categoryId, int productId)
        {
            IQueryable<int> products = _dataSource.ProductCategories
                .Where(pc => pc.CategoryId == categoryId)
                .Select(pc => pc.ProductId);

            if (products.Count() == 1)
            {
                return null;
            }

            InlineKeyboardButton buttonPrevious = GetButtonPreviousProduct(categoryId, productId, products);
            InlineKeyboardButton buttonNext = GetButtonNextProduct(categoryId, productId, products);

            InlineKeyboardButton[] result = new InlineKeyboardButton[]
            {
                buttonPrevious,
                buttonNext
            };

            return result;
        }

        /// <summary>
        /// получение кнопки для пагинации назад для категории
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="categories"></param>
        /// <returns></returns>
        private InlineKeyboardButton GetButtonPreviousCategory(int categoryId, IQueryable<int> categories)
        {
            InlineKeyboardButton result = null;
            IQueryable<int> previousCategoryIds = categories
                .Where(pcid => pcid < categoryId);

            if (previousCategoryIds.Any())
            {
                int previousId = previousCategoryIds
                    .Max();

                result = new InlineKeyboardButton(CatalogEditorText.PaginationPrevious)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.MovePaginationCategory,
                        CategoryId = previousId
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
        /// получение кнопки для пагинации назад для товара
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="products"></param>
        /// <returns></returns>
        private InlineKeyboardButton GetButtonPreviousProduct(int categoryId, int productId, IQueryable<int> products)
        {
            InlineKeyboardButton result = null;
            IQueryable<int> previousProductIds = products
                .Where(pcid => pcid < productId);

            if (previousProductIds.Any())
            {
                int previousId = previousProductIds
                    .Max();

                result = new InlineKeyboardButton(CatalogEditorText.PaginationPrevious)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.MovePaginationProduct,
                        CategoryId = categoryId,
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

        /// <summary>
        /// получение кнопки для пагинации вперед для категории
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="categories"></param>
        /// <returns></returns>
        private InlineKeyboardButton GetButtonNextCategory(int categoryId, IQueryable<int> categories)
        {
            InlineKeyboardButton result = null;
            IQueryable<int> nextIds = categories
                .Where(pcid => pcid > categoryId);

            if (nextIds.Any())
            {
                int nextId = nextIds
                    .Min();

                result = new InlineKeyboardButton(CatalogEditorText.PaginationNext)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.MovePaginationCategory,
                        CategoryId = nextId
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
        /// 
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="products"></param>
        /// <returns></returns>
        private InlineKeyboardButton GetButtonNextProduct(int categoryId, int productId, IQueryable<int> products)
        {
            InlineKeyboardButton result = null;
            IQueryable<int> nextProductIds = products
                .Where(pcid => pcid > productId);

            if (nextProductIds.Any())
            {
                int nextId = nextProductIds
                    .Min();

                result = new InlineKeyboardButton(CatalogEditorText.PaginationNext)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.MovePaginationProduct,
                        CategoryId = categoryId,
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
            InlineKeyboardButton result = new InlineKeyboardButton(CatalogEditorText.NoPagination)
            {
                CallbackData = JsonConvert.SerializeObject(new
                {
                    Cmd = Command.Ignore
                })
            };

            return result;
        }
    }
}
