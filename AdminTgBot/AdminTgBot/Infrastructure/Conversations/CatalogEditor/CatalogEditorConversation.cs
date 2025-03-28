﻿using AdminTgBot.Infrastructure.Conversations.CatalogEditor.Models;
using AdminTgBot.Infrastructure.Models;
using Database;
using Database.Tables;
using Helper;
using MessageContracts;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Telegram.Util.Core.Exceptions;
using Telegram.Util.Core.Extensions;
using CategoryAttribute = AdminTgBot.Infrastructure.Conversations.CatalogEditor.Models.CategoryAttribute;

namespace AdminTgBot.Infrastructure.Conversations.CatalogEditor
{
    internal class CatalogEditorConversation : IConversation
    {
		private const int CATEGORIES_BY_PAGE = 5;
		private const int SELL_LOCATIONS_BY_PAGE = 5;

		private ApplicationContext _dataSource;
        private readonly AdminBotStateManager _stateManager;

        public int? ProductId { get; set; }
        public int? SellLocationChangingId { get; set; }
        public string? AttributeValue { get; set; }
        public Product? NewProduct { get; set; }
        public Category? NewCategory { get; set; }
        public SellLocation? NewSellLocation { get; set; }
        public int? CategoryId { get; set; }

        public CatalogEditorConversation()
        {
            _stateManager = null!;
			_dataSource = null!;
		}

