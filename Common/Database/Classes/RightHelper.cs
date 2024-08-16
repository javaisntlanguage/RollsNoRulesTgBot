using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Classes
{
	public static class RightHelper
	{
		public const string LANG_RU = "ru";
		public const string LANG_EN = "en";

		public static Guid SuperAdmin => new Guid("768f3ebd-52e1-455a-bdfb-4b454f05ddc5");
		public static Guid Orders => new Guid("e6be7f34-caee-4af2-a0f4-b75cbd70daf9");
	}
}
