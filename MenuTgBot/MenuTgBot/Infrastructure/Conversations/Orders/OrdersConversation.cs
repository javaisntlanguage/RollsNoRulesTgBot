using Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using Newtonsoft.Json.Linq;
using MenuTgBot.Infrastructure.Models;
using MenuTgBot.Infrastructure.Conversations.Cart;
using Database.Tables;
using RabbitClient;
using NLog;
using MessageContracts;
using Telegram.Bot.Types.ReplyMarkups;
using Newtonsoft.Json;
using Helper;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Telegram.Bot.Types.Enums;

namespace MenuTgBot.Infrastructure.Conversations.Orders
{
    internal class OrdersConversation : IConversation
    {
        private const int SMS_RESEND_TIMER = 10;
        private const int SMS_LENGTH = 4;
        private readonly StateManager _stateManager;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static readonly RabbitEventHandler _rabbitEventHandler = new RabbitEventHandler(_logger);
        private ApplicationContext _dataSource;

        public DeliveryType? OrderDeliveryType { get; set; }
        public Address OrderAddress { get; set; }
        public long? OrderAddressId { get; set; }
        public string Phone { get; set; }
        public string PreaparedPhone { get; set; }
        public string SmsCode { get; set; }

        public OrdersConversation() { }

        public OrdersConversation(StateManager statesManager)
        {
            _stateManager = statesManager;
        }

