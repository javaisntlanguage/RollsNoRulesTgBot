﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MenuTgBot.Infrastructure {
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
    internal class MessagesText {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal MessagesText() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("MenuTgBot.Infrastructure.MessagesText", typeof(MessagesText).Assembly);
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
        ///   Looks up a localized string similar to 🛒Корзина.
        /// </summary>
        internal static string CommandCart {
            get {
                return ResourceManager.GetString("CommandCart", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 📦Заказы.
        /// </summary>
        internal static string CommandOrder {
            get {
                return ResourceManager.GetString("CommandOrder", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 🌯Каталог.
        /// </summary>
        internal static string CommandShopCatalog {
            get {
                return ResourceManager.GetString("CommandShopCatalog", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Старт.
        /// </summary>
        internal static string CommandStart {
            get {
                return ResourceManager.GetString("CommandStart", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Вернитесь к последнему сообщению.
        /// </summary>
        internal static string GoLastMessage {
            get {
                return ResourceManager.GetString("GoLastMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Что-то пошло не так.
        /// </summary>
        internal static string SomethingWrong {
            get {
                return ResourceManager.GetString("SomethingWrong", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Неизвестная команда.
        /// </summary>
        internal static string UnknownCommand {
            get {
                return ResourceManager.GetString("UnknownCommand", resourceCulture);
            }
        }
    }
}
