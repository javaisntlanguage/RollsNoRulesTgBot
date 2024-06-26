﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MenuTgBot.Infrastructure.Conversations.Cart {
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
    internal class CartText {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal CartText() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("MenuTgBot.Infrastructure.Conversations.Cart.CartText", typeof(CartText).Assembly);
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
        ///   Looks up a localized string similar to Добавить в корзину.
        /// </summary>
        internal static string AddToCart {
            get {
                return ResourceManager.GetString("AddToCart", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to + {0}.
        /// </summary>
        internal static string AddToCartPrice {
            get {
                return ResourceManager.GetString("AddToCartPrice", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Ваша корзина пуста.
        /// </summary>
        internal static string CartEmpty {
            get {
                return ResourceManager.GetString("CartEmpty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;b&gt;{0}&lt;/b&gt;
        ///
        ///&lt;i&gt;{1}&lt;/i&gt;
        ///
        ///{2} * {3} = {4}
        ///
        ///Общая сумма корзины: {5}.
        /// </summary>
        internal static string CartProductDetails {
            get {
                return ResourceManager.GetString("CartProductDetails", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;b&gt;{0}&lt;/b&gt;
        ///
        ///&lt;i&gt;{1}&lt;/i&gt;
        ///
        ///{2}
        ///
        ///Общая сумма корзины: {3}.
        /// </summary>
        internal static string CartZeroCountProductDetails {
            get {
                return ResourceManager.GetString("CartZeroCountProductDetails", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to -.
        /// </summary>
        internal static string DecreaseCount {
            get {
                return ResourceManager.GetString("DecreaseCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Убрать из корзины.
        /// </summary>
        internal static string DeleteFromCart {
            get {
                return ResourceManager.GetString("DeleteFromCart", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to +.
        /// </summary>
        internal static string IncreaseCount {
            get {
                return ResourceManager.GetString("IncreaseCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to X.
        /// </summary>
        internal static string NoPagination {
            get {
                return ResourceManager.GetString("NoPagination", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to =&gt;.
        /// </summary>
        internal static string PaginationNext {
            get {
                return ResourceManager.GetString("PaginationNext", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;=.
        /// </summary>
        internal static string PaginationPrevious {
            get {
                return ResourceManager.GetString("PaginationPrevious", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Товар недоступен.
        /// </summary>
        internal static string ProductNotAvaliable {
            get {
                return ResourceManager.GetString("ProductNotAvaliable", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Начать оформление заказа.
        /// </summary>
        internal static string TakeOrder {
            get {
                return ResourceManager.GetString("TakeOrder", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Ваша корзина:.
        /// </summary>
        internal static string Welcome {
            get {
                return ResourceManager.GetString("Welcome", resourceCulture);
            }
        }
    }
}