        public async Task<Trigger?> TryNextStepAsync(ApplicationContext dataSource, Message message)
        {
            _dataSource = dataSource;

            switch (_stateManager.CurrentState)
            {
                case State.OrderNewAddressCityEditor:
                    {
                        return await EditNewAddressAsync(message.Text, AddressAttribute.City);
                    }
                case State.OrderNewAddressStreetEditor:
                    {
                        return await EditNewAddressAsync(message.Text, AddressAttribute.Street);
                    }
                case State.OrderNewAddressHouseNumberEditor:
                    {
                        return await EditNewAddressAsync(message.Text, AddressAttribute.HouseNumber);
                    }
                case State.OrderNewAddressBuildingEditor:
                    {
                        return await EditNewAddressAsync(message.Text, AddressAttribute.Building);
                    }
                case State.OrderNewAddressFlatEditor:
                    {
                        return await EditNewAddressAsync(message.Text, AddressAttribute.Flat);
                    }
                case State.OrderNewAddressCommentEditor:
                    {
                        return await EditNewAddressAsync(message.Text, AddressAttribute.Comment);
                    }
                case State.OrderPhone:
                    {
                        return await GetPhoneAsync(message);
                    }
                case State.SmsPhone:
                    {
                        return await CheckSmsAsync(message.Text);
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
                case State.CommandOrders:
                    {
                        switch(command)
                        {
                            case Command.TakeOrder:
                                {
                                    await ChooseDeliveryTypeAsync();
                                    return Trigger.Ignore;
                                }
                            case Command.ChooseDeivetyType:
                                {
                                    DeliveryType deliveryType = (DeliveryType)data["DeliveryType"].Value<int>();
                                    return await ChooseDeliveryAsync(deliveryType);
                                }
                            case Command.NewAddress:
                                {
                                    return await SuggestEditDeliveryAddressAsync(AddressAttribute.City);
                                }
                            case Command.ChangeAddress:
                                {
                                    await SuggestChooseAddressAsync();
                                    return Trigger.Ignore;
                                }
                            case Command.ChangePhone:
                                {
                                    await RequestPhoneAsync();
                                    return Trigger.ChangePhone;
                                }
                            case Command.AddressBack:
                                {
                                    await ShowDeliverySettingsAsync();
                                    return Trigger.Ignore;
                                }
                            case Command.ChooseDeliveryAddress:
                                {
                                    long addressId = data["AddressId"].Value<long>();
                                    OrderAddressId = addressId;
                                    await ShowDeliverySettingsAsync();
                                    return Trigger.Ignore;
                                }
                        }
                        break;
                    }
                case State.OrderNewAddressHouseNumberEditor:
                    {
                        switch (command)
                        {
                            case Command.SkipAddressAttribute:
                                {
                                    OrderAddress.HouseNumber = null;
                                    await SuggestEditDeliveryAddressAsync(AddressAttribute.Building);
                                    return Trigger.EnterAddressBuilding;
                                }
                        }
                        break;
                    }
                case State.OrderNewAddressBuildingEditor:
                    {
                        switch (command)
                        {
                            case Command.SkipAddressAttribute:
                                {
                                    OrderAddress.Building = null;
                                    await SuggestEditDeliveryAddressAsync(AddressAttribute.Flat);
                                    return Trigger.EnterAddressFlat;
                                }
                        }
                        break;
                    }
                case State.OrderNewAddressFlatEditor:
                    {
                        switch (command)
                        {
                            case Command.SkipAddressAttribute:
                                {
                                    OrderAddress.Flat = null;
                                    await SuggestEditDeliveryAddressAsync(AddressAttribute.Comment);
                                    return Trigger.EnterAddressComment;
                                }
                        }
                        break;
                    }
                case State.OrderNewAddressCommentEditor:
                    {
                        switch (command)
                        {
                            case Command.SkipAddressAttribute:
                                {
                                    OrderAddress.Comment = null;
                                    await ShowNewAddressAsync();
                                    return Trigger.Ignore;
                                }
                            case Command.CancelAddNewAddress:
                                {
                                    OrderAddress = null;
                                    await SuggestChooseAddressAsync();
                                    return Trigger.CancelAddNewAddress;
                                }
                            case Command.AddNewAddress:
                                {
                                    return await AddNewAddressAsync();
                                }
                        }
                        break; 
                    }
                case State.OrderPhone:
                    {
                        await RequestPhoneAsync();
                        return Trigger.Ignore;
                    }
                case State.SmsPhone:
                    {
                        await RequestSmsAsync();
                        return Trigger.Ignore;
                    }
            }

            return null;
        }

        private async Task<Trigger?> CheckSmsAsync(string messageText)
        {
            if (messageText.Length != SMS_LENGTH)
            {
                string text = string.Format(OrdersText.SmsLengthError, SMS_LENGTH);
                await _stateManager.SendMessageAsync(text);
                return Trigger.Ignore;
            }

            if (messageText != SmsCode)
            {
                await _stateManager.SendMessageAsync(OrdersText.SmsWrong);
                return Trigger.Ignore;
            }

            Phone = PreaparedPhone;
            PreaparedPhone = null;

            await _stateManager.SendMessageAsync(OrdersText.PhoneConfirmed);
            await ShowDeliverySettingsAsync();
            return Trigger.EnteredSms;
        }

        private async Task<Trigger?> GetPhoneAsync(Message message)
        {
            if (message.Text == OrdersText.Back)
            {
                await _stateManager.ShowButtonMenuAsync(OrdersText.BackFromPhone);
                await ShowDeliverySettingsAsync();
                return Trigger.BackFromPhone;
            }

            string messageText = message.Contact.IsNull() ? message.Text : message.Contact.PhoneNumber;
            
            if (TryParseNumber(messageText, out string phone))
            {
                if (phone[0] != '7')
                {
                    await _stateManager.SendMessageAsync(OrdersText.OnlyRussianNumber);
                    return Trigger.Ignore;
                }

                PreaparedPhone = phone;

                await RequestSmsAsync();
                return Trigger.SendedSms;
            }
            else
            {
                await _stateManager.SendMessageAsync(OrdersText.WrongPhone);
            }
            
            return Trigger.Ignore;
        }

        /// <summary>
        /// отправка кнопки для повторного запроса смс
        /// </summary>
        /// <returns></returns>
        private async Task SendRetryButtonAsync()
        {
            await Task.Delay(SMS_RESEND_TIMER * 1000);

            // отправить кнопку для повторной отправки SMS
            if (_stateManager.CurrentState == State.SmsPhone)
            {
                InlineKeyboardButton[] buttons = GetResendSmsButton();
                InlineKeyboardMarkup markup = new InlineKeyboardMarkup(buttons);

                await _stateManager.SendMessageAsync(OrdersText.ResendSmsAvaliable, replyMarkup: markup);
            }
        }

        private InlineKeyboardButton[] GetResendSmsButton()
        {
            InlineKeyboardButton[] result = new InlineKeyboardButton[]
            {
                new InlineKeyboardButton(OrdersText.ResendSms)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.ResendSms,
                    })
                }
            };

