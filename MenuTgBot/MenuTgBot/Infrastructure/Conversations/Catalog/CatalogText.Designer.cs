﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MenuTgBot.Infrastructure.Conversations.Catalog {
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
    internal class CatalogText {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal CatalogText() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("MenuTgBot.Infrastructure.Conversations.Catalog.CatalogText", typeof(CatalogText).Assembly);
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
        ///   Looks up a localized string similar to Выберите категорию.
        /// </summary>
        internal static string ChooseCategory {
            get {
                return ResourceManager.GetString("ChooseCategory", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Выберите продукт.
        /// </summary>
        internal static string ChooseProduct {
            get {
                return ResourceManager.GetString("ChooseProduct", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;b&gt;{0}&lt;/b&gt;
        ///
        ///&lt;i&gt;{1}&lt;/i&gt;
        ///
        ///{2}.
        /// </summary>
        internal static string ProductDetails {
            get {
                return ResourceManager.GetString("ProductDetails", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Товар не найден.
        /// </summary>
        internal static string ProductNotFound {
            get {
                return ResourceManager.GetString("ProductNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Нет доступных товаров.
        /// </summary>
        internal static string ProductsEmpty {
            get {
                return ResourceManager.GetString("ProductsEmpty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Вернуться в меню.
        /// </summary>
        internal static string ReturnToCatalog {
            get {
                return ResourceManager.GetString("ReturnToCatalog", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Вернуться назад.
        /// </summary>
        internal static string ReturnToCategory {
            get {
                return ResourceManager.GetString("ReturnToCategory", resourceCulture);
            }
        }
    }
}
