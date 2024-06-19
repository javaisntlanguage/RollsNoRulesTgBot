﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AdminTgBot.Infrastructure.Conversations.Orders {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class OrdersText {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal OrdersText() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("AdminTgBot.Infrastructure.Conversations.Orders.OrdersText", typeof(OrdersText).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Вернуться к выбору фильтра.
        /// </summary>
        internal static string BackToFilter {
            get {
                return ResourceManager.GetString("BackToFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Вернуться к заказам.
        /// </summary>
        internal static string BackToOrders {
            get {
                return ResourceManager.GetString("BackToOrders", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Заказ больше не подходит под фильтр.
        /// </summary>
        internal static string BadOrderIdForFilter {
            get {
                return ResourceManager.GetString("BadOrderIdForFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} {1} ₽ x {2} - {3} ₽.
        /// </summary>
        internal static string CartItem {
            get {
                return ResourceManager.GetString("CartItem", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Изменить фильтр.
        /// </summary>
        internal static string ChangeFilter {
            get {
                return ResourceManager.GetString("ChangeFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Настройка фильтра заказов.
        /// </summary>
        internal static string ChooseFilter {
            get {
                return ResourceManager.GetString("ChooseFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Введите дату или выберете из предложенных вариантов
        ///
        ///Текущее значение: {0}.
        /// </summary>
        internal static string ChooseFilterDate {
            get {
                return ResourceManager.GetString("ChooseFilterDate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Введите ID заказа
        ///
        ///Текущее значение: {0}.
        /// </summary>
        internal static string ChooseFilterId {
            get {
                return ResourceManager.GetString("ChooseFilterId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Выберите статус.
        /// </summary>
        internal static string ChooseOrderState {
            get {
                return ResourceManager.GetString("ChooseOrderState", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Дата с.
        /// </summary>
        internal static string DateFromFilterEmpty {
            get {
                return ResourceManager.GetString("DateFromFilterEmpty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Значение должно быть не позднее текущего дня.
        /// </summary>
        internal static string DateFromFilterFuture {
            get {
                return ResourceManager.GetString("DateFromFilterFuture", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Дата с: {0}.
        /// </summary>
        internal static string DateFromFilterHasValue {
            get {
                return ResourceManager.GetString("DateFromFilterHasValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Ошибка распознавания даты. Попробуйте ввести в формате &quot;ГГГГ.ММ.ДД&quot;.
        /// </summary>
        internal static string DateParseError {
            get {
                return ResourceManager.GetString("DateParseError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Дата по.
        /// </summary>
        internal static string DateToFilterEmpty {
            get {
                return ResourceManager.GetString("DateToFilterEmpty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Дата по: {0}.
        /// </summary>
        internal static string DateToFilterHasValue {
            get {
                return ResourceManager.GetString("DateToFilterHasValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Введите целое число.
        /// </summary>
        internal static string EnterInt {
            get {
                return ResourceManager.GetString("EnterInt", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Введите текст.
        /// </summary>
        internal static string EnterText {
            get {
                return ResourceManager.GetString("EnterText", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Дата должна быть не позднее &quot;Даты по&quot;.
        /// </summary>
        internal static string FilterDateFromAfterDateTo {
            get {
                return ResourceManager.GetString("FilterDateFromAfterDateTo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Дата должна быть не раньше &quot;Даты с&quot;.
        /// </summary>
        internal static string FilterDateToBeforeDateFrom {
            get {
                return ResourceManager.GetString("FilterDateToBeforeDateFrom", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Нет фильтра.
        /// </summary>
        internal static string FilterEmpty {
            get {
                return ResourceManager.GetString("FilterEmpty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Сбросить.
        /// </summary>
        internal static string FilterReset {
            get {
                return ResourceManager.GetString("FilterReset", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Фильтр 
        ///
        ///{0}.
        /// </summary>
        internal static string FilterTitle {
            get {
                return ResourceManager.GetString("FilterTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ID.
        /// </summary>
        internal static string IdFilterEmpty {
            get {
                return ResourceManager.GetString("IdFilterEmpty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ID: {0}.
        /// </summary>
        internal static string IdFilterHasValue {
            get {
                return ResourceManager.GetString("IdFilterHasValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Месяц назад.
        /// </summary>
        internal static string MonthAgo {
            get {
                return ResourceManager.GetString("MonthAgo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Нет значения.
        /// </summary>
        internal static string NoValue {
            get {
                return ResourceManager.GetString("NoValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Заказ № {0} ({1}) взят в работу.
        /// </summary>
        internal static string OrderApproved {
            get {
                return ResourceManager.GetString("OrderApproved", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Заказ № {0} ({1}) отменен.
        /// </summary>
        internal static string OrderDeclined {
            get {
                return ResourceManager.GetString("OrderDeclined", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Заказ № {0} ({1}) от {2}
        ///
        ///Статус: {3}
        ///
        ///{4}
        ///
        ///Итого: {5}
        ///.
        /// </summary>
        internal static string OrderDetails {
            get {
                return ResourceManager.GetString("OrderDetails", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Заказ с  id = {0} не найден.
        /// </summary>
        internal static string OrderNotFound {
            get {
                return ResourceManager.GetString("OrderNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} ({1}) - {2} ₽.
        /// </summary>
        internal static string OrderPreview {
            get {
                return ResourceManager.GetString("OrderPreview", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Дата.
        /// </summary>
        internal static string OrdersFilterDate {
            get {
                return ResourceManager.GetString("OrdersFilterDate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to с {0}.
        /// </summary>
        internal static string OrdersFilterDateFrom {
            get {
                return ResourceManager.GetString("OrdersFilterDateFrom", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to по {0}.
        /// </summary>
        internal static string OrdersFilterDateTo {
            get {
                return ResourceManager.GetString("OrdersFilterDateTo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Пусто.
        /// </summary>
        internal static string OrdersFilterEmpty {
            get {
                return ResourceManager.GetString("OrdersFilterEmpty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Id - {0}.
        /// </summary>
        internal static string OrdersFilterId {
            get {
                return ResourceManager.GetString("OrdersFilterId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Состояния заказа.
        /// </summary>
        internal static string OrdersStates {
            get {
                return ResourceManager.GetString("OrdersStates", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Взять в работу.
        /// </summary>
        internal static string OrderStateApproved {
            get {
                return ResourceManager.GetString("OrderStateApproved", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Выполнен.
        /// </summary>
        internal static string OrderStateCompleted {
            get {
                return ResourceManager.GetString("OrderStateCompleted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Отменен.
        /// </summary>
        internal static string OrderStateDeclined {
            get {
                return ResourceManager.GetString("OrderStateDeclined", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Ошибка обработки заказа.
        /// </summary>
        internal static string OrderStateError {
            get {
                return ResourceManager.GetString("OrderStateError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Новый.
        /// </summary>
        internal static string OrderStateNew {
            get {
                return ResourceManager.GetString("OrderStateNew", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Неизвестно.
        /// </summary>
        internal static string OrderStateNone {
            get {
                return ResourceManager.GetString("OrderStateNone", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .
        /// </summary>
        internal static string OrderStateOff {
            get {
                return ResourceManager.GetString("OrderStateOff", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ✅.
        /// </summary>
        internal static string OrderStateOn {
            get {
                return ResourceManager.GetString("OrderStateOn", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Статус.
        /// </summary>
        internal static string StateFilterEmpty {
            get {
                return ResourceManager.GetString("StateFilterEmpty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Статус: {0}.
        /// </summary>
        internal static string StateFilterHasValue {
            get {
                return ResourceManager.GetString("StateFilterHasValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Сегодня.
        /// </summary>
        internal static string Today {
            get {
                return ResourceManager.GetString("Today", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Неделю назад.
        /// </summary>
        internal static string WeekAgo {
            get {
                return ResourceManager.GetString("WeekAgo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Вчера.
        /// </summary>
        internal static string Yesterday {
            get {
                return ResourceManager.GetString("Yesterday", resourceCulture);
            }
        }
    }
}