        public CatalogEditorConversation(AdminBotStateManager statesManager)
        {
			_stateManager = statesManager;
			_dataSource = null!;
        }
        public async Task<Trigger?> TryNextStepAsync(ApplicationContext dataSource, Message message)
        {
			_dataSource = dataSource;
			switch (_stateManager.CurrentState)
            {
                case State.CommandCatalogEditor:
                    {
                        if (message.Text == MessagesText.CommandCatalogEditor)
                        {
                            await WelcomeAsync();
                        }
                        break;
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
				case State.CategoryNameEditor:
					{
						await EditCategoryNameAsync(message.Text);
						return Trigger.Ignore;
					}
				case State.NewCategoryNameEditor:
					{
						return await EditNewCategoryAsync(message, CategoryAttribute.Name);
					}
				case State.CatalogEditorSellLocationEnterName:
					{
						SellLocationEnterName(message.Text);
                        await SellLocationSuggestConfirmAsync();
                        return Trigger.Ignore;
					}
				case State.CatalogEditorSellLocationEnterNameChange:
					{
						await SellLocationChangeSaveAsync(message.Text);
                        await ShowSellLocationsAsync();
                        return Trigger.ReturnToCatalogEditor;
					}
			}

            return null;
        }

		public async Task<Trigger?> TryNextStepAsync(ApplicationContext dataSource, CallbackQuery query)
        {
			_dataSource = dataSource;
			JObject data = JObject.Parse(query.Data!);
            Command command = data.GetEnumValue<Command>("Cmd");

            switch (_stateManager!.CurrentState)
            {
                case State.CommandCatalogEditor:
                    {
                        switch (command)
                        {
                            case Command.DeleteSellLocationConfirm:
                                {
									int sellLocationId = data["SellLocationId"]!.Value<int>();
									await DeleteSellLocationConfirmAsync(sellLocationId);
                                    await ShowSellLocationsAsync();
									return Trigger.Ignore;
                                }
                            case Command.DeleteSellLocation:
                                {
									int sellLocationId = data["SellLocationId"]!.Value<int>();
									await DeleteSellLocationAsync(sellLocationId);
									return Trigger.Ignore;
                                }
                            case Command.ChangeSellLocation:
                                {
									int sellLocationId = data["SellLocationId"]!.Value<int>();
									await SuggestChangeSellLocationNameAsync(sellLocationId);
									return Trigger.SellLocationEnterNameChange;
                                }
                            case Command.SellLocationDetails:
                                {
									int sellLocationId = data["SellLocationId"]!.Value<int>();
									await ShowSellLocationDetailsAsync(sellLocationId);
									return Trigger.Ignore;
                                }
                            case Command.AddSellLocation:
                                {
									await AddSellLocationSuggestEnterNameAsync();
									return Trigger.SellLocationEnterName;
                                }
                            case Command.BackToWelcome:
                                {
									await WelcomeAsync();
									return Trigger.Ignore;
                                }
                            case Command.ManageCatalog:
                                {
									await ShowCategoriesAsync();
									return Trigger.Ignore;
                                }
                            case Command.MovePaginationSellLocations:
                                {
									int page = data["P"]!.Value<int>();
									await ShowSellLocationsAsync(page);
									return Trigger.Ignore;
                                }
                            case Command.ManageSellLocations:
                                {
									await ShowSellLocationsAsync();
									return Trigger.Ignore;
                                }
                            case Command.MovePaginationCategories:
                                {
                                    int page = data["Page"]!.Value<int>();
                                    await ShowCategoriesAsync(page);
                                    return Trigger.Ignore;
                                }
                            case Command.Ignore:
                                {
                                    return Trigger.Ignore;
                                }
                            case Command.ShowCategoryProducts:
                                {
                                    int categoryId = data["CategoryId"]!.Value<int>();
                                    await ShowProductAsync(categoryId: categoryId);
                                    return Trigger.Ignore;
                                }
                            case Command.MovePaginationProduct:
                                {
                                    int categoryId = data["CategoryId"]!.Value<int>();
                                    int productId = data["ProductId"]!.Value<int>();
                                    await ShowProductAsync(categoryId: categoryId, productId: productId);
                                    return Trigger.Ignore;
                                }
                            case Command.ShowCategoryActions:
                            case Command.BackToCategory:
                                {
									int categoryId = data["CategoryId"]!.Value<int>();
									await ShowCategoryAsync(categoryId);
                                    return Trigger.Ignore;
                                }
                            case Command.BackToCategories:
                                {
									await ShowCategoriesAsync();
                                    return Trigger.Ignore;
                                }
                            case Command.EditProduct:
                                {
                                    int productId = data["ProductId"]!.Value<int>();
                                    ProductAttribute productAttribute = (ProductAttribute)data["ProductAttribute"]!.Value<int>();
                                    await SuggestEditProductAsync(productId, productAttribute);
                                    return Trigger.Ignore;
                                }
                            case Command.EditCategory:
                                {
                                    int categoryId = data["CategoryId"]!.Value<int>();
									CategoryAttribute categoryAttribute = (CategoryAttribute)data["CategoryAttribute"]!.Value<int>();
                                    await SuggestEditCategoryAsync(categoryId, categoryAttribute);
                                    return Trigger.Ignore;
                                }
                            case Command.ReturnToProduct:
                                {
                                    int productId = data["ProductId"]!.Value<int>();
                                    await ShowProductAsync(productId: productId);
                                    return Trigger.Ignore;
                                }
                            case Command.EditProductAttribute:
                                {
                                    ProductAttribute productAttribute = (ProductAttribute)data["ProductAttribute"]!.Value<int>();
                                    return await GetEditProductAttributeTriggerAsync(productAttribute);
                                }
                            case Command.EditCategoryAttribute:
                                {
									CategoryAttribute categoryAttribute = (CategoryAttribute)data["CategoryAttribute"]!.Value<int>();
                                    return await GetEditCategoryAttributeTriggerAsync(categoryAttribute);
                                }
                            case Command.EditProductVisibility:
                                {
                                    bool isVisible = data["Visibility"]!.Value<bool>();
                                    await ChangeProductAttributeValueAsync(isVisible);
                                    return Trigger.Ignore;
                                }
                            case Command.EditCategoryVisibility:
                                {
                                    bool isVisible = data["Visibility"]!.Value<bool>();
                                    await ChangeCategoryAttributeValueAsync(isVisible);
                                    return Trigger.Ignore;
                                }
                            case Command.AddProduct:
                                {
                                    await EnterNewProductNameAsync();
                                    return Trigger.EnterProductName;
                                }
                            case Command.AddCategory:
                                {
                                    await EnterNewCategoryNameAsync();
                                    return Trigger.EnterCategoryName;
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
                                    if(ProductId == null)
                                    {
                                        throw new Exception($"TryNextStepAsync Пустой ProductId");
                                    }

                                    ProductAttribute productAttribute = (ProductAttribute)data["ProductAttribute"]!.Value<int>();
                                    await SuggestEditProductAsync(ProductId.Value, productAttribute);
                                    return Trigger.ReturnToProductAttributeEditor;
                                }
                            case Command.EditProductAttributeYes:
                                {
                                    ProductAttribute productAttribute = (ProductAttribute)data["ProductAttribute"]!.Value<int>();
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
                                    return Trigger.ReturnToCatalog;
                                }
                        }
                        break;
                    }
                case State.CategoryNameEditor:
                    {
                        switch(command)
                        {
                            case Command.EditCategory:
                                {
									if (CategoryId == null)
									{
										throw new Exception($"TryNextStepAsync Пустой CategoryId");
									}

									CategoryAttribute attribute = (CategoryAttribute)data["CategoryAttribute"]!.Value<int>();
									await SuggestEditCategoryAsync(CategoryId.Value, attribute);
									return Trigger.ReturnToCatalogEditor;
								}

							case Command.EditCategoryAttributeYes:
								{
									CategoryAttribute attribute = (CategoryAttribute)data["CategoryAttribute"]!.Value<int>();
									await ChangeCategoryAttributeValueAsync(attribute);
									return Trigger.ReturnToCatalogEditor;
								}
						}
                        break;
                    }
				case State.NewCategoryNameEditor:
					{
						switch (command)
                        {
                            case Command.AddCategory:
                                {
                                    await AddCategoryAsync();
									return Trigger.ReturnToCatalogEditor;
								}
							case Command.BackToCategories:
								{
									await ShowCategoriesAsync();
									return Trigger.ReturnToCatalogEditor;
								}
						}
                        break;
					}
				case State.CatalogEditorSellLocationEnterName:
					{
						switch (command)
                        {
							case Command.BackToSellLocations:
								{
                                    NewSellLocation = null;
									await ShowSellLocationsAsync();
									return Trigger.ReturnToCatalogEditor;
								}
                            case Command.AddSellLocation:
                                {
                                    await AddSellLocationAsync();
									await ShowSellLocationsAsync();
									return Trigger.ReturnToCatalogEditor;
								}
						}
                        break;
					}
				case State.CatalogEditorSellLocationEnterNameChange:
					{
						switch (command)
                        {
							case Command.SellLocationDetails:
								{
									int sellLocationId = data["SellLocationId"]!.Value<int>();
									await ShowSellLocationDetailsAsync(sellLocationId);
									return Trigger.ReturnToCatalogEditor;
								}
						}
                        break;
					}
			}

            return null;
        }

		private async Task DeleteSellLocationConfirmAsync(int sellLocationId)
		{
            TryGetSellLocation(sellLocationId, out SellLocation sellLocation);

            _dataSource.SellLocations.Remove(sellLocation);
            await _dataSource.SaveChangesAsync();

            await _stateManager.SendMessageAsync(CatalogEditorText.SellLocationDeleteSuccess);
		}

		private async Task DeleteSellLocationAsync(int sellLocationId)
		{
			CheckSellLocation(sellLocationId);

            InlineKeyboardMarkup markup = GetDeleteSellLocationConfirmButtons(sellLocationId);
            await _stateManager.SendMessageAsync(CatalogEditorText.DeleteSellLocationConfirm, markup);
		}

		private InlineKeyboardMarkup GetDeleteSellLocationConfirmButtons(int sellLocationId)
		{
			InlineKeyboardButton[] keyboard =
			[
				new (MessagesText.Yes)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.DeleteSellLocationConfirm,
                        SellLocationId = sellLocationId
					})
				},
				new (MessagesText.No)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.SellLocationDetails,
                        SellLocationId = sellLocationId
					})
				},
			];

			InlineKeyboardMarkup result = new(keyboard);
			return result;
		}

		private async Task SellLocationChangeSaveAsync(string? text)
		{
			_stateManager.CheckTextAndLength(text,
				minLength: SellLocation.MIN_NAME_LENGTH,
				maxLength: SellLocation.MAX_NAME_LENGTH);

            if (SellLocationChangingId == null)
            {
                throw new CustomMessageException("Данные утеряны. Попробуйте еще раз от главного меню");
            }

            TryGetSellLocation(SellLocationChangingId.Value, out SellLocation sellLocation);

            sellLocation.Name = text!;
            _dataSource.SellLocations.Update(sellLocation);
            await _dataSource.SaveChangesAsync();

            await _stateManager.SendMessageAsync(CatalogEditorText.SellLocationChangeSuccess);
		}

		private async Task SuggestChangeSellLocationNameAsync(int sellLocationId)
		{
			TryGetSellLocation(sellLocationId, out SellLocation sellLocation);

            SellLocationChangingId = sellLocationId;

			InlineKeyboardMarkup markup = GetChangeSellLocationNameButtons(sellLocationId);

			await _stateManager.SendMessageAsync(CatalogEditorText.SuggestChangeSellLocationName, markup);
		}

		private InlineKeyboardMarkup GetChangeSellLocationNameButtons(int sellLocationId)
		{
			InlineKeyboardButton keyboard = new (CatalogEditorText.Back)
			{
				CallbackData = JsonConvert.SerializeObject(new
				{
					Cmd = Command.SellLocationDetails,
                    SellLocationId = sellLocationId
				})
			};

			InlineKeyboardMarkup result = new(keyboard);
			return result;
		}

		private async Task ShowSellLocationDetailsAsync(int sellLocationId)
		{
            TryGetSellLocation(sellLocationId, out SellLocation sellLocation);

            InlineKeyboardMarkup markup = GetSellLocationDetailsButtons(sellLocationId);
            string text = string.Format(CatalogEditorText.SellLocationDetails, sellLocation!.Name);
            await _stateManager.SendMessageAsync(text, markup);
		}

		private void CheckSellLocation(int sellLocationId)
        {
            TryGetSellLocation(sellLocationId, out _);
        }

		private void TryGetSellLocation(int sellLocationId, out SellLocation sellLocation)
		{
			SellLocation? sellLocationNullable = _dataSource.SellLocations
				.FirstOrDefault(sl => sl.Id == sellLocationId);

			if (sellLocationNullable == null)
			{
				throw new CustomMessageException("Точка продаж не найдена", 
                    "Id точки продаж не найдено при попытке просмотра подробностей о точке продаж");
			}

            sellLocation = sellLocationNullable;
		}

		private InlineKeyboardMarkup GetSellLocationDetailsButtons(int sellLocationId)
		{
            InlineKeyboardButton[][] keyboard =
            [
                [
                    new (CatalogEditorText.ChangeSellLocation)
                    {
						CallbackData = JsonConvert.SerializeObject(new
					    {
						    Cmd = Command.ChangeSellLocation,
                            SellLocationId = sellLocationId
					    })
					}
                ],
                [
                    new (CatalogEditorText.DeleteSellLocation)
                    {
						CallbackData = JsonConvert.SerializeObject(new
					    {
						    Cmd = Command.DeleteSellLocation,
							SellLocationId = sellLocationId
						})
					}
                ],
                [
                    new (CatalogEditorText.Back)
                    {
						CallbackData = JsonConvert.SerializeObject(new
					    {
						    Cmd = Command.ManageSellLocations
						})
					}
                ],
            ];

			InlineKeyboardMarkup result = new(keyboard);
			return result;
		}

		private async Task AddSellLocationAsync()
		{
			if (NewSellLocation == null)
            {
                throw new Exception("NewSellLocation не существует при попытке добавления новой точки продаж");
            }

            await _dataSource.SellLocations.AddAsync(NewSellLocation);
            await _dataSource.SaveChangesAsync();

            await _stateManager.SendMessageAsync(CatalogEditorText.SellLocationAdded);
		}

		private async Task SellLocationSuggestConfirmAsync()
		{
			InlineKeyboardMarkup markup = GetSellLocationSuggestConfirmAsyncButtons();
			await _stateManager.SendMessageAsync(CatalogEditorText.SellLocationSuggestConfirm, markup);
		}

		private InlineKeyboardMarkup GetSellLocationSuggestConfirmAsyncButtons()
		{
			InlineKeyboardButton[] keyboard =
			[
				new (MessagesText.Yes)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.AddSellLocation,
					})
				},
				new (MessagesText.No)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.BackToSellLocations,
					})
				},
			];

			InlineKeyboardMarkup result = new(keyboard);
			return result;
		}

		private void SellLocationEnterName(string? text)
		{
            _stateManager.CheckTextAndLength(text,
                minLength: SellLocation.MIN_NAME_LENGTH,
                maxLength: SellLocation.MAX_NAME_LENGTH);

            NewSellLocation = new SellLocation()
            {
                Name = text!
			};
		}

		private async Task AddSellLocationSuggestEnterNameAsync()
		{
            InlineKeyboardMarkup markup = GetAddSellLocationSuggestEnterNameButtons();

			await _stateManager.SendMessageAsync(CatalogEditorText.SellLocationSuggestEnterName);
		}

		private InlineKeyboardMarkup GetAddSellLocationSuggestEnterNameButtons()
		{
            InlineKeyboardButton[] keyboard =
            [
                new (CatalogEditorText.Back)
                {
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.BackToSellLocations,
					})
				}
            ];

            InlineKeyboardMarkup result = new(keyboard);
            return result;
		}

		private async Task ShowSellLocationsAsync(int page=1)
		{
			IQueryable<SellLocation> sellLocations = _dataSource.SellLocations
				.Skip((page - 1) * SELL_LOCATIONS_BY_PAGE)
				.Take(SELL_LOCATIONS_BY_PAGE);

			int sellLocationsCount = _dataSource.SellLocations.Count();

			InlineKeyboardMarkup markup = GetSellLocationsButtons(sellLocations, sellLocationsCount, page);
			await _stateManager.SendMessageAsync(CatalogEditorText.SellLocations, markup);
		}

		private InlineKeyboardMarkup GetSellLocationsButtons(IQueryable<SellLocation> sellLocations, int sellLocationsCount, int page)
		{
			List<InlineKeyboardButton[]> keyboard = new List<InlineKeyboardButton[]>();

			if (sellLocations.Any())
			{
				keyboard = sellLocations
					.AsEnumerable()
					.Select(sellLocations =>
					{
						InlineKeyboardButton[] row = new InlineKeyboardButton[]
						{
								new InlineKeyboardButton(sellLocations.Name)
								{
									CallbackData = JsonConvert.SerializeObject(new
									{
										Cmd = Command.SellLocationDetails,
										SellLocationId = sellLocations.Id,
									})
								},

						}
						.ToArray();

						return row;
					})
					.ToList();

				InlineKeyboardButton[] pagination = TelegramHelper.GetPagination(page, sellLocationsCount, SELL_LOCATIONS_BY_PAGE, Command.MovePaginationSellLocations);
				keyboard.Add(pagination);
			}
			else
			{
				keyboard =
					[
						[
							new(MessagesText.Empty)
							{
								CallbackData = JsonConvert.SerializeObject(new
								{
									Cmd = Command.Ignore,
								})
							}
						]
					];
			}

			keyboard.AddRange(
            [
			    [
				    new InlineKeyboardButton(CatalogEditorText.AddSellLocation)
				    {
					    CallbackData = JsonConvert.SerializeObject(new
					    {
						    Cmd = Command.AddSellLocation,
					    })
				    }
			    ],
			    [
				    new InlineKeyboardButton(CatalogEditorText.Back)
				    {
					    CallbackData = JsonConvert.SerializeObject(new
					    {
						    Cmd = Command.BackToWelcome,
					    })
				    }
			    ],
            ]);

			InlineKeyboardMarkup result = new InlineKeyboardMarkup(keyboard);
			return result;
		}

		private async Task WelcomeAsync()
		{
            InlineKeyboardMarkup markup = GetWelcomeButtons();
            await _stateManager.SendMessageAsync(CatalogEditorText.Welcome, markup);
		}

		private InlineKeyboardMarkup GetWelcomeButtons()
		{
            InlineKeyboardButton[][] keyboard =
            [
                [
                    new (CatalogEditorText.ManageCatalog)
                    {
						CallbackData = JsonConvert.SerializeObject(new
					    {
						    Cmd = Command.ManageCatalog,
					    })
					}
                ],
                [
                    new (CatalogEditorText.ManageSellLocations)
                    {
						CallbackData = JsonConvert.SerializeObject(new
					    {
						    Cmd = Command.ManageSellLocations,
					    })
					}
                ],
            ];

            InlineKeyboardMarkup result = new(keyboard);
            return result;
		}

		private async Task AddCategoryAsync()
        {
            if(NewCategory == null)
            {
                throw new Exception("При попытке добавления категории она не найдена");
            }

			await _dataSource.Categories.AddAsync(NewCategory);
            await _dataSource.SaveChangesAsync();

            await _stateManager.SendMessageAsync(CatalogEditorText.NewCategoryRemind);

            await ShowCategoryAsync(NewCategory.Id);
		}

		private async Task AddProductAsync()
        {
            if(NewProduct == null)
            {
                throw new Exception("При попытке добавления товара он не найден");
            }
            if (CategoryId == null || !await _dataSource.Categories.AnyAsync(c => c.Id == CategoryId))
            {
                throw new Exception("При попытке добавления товара категория не найдена");
            }

            NewProduct.IsVisible = true;

            ProductCategory product = new ProductCategory();
            product.Product = NewProduct;
            product.CategoryId = CategoryId.Value;

            await _dataSource.ProductCategories.AddAsync(product);
            await _dataSource.SaveChangesAsync();
        }

        private async Task<Trigger> EditNewCategoryAsync(Message message, CategoryAttribute attribute)
        {
			_stateManager.CheckText(message.Text);

			Trigger? result = null;
            string? nextAttributeText = null;

            if(NewCategory == null)
            {
				NewCategory = new Category() { Name = string.Empty };
            }

            switch (attribute)
            {
                case CategoryAttribute.Name:
                    {
                        string? text = message.Text;
						string? error = CheckTextLength(text, 32);

                        if(error != null)
                        {
                            await _stateManager.SendMessageAsync(error);
                            return Trigger.Ignore;
                        }

                        NewCategory.Name = text!;
                        result = Trigger.Ignore;
                        break;
                    }
            }

            if (nextAttributeText == null)
            {
                string text = string.Format(CatalogEditorText.CategoryPreview, NewCategory.Name);
                InlineKeyboardButton[] add = GetAddNewCategoryButton();
                InlineKeyboardMarkup markup = new InlineKeyboardMarkup(add);

                await _stateManager.SendMessageAsync(text, markup: markup);
            }
            else
            {
                string text = string.Format(CatalogEditorText.EnterProductAttribute, nextAttributeText);
                await _stateManager.SendMessageAsync(text);
            }

            return result!.Value;
        }

        private async Task<Trigger> EditNewProductAsync(Message message, ProductAttribute productAttribute)
        {
            Trigger? result = null;
            string nextAttributeText = null;
            bool isInvalidValue = false;

            if(NewProduct.IsNull())
            {
                NewProduct = new Product();
            }

            switch (productAttribute)
            {
                case ProductAttribute.Name:
                    {
                        NewProduct.Name = message.Text;
                        nextAttributeText = CatalogEditorText.ProductDesciption;
                        result = Trigger.EnterProductDescription;
                        break;
                    }
                case ProductAttribute.Description:
                    {
                        NewProduct.Description = message.Text;
                        nextAttributeText = CatalogEditorText.ProductPrice;
                        result = Trigger.EnterProductPrice;
                        break;
                    }
                case ProductAttribute.Price:
                    {
                        if (decimal.TryParse(message.Text, out decimal price))
                        {
                            NewProduct.Price = price;
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
                            NewProduct.Photo = await _stateManager.GetFileAsync(fileId);
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
                NewProduct.Name,
                NewProduct.Description,
                NewProduct.Price.ToString(TelegramHelper.PRICE_FORMAT));

                InlineKeyboardButton[] AddProduct = GetAddNewProductButton();
                InlineKeyboardMarkup markup = new InlineKeyboardMarkup(AddProduct);

                await _stateManager.SendMessageAsync(caption, parseMode: ParseMode.Html, markup: markup, photo: NewProduct.Photo);
            }
            else
            {
                string text = string.Format(CatalogEditorText.EnterProductAttribute, nextAttributeText);
                await _stateManager.SendMessageAsync(text);
            }

            return result.Value;
        }

        private InlineKeyboardButton[] GetAddNewCategoryButton()
        {
            InlineKeyboardButton[] result = new InlineKeyboardButton[]
            {
                new InlineKeyboardButton(CatalogEditorText.Add)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.AddCategory,
                    })
                },
                new InlineKeyboardButton(CatalogEditorText.Cancel)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.BackToCategories,
                    })
                }
            };

            return result;
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

        private async Task EnterNewProductNameAsync()
        {
            string text = string.Format(CatalogEditorText.EnterProductAttribute, CatalogEditorText.ProductName);
            await _stateManager.SendMessageAsync(text);
        }

        private async Task EnterNewCategoryNameAsync()
        {
            string text = string.Format(CatalogEditorText.EnterProductAttribute, CatalogEditorText.CategoryName);
            InlineKeyboardButton[] back = GetBackCategoryButton();
            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(back);

            await _stateManager.SendMessageAsync(text, markup: markup);
        }

        private async Task ChangeCategoryAttributeValueAsync(bool isVisible)
        {
            if(!CategoryId.HasValue)
            {
				throw new Exception($"ChangeCategoryAttributeValueAsync Пустой CategoryId");
			}

			Category? category = await _dataSource.Categories
                .FirstOrDefaultAsync(p => p.Id == CategoryId);

			if (category == null)
			{
				throw new Exception($"ChangeCategoryAttributeValueAsync Не удалось найти категорию: id={CategoryId}");
			}

			category.IsVisible = isVisible;

            await _dataSource.SaveChangesAsync();

            await SuggestEditCategoryAsync(CategoryId.Value, CategoryAttribute.Visibility);
        }

        private async Task ChangeProductAttributeValueAsync(bool isVisible)
        {
            if(!ProductId.HasValue)
            {
				throw new Exception($"ChangeProductAttributeValueAsync Пустой ProductId");
			}

			Product? product = await _dataSource.Products
                .FirstOrDefaultAsync(p => p.Id == ProductId);

			if (product.IsNull())
			{
				throw new Exception($"ChangeProductAttributeValueAsync Не удалось найти товар: id={ProductId}");
			}

			product!.IsVisible = isVisible;

            await _dataSource.SaveChangesAsync();

            await SuggestEditProductAsync(ProductId.Value, ProductAttribute.Visibility);
        }

        private async Task ChangeCategoryAttributeValueAsync(CategoryAttribute attribute)
        {
			if (!CategoryId.HasValue)
			{
				throw new Exception($"ChangeCategoryAttributeValueAsync Пустой ProductId");
			}

            if (AttributeValue == null)
            {
				throw new Exception($"ChangeCategoryAttributeValueAsync Пустой AttributeValue");
			}

			Category? category = await _dataSource.Categories
				.FirstOrDefaultAsync(p => p.Id == CategoryId);

			if (category == null)
            {
                throw new Exception($"ChangeCategoryAttributeValueAsync Не удалось найти категорию: id={CategoryId}");
            }

            switch(attribute)
            {
                case CategoryAttribute.Name:
                    {
                        category.Name = AttributeValue;
                        break;
                    }
                default:
                    {
                        ProductId = null;
                        AttributeValue = null!;
                        throw new Exception($"ChangeCategoryAttributeValueAsync Неизвестный тип атрибута: {attribute.ToString()}");
                    }
            }

            await _dataSource.SaveChangesAsync();

            AttributeValue = null!;

            await SuggestEditCategoryAsync(CategoryId.Value, attribute);
        }

        private async Task ChangeProductAttributeValueAsync(ProductAttribute productAttribute)
        {
			if (!ProductId.HasValue)
			{
				throw new Exception($"ChangeProductAttributeValueAsync Пустой ProductId");
			}

            if (AttributeValue == null)
            {
				throw new Exception($"ChangeProductAttributeValueAsync Пустой AttributeValue");
			}

			Product? product = await _dataSource.Products
				.FirstOrDefaultAsync(p => p.Id == ProductId);

			if (product == null)
            {
                throw new Exception($"ChangeProductAttributeValueAsync Не удалось найти товар: id={ProductId}");
            }

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
                        AttributeValue = null!;
                        throw new Exception($"ChangeProductAttributeValueAsync Неизвестный тип атрибута: {productAttribute.ToString()}");
                    }
            }

            await _dataSource.SaveChangesAsync();

            AttributeValue = null!;

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

            await _stateManager.SendMessageAsync(CatalogEditorText.Approve, markup: markup);
        }

        private async Task EditPriceAsync(string? text)
        {
			_stateManager.CheckText(text);

			if (!decimal.TryParse(text, out var _))
            {
                await _stateManager.SendMessageAsync(CatalogEditorText.WrongValue);
                return;
            }

            AttributeValue = text;

            InlineKeyboardButton[] yesNo = GetYesNoChangeAttributeButtons(ProductAttribute.Price);
			InlineKeyboardMarkup markup = new InlineKeyboardMarkup(yesNo);

            await _stateManager.SendMessageAsync(CatalogEditorText.Approve, markup: markup);
        }

        private async Task EditDescriptionAsync(string? text)
        {
			_stateManager.CheckTextAndLength(text, maxLength:255);

			AttributeValue = text;

            InlineKeyboardButton[] yesNo = GetYesNoChangeAttributeButtons(ProductAttribute.Description);

            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(yesNo);

            await _stateManager.SendMessageAsync(CatalogEditorText.Approve, markup: markup);
        }

        private async Task EditNameAsync(string? text)
        {
			_stateManager.CheckTextAndLength(text, 255);

			AttributeValue = text;

            InlineKeyboardButton[] yesNo = GetYesNoChangeAttributeButtons(ProductAttribute.Name);

            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(yesNo);

            await _stateManager.SendMessageAsync(CatalogEditorText.Approve, markup: markup);
        }

        private async Task EditCategoryNameAsync(string? text)
        {
			_stateManager.CheckTextAndLength(text, maxLength:32);

			AttributeValue = text;

            InlineKeyboardButton[] yesNo = GetYesNoChangeAttributeButtons(CategoryAttribute.Name);

            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(yesNo);

            await _stateManager.SendMessageAsync(CatalogEditorText.Approve, markup: markup);
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

        private InlineKeyboardButton[] GetYesNoChangeAttributeButtons(CategoryAttribute attribute)
        {
            InlineKeyboardButton[] result = new InlineKeyboardButton[]
            {
                new InlineKeyboardButton(CatalogEditorText.Yes)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                            {
                                Cmd = Command.EditCategoryAttributeYes,
								CategoryAttribute = attribute
                            })
                },
                new InlineKeyboardButton(CatalogEditorText.No)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                            {
                                Cmd = Command.EditCategory,
								CategoryAttribute = attribute
                            })
                },
            };

            return result;
        }

        private async Task<Trigger?> GetEditCategoryAttributeTriggerAsync(CategoryAttribute attribute)
        {
            Trigger? result = null;
            switch (attribute)
            {
                case CategoryAttribute.Name:
                    {
                        result = Trigger.EditCategoryName;
                        break;
                    }
                default:
                    {
                        throw new Exception("GetEditCategoryAttributeTriggerAsync Неизвестный тип атрибута");
                    }
            }

            if(CategoryId == null)
            {
                throw new Exception($"GetEditCategoryAttributeTriggerAsync Пустой CategoryId");
            }

            InlineKeyboardButton[] returnToAttributeMenu = GetEditCategoryAttributeButton(attribute, CatalogEditorText.ReturnToAttributeEditor, CategoryId.Value);

            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(returnToAttributeMenu);

            await _stateManager.SendMessageAsync(CatalogEditorText.EnterValue, markup: markup);

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
                        throw new Exception("GetEditProductAttributeTriggerAsync Неизвестный тип атрибута");
                    }
            }

            InlineKeyboardButton[] returnToAttributeMenu = GetEditProductButton(productAttribute, CatalogEditorText.ReturnToAttributeEditor);

            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(returnToAttributeMenu);

            await _stateManager.SendMessageAsync(CatalogEditorText.EnterValue, markup: markup);

            return result;
        }

		private async Task SuggestEditCategoryAsync(int categoryId, CategoryAttribute categoryAttribute)
		{
			switch (categoryAttribute)
            {
                case CategoryAttribute.Name:
                    {
                        await SuggestEditCategoryNameAsync(categoryId);
						break;
					}
                case CategoryAttribute.Visibility:
                    {
                        await SuggestEditCategoryVisibilityAsync(categoryId);
						break;
					}
				default:
					{
						ProductId = null;
						throw new Exception($"SuggestEditCategoryAsync Неизвестный тип атрибута: CategoryAttribute={categoryAttribute.ToString()}");
					}
			}
		}

		private async Task SuggestEditCategoryVisibilityAsync(int categoryId)
		{
			bool? isVisible = _dataSource.Categories.FirstOrDefault(product => product.Id == categoryId)?.IsVisible;

			string attributeValue = isVisible.HasValue && isVisible.Value ?
				CatalogEditorText.Yes :
				CatalogEditorText.No;

			if (attributeValue.IsNullOrEmpty())
			{
				attributeValue = CatalogEditorText.AttributeNoValue;
			}

			InlineKeyboardButton[] editAttribute = GetEditCategoryVisibilityButton(categoryId, isVisible);
			InlineKeyboardButton[] nextAttribute = GetEditCategoryAttributeButton(CategoryAttribute.Name, CatalogEditorText.Next, categoryId);
			InlineKeyboardButton[] returnToCategory = GetReturnToCategoryButton(categoryId);

			string text = string.Format(CatalogEditorText.EditProductAttributeTemplate,
				CatalogEditorText.CategoryVisibility,
				attributeValue);

			InlineKeyboardMarkup markup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
			{
				editAttribute,
				nextAttribute,
				returnToCategory
			});

			await _stateManager.SendMessageAsync(text, parseMode: ParseMode.Html, markup: markup);
		}

		private async Task SuggestEditCategoryNameAsync(int categoryId)
		{
			string? attributeValue = _dataSource.Categories.FirstOrDefault(p => p.Id == categoryId)?.Name;

			if (attributeValue.IsNullOrEmpty())
			{
				attributeValue = CatalogEditorText.AttributeNoValue;
			}

			InlineKeyboardButton[] editAttribute = GetEditAttributeCategoryButton(categoryId, CategoryAttribute.Name);
			InlineKeyboardButton[] returnToCategory = GetReturnToCategoryButton(categoryId);

			string text = string.Format(CatalogEditorText.EditProductAttributeTemplate,
				CatalogEditorText.CategoryName,
				attributeValue);

			InlineKeyboardMarkup markup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
			{
				editAttribute,
				returnToCategory
			});

			await _stateManager.SendMessageAsync(text, parseMode: ParseMode.Html, markup: markup);
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
                        throw new Exception($"SuggestEditProductAsync Неизвестный тип атрибута: ProductAttribute={productAttribute.ToString()}");
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


            await _stateManager.SendMessageAsync(text, markup, ParseMode.Html);
        }
        private async Task SuggestEditProductPhotoAsync(int productId)
        {
            string? attributeValue = _dataSource.Products.FirstOrDefault(product => product.Id == productId)?.Photo;

            InlineKeyboardButton[] editAttribute = GetEditProductAttributeButton(productId, ProductAttribute.Photo);
            InlineKeyboardButton[] returnToProduct = GetReturnToProductButton(productId);

            string photoText = attributeValue.IsNullOrEmpty() ?
                CatalogEditorText.AttributeNoValue :
                CatalogEditorText.LookPhoto;

            string text = string.Format(CatalogEditorText.EditProductAttributeTemplate,
                CatalogEditorText.ProductPhoto,
                photoText);

            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
            {
                editAttribute,
                returnToProduct
            });

            await _stateManager.SendMessageAsync(text, markup, ParseMode.Html, photo: attributeValue);
        }

        private async Task SuggestEditProductPriceAsync(int productId)
        {
            string? attributeValue = _dataSource.Products.FirstOrDefault(product => product.Id == productId)?.Price.ToString();

            if (attributeValue.IsNullOrEmpty())
            {
                attributeValue = CatalogEditorText.AttributeNoValue;
            }

            InlineKeyboardButton[] editAttribute = GetEditProductAttributeButton(productId, ProductAttribute.Price);
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

            await _stateManager.SendMessageAsync(text, parseMode: ParseMode.Html, markup: markup);
        }

        private async Task SuggestEditProductDescriptionAsync(int productId)
        {
            string? attributeValue = _dataSource.Products.FirstOrDefault(product => product.Id == productId)?.Description;

            if (attributeValue.IsNullOrEmpty())
            {
                attributeValue = CatalogEditorText.AttributeNoValue;
            }

            InlineKeyboardButton[] editAttribute = GetEditProductAttributeButton(productId, ProductAttribute.Description);
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

            await _stateManager.SendMessageAsync(text, parseMode: ParseMode.Html, markup: markup);
        }

        private async Task SuggestEditProductNameAsync(int productId)
        {
            string? attributeValue = _dataSource.Products.FirstOrDefault(p => p.Id == productId)?.Name;

            if (attributeValue.IsNullOrEmpty())
            {
                attributeValue = CatalogEditorText.AttributeNoValue;
            }

            InlineKeyboardButton[] editAttribute = GetEditProductAttributeButton(productId, ProductAttribute.Name);
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

            await _stateManager.SendMessageAsync(text, parseMode: ParseMode.Html, markup: markup);
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

        private InlineKeyboardButton[] GetEditCategoryVisibilityButton(int categoryId, bool? isVisible)
        {
			InlineKeyboardButton[] result;
			if (isVisible.HasValue && isVisible.Value)
            {
                result = new InlineKeyboardButton[]{
                        new InlineKeyboardButton(CatalogEditorText.VisibilityOff)
                        {
                            CallbackData = JsonConvert.SerializeObject(new
                            {
                                Cmd = Command.EditCategoryVisibility,
								CategoryId = categoryId,
                                Visibility = false
                            })
                        }
                    };
            }
            else
            {
                result = new InlineKeyboardButton[]{
                        new InlineKeyboardButton(CatalogEditorText.VisibilityOn)
                        {
                            CallbackData = JsonConvert.SerializeObject(new
                            {
                                Cmd = Command.EditCategoryVisibility,
								CategoryId = categoryId,
                                Visibility = true
                            })
                        }
                    };
            }
            

            return result;
        }
        private InlineKeyboardButton[] GetEditProductVisibilityButton(int productId, bool? isVisible)
        {
			InlineKeyboardButton[] result;
			if (isVisible.HasValue && isVisible.Value)
            {
                result = new InlineKeyboardButton[]{
                        new InlineKeyboardButton(CatalogEditorText.VisibilityOff)
                        {
                            CallbackData = JsonConvert.SerializeObject(new
                            {
                                Cmd = Command.EditProductVisibility,
                                ProductId = productId,
                                Visibility = false
                            })
                        }
                    };
            }
            else
            {
                result = new InlineKeyboardButton[]{
                        new InlineKeyboardButton(CatalogEditorText.VisibilityOn)
                        {
                            CallbackData = JsonConvert.SerializeObject(new
                            {
                                Cmd = Command.EditProductVisibility,
                                ProductId = productId,
                                Visibility = true
                            })
                        }
                    };
            }
            

            return result;
        }

		private InlineKeyboardButton[] GetEditAttributeCategoryButton(object categoryId, CategoryAttribute attribute)
		{
			InlineKeyboardButton[] result = new InlineKeyboardButton[]{
						new InlineKeyboardButton(CatalogEditorText.Change)
						{
							CallbackData = JsonConvert.SerializeObject(new
							{
								Cmd = Command.EditCategoryAttribute,
								CategoryId = categoryId,
								CategoryAttribute = attribute
							})
						}
					};

			return result;
		}

		private InlineKeyboardButton[] GetEditProductAttributeButton(int productId, ProductAttribute attribute)
        {
            InlineKeyboardButton[] result = new InlineKeyboardButton[]{
                        new InlineKeyboardButton(CatalogEditorText.Change)
                        {
                            CallbackData = JsonConvert.SerializeObject(new
                            {
                                Cmd = Command.EditProductAttribute,
                                ProductId = productId,
                                ProductAttribute = attribute
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
            Product? product = null;
            List<InlineKeyboardButton[]> keyboard;
            string text = null!;

            if(productId.IsNull() && categoryId.IsNull())
            {
                throw new Exception("не указан продукт и категория");
            }

            if (categoryId.IsNull())
            {
                categoryId = (await _dataSource.ProductCategories
                    .FirstOrDefaultAsync(pc => pc.ProductId == productId))
                    ?.CategoryId;

                if(categoryId.IsNull())
                {
                    throw new Exception($"Не удалось найти категорию для productId={productId}");
                }
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

            List<InlineKeyboardButton[]> actions = GetProductActions(categoryId!.Value, product?.Id);
            InlineKeyboardButton[] returnToCatalog = GetReturnToCategoryButton(categoryId.Value);

            keyboard = actions;

            if (product.IsNull())
            {
                text = CatalogEditorText.ProductsNotFound;
            }
            else
            {
                ProductId = product!.Id;

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

            await _stateManager.SendMessageAsync(text, markup, ParseMode.Html, photo: product?.Photo);
        }

        /// <summary>
        /// получение кнопки возврата в выбор категории
        /// </summary>
        /// <returns></returns>
        private InlineKeyboardButton[] GetReturnToCategoryButton(int categoryId)
        {
            InlineKeyboardButton[] result = new InlineKeyboardButton[]{
                        new InlineKeyboardButton(CatalogEditorText.BackToCategory)
                        {
                            CallbackData = JsonConvert.SerializeObject(new
                            {
                                Cmd = Command.BackToCategory,
                                CategoryId = categoryId
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
        private InlineKeyboardButton[] GetAddProductButton()
        {
            InlineKeyboardButton[] result = new InlineKeyboardButton[]
            { new InlineKeyboardButton(CatalogEditorText.AddProduct)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.AddProduct
                    })
                } 
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

        private InlineKeyboardButton[] GetEditCategoryAttributeButton(CategoryAttribute attribute, string text, int categoryId)
        {
            InlineKeyboardButton[] result = new InlineKeyboardButton[]
            {
                new InlineKeyboardButton(text)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.EditCategory,
                        CategoryId = categoryId,
						CategoryAttribute = attribute
                    })
                } 
            };

            return result;
        }

		private async Task ShowCategoriesAsync(int page = 1)
		{
            CategoryId = null;

            IEnumerable<Category> categories = _dataSource.Categories
                .ToList();

			 IEnumerable<Category> filteredCategories = categories
				.Skip((page - 1) * CATEGORIES_BY_PAGE)
							.Take(CATEGORIES_BY_PAGE)
							.ToList();

			List<IEnumerable<InlineKeyboardButton>> categoriesButtons = GetCategoriesButtons(categories, filteredCategories, page);
            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(categoriesButtons);

            await _stateManager.SendMessageAsync(CatalogEditorText.ChoosingCategory, markup: markup);
        }

        private IEnumerable<InlineKeyboardButton> GetCategoriesPaginationButtons(int page, IEnumerable<Category> categories)
		{
			List<InlineKeyboardButton> result = new List<InlineKeyboardButton>();

			if (page > 1)
			{
				result.Add(new InlineKeyboardButton(MessagesText.PaginationPrevious)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.MovePaginationCategories,
						Page = page - 1,
					})
				});
			}
			else
			{
				result.Add(GetNoPaginationButton());
			}

			if (categories.Count() > page * CATEGORIES_BY_PAGE)
			{
				result.Add(new InlineKeyboardButton(MessagesText.PaginationNext)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.MovePaginationCategories,
						Page = page + 1,
					})
				});
			}
			else
			{
				result.Add(GetNoPaginationButton());
			}

			return result;
		}

		private List<IEnumerable<InlineKeyboardButton>> GetCategoriesButtons(IEnumerable<Category> categories, IEnumerable<Category> filteredCategories, int page)
		{
			List<IEnumerable<InlineKeyboardButton>> result = new List<IEnumerable<InlineKeyboardButton>>();
			if (filteredCategories.IsNullOrEmpty())
            {
                result.Add(new InlineKeyboardButton[]
                {
                    new InlineKeyboardButton(CatalogEditorText.Empty)
                    {
						CallbackData = JsonConvert.SerializeObject(new
					    {
						    Cmd = Command.Ignore,
					    })
					}
                });
            }
            else
            {
                result.AddRange(filteredCategories
                    .Select(category => new InlineKeyboardButton[]
                    {
                        new InlineKeyboardButton(category.Name)
                        {
							CallbackData = JsonConvert.SerializeObject(new
						    {
							    Cmd = Command.ShowCategoryActions,
                                CategoryId = category.Id,
						    })
						}
                    }));
            }

			IEnumerable<InlineKeyboardButton> pagination = GetCategoriesPaginationButtons(page, categories);
            result.Add(pagination);

			result.AddRange(
            [
                [
                    new InlineKeyboardButton(CatalogEditorText.AddCategory)
                    {
					    CallbackData = JsonConvert.SerializeObject(new
					    {
						    Cmd = Command.AddCategory,
					    })
				    }
                ],
                [
                    new InlineKeyboardButton(CatalogEditorText.Back)
                    {
					    CallbackData = JsonConvert.SerializeObject(new
					    {
						    Cmd = Command.BackToWelcome,
					    })
				    }
                ],
            ]);

            return result;
		}

		/// <summary>
		/// показать категорию
		/// </summary>
		/// <param name="categoryId"></param>
		/// <returns></returns>
		private async Task ShowCategoryAsync(int categoryId)
        {
            ProductId = null;

            Category? category = await _dataSource.Categories
                    .FirstOrDefaultAsync(c => c.Id == categoryId);

            if(category == null)
            {
                throw new Exception($"ShowCategoryAsync Не удалось найти категорию: id={categoryId}");
            }

			CategoryId = category.Id;

			string text = string.Format(CatalogEditorText.CategoryActions, category.Name);
            InlineKeyboardButton[][] actions = GetCategoryButtons(category);
            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(actions);

            await _stateManager.SendMessageAsync(text, markup: markup);
        }

        /// <summary>
        /// получение действий для категории
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private InlineKeyboardButton[][] GetCategoryButtons(Category category)
        {
            InlineKeyboardButton[] edit = GetEditCategoryButton();
            InlineKeyboardButton[] showProducts = GetShowProductsButton(category);
			InlineKeyboardButton[] addProduct = GetAddProductButton();
			InlineKeyboardButton[] back = GetBackCategoryButton();

			InlineKeyboardButton[][] result = new InlineKeyboardButton[][]
            {
				edit,
				showProducts,
				addProduct,
				back
			};

            return result;
        }

		private InlineKeyboardButton[] GetEditCategoryButton()
		{
			InlineKeyboardButton[] result = new InlineKeyboardButton[]
			{
				new InlineKeyboardButton(CatalogEditorText.EditCategory)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.EditCategory,
                        CategoryId = CategoryId,
						CategoryAttribute = CategoryAttribute.Visibility
					})
				}
			};

			return result;
		}

		private InlineKeyboardButton[] GetBackCategoryButton()
		{
            InlineKeyboardButton[] result = new InlineKeyboardButton[]
            {
                new InlineKeyboardButton(CatalogEditorText.BackToCategories)
                {
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.BackToCategories,
					})
				}
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
            List<InlineKeyboardButton[]> result = new List<InlineKeyboardButton[]>();

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
        private InlineKeyboardButton[] GetShowProductsButton(Category category)
        {
            InlineKeyboardButton[] result = new InlineKeyboardButton[]
            {
                new InlineKeyboardButton(CatalogEditorText.ShowProducts)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.ShowCategoryProducts,
                        CategoryId = category.Id
                    })
                }
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

                result = new InlineKeyboardButton(MessagesText.PaginationPrevious)
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

                result = new InlineKeyboardButton(MessagesText.PaginationNext)
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
            InlineKeyboardButton result = new InlineKeyboardButton(MessagesText.NoPagination)
            {
                CallbackData = JsonConvert.SerializeObject(new
                {
                    Cmd = Command.Ignore
                })
            };

            return result;
        }

        private string? CheckTextLength(string? text, int length)
        {
			if (text == null || text.IsEmpty())
			{
				return CatalogEditorText.TextRequired;
			}
			else if (text.Length > length)
			{
				return CatalogEditorText.TextTooLong;
			}

            return null;
		}
    }
}