            return result;
        }

        private async Task RequestSmsAsync()
        {
            RandomSeq random = new RandomSeq();
            SmsCode = random.GenNum(SMS_LENGTH);

            string text = string.Format(OrdersText.EnterSms, DBHelper.PhonePrettyPrint(PreaparedPhone), SMS_RESEND_TIMER.ToString());
            await _stateManager.ShowButtonMenuAsync(text);

            Task task = Task.Run(SendRetryButtonAsync);

#if DEBUG
            await _stateManager.SendMessageAsync($"СМС код {SmsCode}");
#endif
        }

        private bool TryParseNumber(string phoneNumber, out string phone)
        {
            phone = null;
            string text = phoneNumber;
            const string MatchPhonePattern = @"\+?(\d)-?\s*\(?(\d{3})\)?-?\s*(\d{3})-?\s*(\d{2})-?\s*(\d{2})";

            Regex rx = new Regex(MatchPhonePattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            MatchCollection matches = rx.Matches(text);

            if(matches.Count != 1)
            {
                return false;
            }

            phone = string.Join("", matches[0].Groups.Values.Skip(1));

            if (phone[0] == '8')
            {
                phone = "7" + phone[1..];
            }

            if(phone.Length == 11)
            {
                return true;
            }

            return false;
        }

        private async Task RequestPhoneAsync()
        {
            KeyboardButton[][] requestPhoneButton = new KeyboardButton[][]
            {
                new KeyboardButton[]
                {
                    new KeyboardButton(OrdersText.SharePhone)
                    {
                        RequestContact = true,
                    }
                },
                new KeyboardButton[]
                {
                    new KeyboardButton(OrdersText.Back)
                },
            };

            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(requestPhoneButton);

            await _stateManager.SendMessageAsync(OrdersText.EnterPhone, replyMarkup: markup);
        }

        private async Task<Trigger> AddNewAddressAsync()
        {
            if(OrderAddress.IsNull())
            {
                throw new Exception("Попытка добавить пустой адресс");
            }

            await _dataSource.Addresses.AddAsync(OrderAddress);
            await _dataSource.SaveChangesAsync();

            OrderAddressId = OrderAddress.Id;
            OrderAddress = null;

            if(Phone.IsNullOrEmpty())
            {
                return Trigger.AddressAddedNextPhone;
            }
            else
            {
                await ShowDeliverySettingsAsync();
                return Trigger.AddressAddedReturnToDeliverySettings;
            }
        }

        private async Task<Trigger> EditNewAddressAsync(string value, AddressAttribute addressAttribute)
        {
            Trigger result = Trigger.Ignore;
            bool isInvalidValue = false;
            string errorReason = string.Empty;
            AddressAttribute? nextAddressAttribute = null;

            if (OrderAddress.IsNull())
            {
                OrderAddress = new Address();
                OrderAddress.UserId = _stateManager.ChatId;
            }

            switch (addressAttribute)
            {
                case AddressAttribute.City:
                    {
                        int maxLength = 255;
                        if (value.Length > maxLength)
                        {
                            isInvalidValue = true;
                            errorReason = string.Format(OrdersText.ErrorLengthMax, maxLength);
                        }
                        else
                        {
                            OrderAddress.City = value;
                            nextAddressAttribute = AddressAttribute.Street;
                        }

                        break;
                    }
                case AddressAttribute.Street:
                    {
                        int maxLength = 255;
                        if (value.Length > maxLength)
                        {
                            isInvalidValue = true;
                            errorReason = string.Format(OrdersText.ErrorLengthMax, maxLength);
                        }
                        else
                        {
                            OrderAddress.Street = value;
                            nextAddressAttribute = AddressAttribute.HouseNumber;
                        }

                        break;
                    }
                case AddressAttribute.HouseNumber:
                    {
                        int maxLength = 255;
                        if (value.Length > maxLength)
                        {
                            isInvalidValue = true;
                            errorReason = string.Format(OrdersText.ErrorLengthMax, maxLength);
                        }
                        else
                        {
                            OrderAddress.HouseNumber = value;
                            nextAddressAttribute = AddressAttribute.Building;
                        }

                        break;
                    }
                case AddressAttribute.Building:
                    {
                        int maxLength = 255;
                        if (value.Length > maxLength)
                        {
                            isInvalidValue = true;
                            errorReason = string.Format(OrdersText.ErrorLengthMax, maxLength);
                        }
                        else
                        {
                            OrderAddress.Building = value;
                            nextAddressAttribute = AddressAttribute.Flat;
                        }

                        break;
                    }
                case AddressAttribute.Flat:
                    {
                        int maxLength = 255;
                        if (value.Length > maxLength)
                        {
                            isInvalidValue = true;
                            errorReason = string.Format(OrdersText.ErrorLengthMax, maxLength);
                        }
                        else
                        {
                            OrderAddress.Flat = value;
                            nextAddressAttribute = AddressAttribute.Comment;
                        }

                        break;
                    }
                case AddressAttribute.Comment:
                    {
                        int maxLength = 255;
                        if (value.Length > maxLength)
                        {
                            isInvalidValue = true;
                            errorReason = string.Format(OrdersText.ErrorLengthMax, maxLength);
                        }
                        else
                        {
                            OrderAddress.Comment = value;
                        }

                        break;
                    }
                default:
                    {
                        throw new NotSupportedException($"Неизвестный аттрибут при добавлении нового адреса: {addressAttribute.ToString()}");
                    }
            }

            if (isInvalidValue)
            {
                string errorText = string.Format(OrdersText.InvalidAddressAttribute, errorReason);
                await _stateManager.SendMessageAsync(errorText);
            }
            else if (nextAddressAttribute.IsNull())
            {
                await ShowNewAddressAsync();
            }
            else
            {
                result = await SuggestEditDeliveryAddressAsync(nextAddressAttribute.Value);
            }

            return result;
        }

        private async Task ShowNewAddressAsync()
        {
            string text = string.Format(OrdersText.CurrentAddress, OrderAddress.ToString());
            InlineKeyboardButton[] addAddress = GetAddAddressButtons();
            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(addAddress);

            await _stateManager.SendMessageAsync(text, replyMarkup: markup);
        }

        private InlineKeyboardButton[] GetAddAddressButtons()
        {
            InlineKeyboardButton[] result = new InlineKeyboardButton[]
            {
                new InlineKeyboardButton(OrdersText.Add)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.AddNewAddress,
                    })
                },
                new InlineKeyboardButton(OrdersText.Cancel)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.CancelAddNewAddress,
                    })
                },
            };

            return result;
        }

        private async Task<Trigger> SuggestEditDeliveryAddressAsync(AddressAttribute addressStage)
        {
            switch (addressStage)
            {
                case AddressAttribute.City:
                    {
                        await EnterAddressStageAsync(OrdersText.EnterCity, false);
                        return Trigger.EnterAddressCity;
                    }
                case AddressAttribute.Street:
                    {
                        await EnterAddressStageAsync(OrdersText.EnterStreet, false);
                        return Trigger.EnterAddressStreet;
                    }
                case AddressAttribute.HouseNumber:
                    {
                        await EnterAddressStageAsync(OrdersText.EnterHouseNumber, true);
                        return Trigger.EnterAddressHouseNumber;
                    }
                case AddressAttribute.Building:
                    {
                        await EnterAddressStageAsync(OrdersText.EnterBuilding, true);
                        return Trigger.EnterAddressBuilding;
                    }
                case AddressAttribute.Flat:
                    {
                        await EnterAddressStageAsync(OrdersText.EnterFlat, true);
                        return Trigger.EnterAddressFlat;
                    }
                case AddressAttribute.Comment:
                    {
                        await EnterAddressStageAsync(OrdersText.EnterComment, true);
                        return Trigger.EnterAddressComment;
                    }
                default:
                    {
                        throw new Exception($"Неизвестный этап ввода адреса: {addressStage.ToString()}");
                    }
            }
        }

        private async Task EnterAddressStageAsync(string text, bool withSkip)
        {
            InlineKeyboardMarkup markup = null;

            if (withSkip)
            {
                InlineKeyboardButton[] skip = GetSkipAddressStageButton();
                markup = new InlineKeyboardMarkup(skip);
            }

            await _stateManager.SendMessageAsync(text, replyMarkup: markup);
        }

        private InlineKeyboardButton[] GetSkipAddressStageButton()
        {
            InlineKeyboardButton[] result = new InlineKeyboardButton[]
            {
                new InlineKeyboardButton(OrdersText.Skip)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.SkipAddressAttribute,
                    })
                }
            };

            return result;
        }

        private async Task<Trigger> ChooseDeliveryAsync(DeliveryType deliveryType)
        {
            OrderDeliveryType = deliveryType;
            switch (deliveryType)
            {
                case DeliveryType.PickUp:
                    {
                        await CreateOrderAsync();
                        break;
                    }
                case DeliveryType.Delivery:
                    {
                        if (!OrderAddressId.HasValue)
                        {
                            OrderAddressId = (await _dataSource.Orders
                                .Where(order => order.UserId == _stateManager.ChatId && order.AddressId != null)
                                .OrderByDescending(order => order.DateFrom)
                                .Include(order => order.Address)
                                .Select(order => order.Address)
                                .FirstOrDefaultAsync())
                                ?.Id;

                            if(OrderAddressId.IsNull())
                            {
                                return await SuggestEditDeliveryAddressAsync(AddressAttribute.City);
                            }
                        }

                        if(Phone.IsNullOrEmpty())
                        {
                            return Trigger.EmptyPhone;
                        }

                        await ShowDeliverySettingsAsync();
                        break;
                    }
            }

            return Trigger.Ignore;
        }

        private async Task ShowDeliverySettingsAsync()
        {
            Address address = OrderAddressId.HasValue ? await _dataSource.Addresses.FindAsync(OrderAddressId) : null;
            string phoneText = Phone.IsNullOrEmpty() ? OrdersText.Empty : DBHelper.PhonePrettyPrint(Phone);
            string text = string.Format(OrdersText.OrderSettings,
                address?.ToString(),
                phoneText);

            InlineKeyboardButton[][] deliverySettings = GetDeliverySettingsButtons();
            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(deliverySettings);

            await _stateManager.SendMessageAsync(text, replyMarkup: markup);
        }

        private InlineKeyboardButton[][] GetDeliverySettingsButtons()
        {
            InlineKeyboardButton[][] result = new InlineKeyboardButton[][]
            {
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardButton(OrdersText.ChangeAddress)
                    {
                        CallbackData = JsonConvert.SerializeObject(new
                        {
                            Cmd = Command.ChangeAddress,
                        })
                    }
                },
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardButton(OrdersText.ChangePhone)
                    {
                        CallbackData = JsonConvert.SerializeObject(new
                        {
                            Cmd = Command.ChangePhone,
                        })
                    }
                },
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardButton(OrdersText.TakeOrder)
                    {
                        CallbackData = JsonConvert.SerializeObject(new
                        {
                            Cmd = Command.ConfirmOrder,
                        })
                    }
                },
            };

            return result;
        }

        private async Task SuggestChooseAddressAsync()
        {
            Address[] addresses = _dataSource.Addresses
                           .Where(address => address.UserId == _stateManager.ChatId)
                           .ToArray();

            string text = string.Format(OrdersText.ChooseAddress, string.Join('\n',
                addresses
                    .Select((address, index) => $"{index+1}. {address.ToString()}")));

            List<InlineKeyboardButton[]> addressesButtons = GetAddressesButtons(addresses);
            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(addressesButtons);

            await _stateManager.SendMessageAsync(text, replyMarkup: markup);
        }

        private List<InlineKeyboardButton[]> GetAddressesButtons(Address[] addresses)
        {
            List<InlineKeyboardButton[]> result = new List<InlineKeyboardButton[]>();
            
            if(addresses.IsNotNullOrEmpty())
            {
                result.AddRange(addresses.Select((address, index) => new InlineKeyboardButton[]
                {
                    new InlineKeyboardButton((index+1).ToString())
                    {
                        CallbackData = JsonConvert.SerializeObject(new
                        {
                            Cmd = Command.ChooseDeliveryAddress,
                            AddressId = address.Id,
                        })
                    }
                }));
            }

            List<InlineKeyboardButton[]> newAddress = GetNewAddressButton();
            result.AddRange(newAddress);
            List<InlineKeyboardButton[]> back = GetAddressBackButton();
            result.AddRange(back);

            return result;
        }

        private List<InlineKeyboardButton[]> GetAddressBackButton()
        {
            List<InlineKeyboardButton[]> result = new List<InlineKeyboardButton[]>
            {
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardButton(OrdersText.Back)
                    {
                        CallbackData = JsonConvert.SerializeObject(new
                        {
                            Cmd = Command.AddressBack,
                        })
                    }
                }
            };

            return result;
        }

        private List<InlineKeyboardButton[]> GetNewAddressButton()
        {
            List<InlineKeyboardButton[]> result = new List<InlineKeyboardButton[]>
            {
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardButton(OrdersText.NewAddress)
                    {
                        CallbackData = JsonConvert.SerializeObject(new
                        {
                            Cmd = Command.NewAddress,
                        })
                    }
                }
            };

            return result;
        }

        private async Task ChooseDeliveryTypeAsync()
        {
            InlineKeyboardButton[] deliveryTypes = GetDeliveryTypes();
            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(deliveryTypes);

            await _stateManager.SendMessageAsync(OrdersText.ChooseDeliveryType, replyMarkup: markup);
        }

        private InlineKeyboardButton[] GetDeliveryTypes()
        {
            InlineKeyboardButton[] result = new InlineKeyboardButton[]
            {
                new InlineKeyboardButton(OrdersText.DeliveryTypeDelivery)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.ChooseDeivetyType,
                        DeliveryType = DeliveryType.Delivery,
                    })
                },
                new InlineKeyboardButton(OrdersText.DeliveryTypePickUp)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.ChooseDeivetyType,
                        DeliveryType = DeliveryType.PickUp,
                    })
                },
            };

            return result;
        }

        private async Task CreateOrderAsync(long? arddressId = null)
        {
            IEnumerable<CartProduct> cart = _stateManager.GetHandler<CartConversation>().Cart;

            if(cart.IsNullOrEmpty())
            {
                throw new Exception($"userid={_stateManager.ChatId}. Пустая корзина при попытке создать заказ");
            }

            IEnumerable<OrderCart> orderCart = cart
                .Select(product => new OrderCart()
                {
                    ProductId = product.Id,
                    Count = product.Count,
                });

            Order order = await _dataSource.TakeOrderAsync(_stateManager.ChatId, orderCart, arddressId);

            SendAdmins(order.Id);

            string text = string.Format(OrdersText.NewOrder, order.Number);

            await _stateManager.SendMessageAsync(text);

        }

        private void SendAdmins(int orderId)
        {
            _rabbitEventHandler.Publish<IOrder>(orderId);
        }
    }
}
