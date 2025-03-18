using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper
{
    public class ExcDetails
    {
        public static string Get(Exception ex)
        {
            ExcDetails details = new ExcDetails();
            return _BuildDetails(ex, details);
        }

        private static string _BuildDetails(Exception ex, ExcDetails details)
        {
            var sb = new StringBuilder();
            sb.AppendLine("==Details".PadRight(20, '='));
            details._GetDetails(ex, sb);
            return sb.ToString();
        }

        private void _GetDetails(Exception ex, StringBuilder sb)
        {
            sb.AppendFormatLine("----Date: {0:s}", DateTime.UtcNow);
            sb.AppendFormatLine("----Type: {0}", ex.GetType().FullName!);
            sb.AppendFormatLine("----Source: {0}", ex.Source!);
            sb.AppendLine("----Message".PadRight(20, '-'));
            sb.AppendLine(ex.Message);
            sb.AppendLine("----StackTrace".PadRight(20, '-'));
            sb.AppendLine(ex.StackTrace);

            if (ex.InnerException.IsNotNull())
            {
                sb.AppendLine("==Inner".PadRight(20, '='));
                _GetDetails(ex.InnerException, sb);
            }

            sb.AppendLine();
        }
    }
}
