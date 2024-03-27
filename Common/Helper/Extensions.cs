using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using NLog;
using Helper.Attributes;
using Helper.Interfaces;
using Newtonsoft.Json.Linq;

namespace Helper
{
    public static class Extensions
    {
        #region Private variables



        private static readonly string[] ruschars = { "А", "Б", "В", "Г", "Д", "Е", "Ё", "Ж", "З", "И", "Й", "К", "Л", "М", "Н", "О", "П", "Р", "С", "Т", "У", "Ф", "Х", "Ц", "Ч", "Ш", "Щ", "Ъ", "Ы", "Ь", "Э", "Ю", "Я" };
        private static readonly string[] engchars = { "A", "B", "V", "G", "D", "E", "E", "ZH", "Z", "I", "IY", "K", "L", "M", "N", "O", "P", "R", "S", "T", "U", "F", "H", "C", "CH", "SH", "SH", "", "YI", "", "E", "YU", "YA" };
        private static readonly string[] vengchars = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", ".", ",", ":", ";", "(", ")", "[", "]" };

        private static readonly Regex sWhitespace = new Regex(@"\s+");
        private static readonly Regex LatinAlphaNumericRegExp = new Regex("[^a-zA-Z0-9]");
        private static readonly IdnMapping m_idn = new IdnMapping();


        #endregion
        #region Public Methods

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool IsNull(this object o)
        {
            return object.ReferenceEquals(o, null);
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool IsNotNull(this object o)
        {
            return !o.IsNull();
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool IsNullOrEmpty(this string s)
        {
            return string.IsNullOrWhiteSpace(s);
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool IsNullOrEmpty(this Array a)
        {
            return a.IsNull() || a.Length == 0;
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool IsNullOrEmpty(this IEnumerable e)
        {
            return e.IsNull() || !e.Cast<object>().Any();
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool IsNotNullOrEmpty(this string s)
        {
            return !s.IsNullOrEmpty();
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool IsNotNullOrEmpty(this Array a)
        {
            return !a.IsNullOrEmpty();
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool IsNotNullOrEmpty(this IEnumerable e)
        {
            return e.IsNotNull() && e.Cast<object>().Any();
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool IsEmpty(this Array a)
        {
            return a.Length == 0;
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool IsEmpty(this IEnumerable e)
        {
            return !e.Cast<object>().Any();
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool IsNotEmpty(this Array a)
        {
            return a.Length > 0;
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool IsNotEmpty(this IEnumerable e)
        {
            return e.Cast<object>().Any();
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool ContainsKey(this NameValueCollection collection, string key)
        {
            return collection.Get(key) != null || collection.AllKeys.Contains(key);
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static string NullIfEmpty(this string s)
        {
            return s.IsNotNullOrEmpty() ? s : null;
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static string EmptyIfNull(this string s)
        {
            return s ?? string.Empty;
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static string Capitalize(this string s)
        {
            if (s.IsNullOrEmpty())
                return s;

            s = s.ToLowerInvariant();
            return char.ToUpperInvariant(s[0]) + s.Remove(0, 1);
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool In<T>(this T value, params T[] values)
        {
            return values.Contains(value);
        }[System.Diagnostics.DebuggerNonUserCode()]
        public static bool In<T>(this T value, IEnumerable<T> values)
        {
            return values.Contains(value);
        }
        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool In<T>(this IEnumerable<T> value, IEnumerable<T> values)
        {
            return value.Any(v => v.In(values));
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool In(this string sValue, params string[] values)
        {
            return sValue.In(false, values);
        }

        public static string GetNotEmpty(this string[] sVals)
        {
            return sVals.FirstOrDefault(v => !v.IsNullOrEmpty());
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool In(this string sValue, bool bIgnoreCase, params string[] values)
        {
            return values.IsNotNullOrEmpty() && values.Contains(sValue, bIgnoreCase ? StringComparer.InvariantCultureIgnoreCase : StringComparer.InvariantCulture);
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool NotIn(this string sValue, params string[] values)
        {
            return !sValue.In(false, values);
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool NotIn(this string sValue, bool bIgnoreCase, params string[] values)
        {
            return !sValue.In(bIgnoreCase, values);
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static IEnumerable<T> Except<T>(this IEnumerable<T> sequence, params T[] values)
        {
            return Enumerable.Except(sequence, values);
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static string TrimNullable(this string s)
        {
            return string.IsNullOrEmpty(s) ? s : s.Trim();
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static string EmptyAsNull(this string s)
        {
            return s.IsNullOrEmpty() ? null : s.Trim();
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool In(this int nValue, params int[] values)
        {
            return values.IsNotNullOrEmpty() && values.Contains(nValue);
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static IEnumerable<decimal> Split(this decimal nSum, decimal nMax)
        {
            if (nSum <= nMax)
                return new decimal[] { nSum };

            decimal[] list = new decimal[(int)decimal.Ceiling(nSum / nMax)];
            for (int i = 0; i < list.Length; i++)
                list[i] = nMax;

            decimal nMod = nSum % nMax;
            list[list.Length - 1] = nSum - nMax * (list.Length - 1);

            return list;
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool NotEquals(this object oValue, object oValueToCompare)
        {
            return !oValue.Equals(oValueToCompare);
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool NotEquals(this string sValue, string sValueToCompare)
        {
            return !sValue.Equals(sValueToCompare);
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool NotEquals(this string sValue, string sValueToCompare, StringComparison comparisionType)
        {
            return !sValue.Equals(sValueToCompare, comparisionType);
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool EqualsCI(this string sValue, string sValueToCompare)
        {
            return string.Equals(sValue, sValueToCompare, StringComparison.InvariantCultureIgnoreCase);
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool NotEqualsCI(this string sValue, string sValueToCompare)
        {
            return !string.Equals(sValue, sValueToCompare, StringComparison.InvariantCultureIgnoreCase);
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static string Wrap(this string s, int n)
        {
            if (s.IsNullOrEmpty() || s.Length <= n)
                return s;

            var sb = new StringBuilder();
            int cnt = 0;
            int len = s.Length;

            while (true)
            {
                int nTmp = len - cnt;
                if (nTmp <= n)
                {
                    sb.Append(s.Substring(cnt));
                    break;
                }

                sb.AppendLine(s.Substring(cnt, n));
                cnt += n;
            }

            return sb.ToString();
        }

        [System.Diagnostics.DebuggerNonUserCode]
        [CLSCompliant(false)]
        public static TData Extract<TData>(this string sSource, string sRegex, CultureInfo fmt = null) where TData : IConvertible
        {
            Match m = Regex.Match(sSource, sRegex);
            return m.Success
                ? (TData)Convert.ChangeType(m.Groups[1].Value, typeof(TData), fmt ?? CultureInfo.InvariantCulture)
                : default(TData);
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static string FindByStartsWith(this IEnumerable<string> lines, string sValue)
        {
            return lines.First(l => l.StartsWith(sValue, StringComparison.InvariantCultureIgnoreCase));
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static string FindByStartsWith(this IEnumerable<string> lines, string sValue, bool bSuppressError)
        {
            return bSuppressError
                ? lines.FirstOrDefault(l => l.StartsWith(sValue, StringComparison.InvariantCultureIgnoreCase))
                : FindByStartsWith(lines, sValue);
        }

        public static void Map<T>(this IEnumerable<T> collection, Action<T> action)
        {
            if (collection == null || action == null)
                return;

            foreach (var item in collection)
                action(item);
        }

        public static string ToTitle(this string src)
        {
            if (src.IsNull() || src.Length < 2) return src;

            return char.ToUpper(src[0]) + src.Substring(1);

        }

        public static string ToTranslit(this string src)
        {
            var dst = string.Empty;

            for (int idx = 0; idx < src.Length; idx++)
            {
                int ruschar = -1;
                bool toLower = false;
                bool addsrc = true;

                for (int ridx = 0; ridx < ruschars.Length; ridx++)
                {
                    if (ruschars[ridx].Equals(src[idx].ToString()))
                    {
                        ruschar = ridx;
                        toLower = false;
                    }
                    else
                    {
                        if (ruschars[ridx].Equals(char.ToUpper(src[idx]).ToString()))
                        {
                            ruschar = ridx;
                            toLower = true;
                        }
                    }

                    if (ruschar > -1)
                    {
                        addsrc = false;
                        if (toLower)
                        {
                            dst += engchars[ruschar].ToLower();
                            break;
                        }

                        dst += engchars[ruschar];
                        break;
                    }
                }

                if (addsrc)
                    dst += src[idx];
            }

            return dst;
        }

        public static string ConvertToString(this NameValueCollection coll, string sSeparator = ";")
        {
            return coll.IsNull() ? null : string.Join(sSeparator, coll.AllKeys.Select(key => $"{key}={coll[key]}"));
        }

        public static double ToUnixDateTime(this DateTime dt)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = dt - origin;
            return Math.Floor(diff.TotalSeconds);
        }

        public static DateTime FromUnixDateTime(this double dt)
        {
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(dt);
        }

        /// <summary>
        /// Добавляет к текущей дате рабочие дни (нерабочими днями считаются только суббота и воскресенье)
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="count">Количество рабочих дней</param>
        /// <returns></returns>
        public static DateTime AddWorkDays(this DateTime dt, int count)
        {
            var result = dt;
            var index = 1;

            while (index++ < count)
            {
                while (((int)result.AddDays(1).DayOfWeek).In(0, 6))
                    result = result.AddDays(1);

                result = result.AddDays(1);
            }

            return result;
        }

        [System.Diagnostics.DebuggerNonUserCode]

        public static bool IsINN(this string inn)
        {
            if (!Regex.IsMatch(inn, @"^\d{10,12}$", RegexOptions.None) ||
                !Regex.IsMatch(inn, @"(^\d{10}$)|(^F\d{10}$)|(^\d{12}$)", RegexOptions.None))
                return false;

            var value = inn;
            var sum = 0;

            if (value.StartsWith("F"))
                value = value.Substring(1);

            switch (value.Length)
            {
                case 10:
                    {
                        int[] arrMul10 = { 2, 4, 10, 3, 5, 9, 4, 6, 8 };
                        sum =
                            arrMul10.Select(
                                (mul, index) => int.Parse(value[index].ToString(CultureInfo.InvariantCulture)) * mul)
                                .Sum();
                        sum = (sum % 11) % 10;

                        return sum == int.Parse(value[9].ToString(CultureInfo.InvariantCulture));
                    }
                case 12:
                    {
                        int[] arrMul121 = { 7, 2, 4, 10, 3, 5, 9, 4, 6, 8 };
                        sum =
                            arrMul121.Select(
                                (mul, index) => int.Parse(value[index].ToString(CultureInfo.InvariantCulture)) * mul)
                                .Sum();
                        sum = (sum % 11) % 10;

                        if (sum != int.Parse(value[10].ToString(CultureInfo.InvariantCulture)))
                            return false;

                        int[] arrMul122 = { 3, 7, 2, 4, 10, 3, 5, 9, 4, 6, 8 };
                        sum =
                            arrMul122.Select(
                                (mul, index) => int.Parse(value[index].ToString(CultureInfo.InvariantCulture)) * mul)
                                .Sum();
                        sum = (sum % 11) % 10;

                        return sum == int.Parse(value[11].ToString(CultureInfo.InvariantCulture));
                    }
                default:
                    return false;
            }
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool IsPersonalBankAccount(this string account)
        {
            //40817,40820,423,426
            return account.StartsWith("40817") ||
                   account.StartsWith("40820") ||
                   account.StartsWith("423") ||
                   account.StartsWith("426");
        }

        [System.Diagnostics.DebuggerNonUserCode()]
        public static bool IsCorrectAccountForBIK(this string account, string bik)
        {
            /*

                 DECLARE @Contr int = 0
    DECLARE @Koef varchar(23) = '71371371371371371371371' --Весовые коэффициенты 
    DECLARE @Sym6 char(1) --6-ой символ счета (он может быть буквенным) 
        
    SET @Sym6 = SUBSTRING(@Account, 6, 1)
    --Проверим длину счета и соответствие формату (6-ой символ может быть определенным буквенным знаком) 
    IF LEN(@Account) <> 20
        RETURN -1

    IF TRY_CAST(SUBSTRING(@Account, 1, 5) AS int) IS NULL
        RETURN -2
    IF CHARINDEX(@Sym6, '0123456789ABCEHKMPTX') = 0
        RETURN -2
    IF TRY_CAST(SUBSTRING(@Account, 7, 15) AS bigint) IS NULL
        RETURN -2

    --20-ти значный банковский счет 
    IF TRY_CAST(@Sym6 AS tinyint) IS NULL
    BEGIN 
        --6-ой символ счета буквенный 
        SET @Account = SUBSTRING(@Account, 1, 5) + CAST(CHARINDEX(@Sym6, 'ABCEHKMPTX') - 1 AS char(1)) + SUBSTRING(@Account, 7, 15)
    END

    DECLARE @Acc2 varchar(23)
    IF TRY_CAST(SUBSTRING(@Bik, 7, 2) AS int) <> 0
        -- счёт не в РКЦ
        SET @Acc2 = SUBSTRING(@Bik, 7, 3) + @Account
    ELSE
        SET @Acc2 = '0' + SUBSTRING(@Bik, 5, 2) + @Account
    

    DECLARE @Crc int;
    WITH StrCTE(Start) AS
    (
        SELECT  1
        UNION ALL
        SELECT  Start + 1
        FROM StrCTE
        WHERE Start < LEN(@Acc2)
    )
    SELECT @Crc = SUM((CAST(SUBSTRING(@Acc2 , Start, 1) AS int) * CAST(SUBSTRING(@Koef , Start, 1) AS int)))
    FROM StrCTE
    SET @Crc = @Crc % 10

    RETURN @Crc

             
             */

            try
            {
                string rkc = string.Join("", new string[] { "0", bik.Substring(4, 2) });
                string ko = int.Parse(bik.Substring(6, 2)) != 0 ? bik.Substring(6, 3) : "0" + bik.Substring(4, 2);
                string mask_ko = "713";
                string mask_account = "71371371071371371371";

                int nKey = 0;
                for (int idx = 0; idx < ko.Length; idx++)
                {
                    nKey += int.Parse(mask_ko.Substring(idx, 1)) * int.Parse(ko.Substring(idx, 1));
                }
                for (int idx = 0; idx < account.Length; idx++)
                {
                    nKey += int.Parse(mask_account.Substring(idx, 1)) * int.Parse(account.Substring(idx, 1));
                }

                string sKey = (nKey * 3).ToString();

                return sKey.Substring(sKey.Length - 1).Equals(account.Substring(8, 1));
            }
            catch
            {
                return false;
            }
        }

        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            if (enumeration == null || action == null)
                return;

            foreach (T item in enumeration)
            {
                action(item);
            }
        }

        public static IEnumerable<TResult> LeftOuterJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKey, Func<TRight, TKey> rightKey,
        Func<TLeft, TRight, TResult> result)
        {
            return left
                .GroupJoin(right, leftKey, rightKey, (l, r) => new { l, r })
                .SelectMany(
                    o => o.r.DefaultIfEmpty(),
                    (l, r) => new { lft = l.l, rght = r })
                .Select(o => result.Invoke(o.lft, o.rght));
        }

        public static void RemoveIfExists<TKey, TValue>(this IDictionary<TKey, TValue> enumeration, TKey key)
        {
            if (enumeration == null || key == null)
                return;

            if(enumeration.ContainsKey(key))
            {
                enumeration.Remove(key);
            }
        }

        public static NameValueCollection ToNameValueCollection(this IUriQuery dynamicObject)
        {
            NameValueCollection nameValueCollection = new NameValueCollection();

            TypeDescriptor.GetProperties(dynamicObject)
                .Cast<PropertyDescriptor>()
                .Where(property => property.Attributes
                    .OfType<UriQueryParameterAttribute>()
                    .Any())
                .ForEach(property =>
                {
                    string value = property.GetValue(dynamicObject)?.ToString();
                    nameValueCollection.Add(property.Name, value);
                });

            return nameValueCollection;
        }

        #endregion
        #region String extensions



        public static string Left(this string sValue, int nLength)
        {
            return string.IsNullOrEmpty(sValue) || sValue.Length <= nLength
                ? sValue
                : sValue.Substring(0, nLength);
        }

        public static string Right(this string sValue, int nLength)
        {
            return string.IsNullOrEmpty(sValue) || sValue.Length <= nLength
                ? sValue
                : sValue.Substring(sValue.Length - nLength, nLength);
        }

        public static byte[] HexString2ByteArray(this string sValue)
        {
            return Enumerable.Range(0, sValue.Length)
                    .Where(x => x % 2 == 0)
                    .Select(x => Convert.ToByte(sValue.Substring(x, 2), 16))
                    .ToArray();
        }

        public static TType ToEnum<TType>(this string sValue, bool bIgnoreCase = true)
        {
            return (TType)Enum.Parse(typeof(TType), sValue, bIgnoreCase);
        }

        /// <summary>
        /// Кодирует текст в base64
        /// </summary>
        /// <param name="data"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string ToBase64(this string data, Encoding encoding = null)
        {
            if (data.IsNull())
            {
                return null;
            }

            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }

            var encDataByte = encoding.GetBytes(data);
            var ret = Convert.ToBase64String(encDataByte);
            return ret;
        }

        /// <summary>
        /// Декодирует текст в строку из base64
        /// </summary>
        /// <param name="base64EncodedData"></param>
        /// <returns></returns>
        public static string Base64Decode(this string base64EncodedData)
        {
            if (base64EncodedData.IsNull())
            {
                return null;
            }

            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static object ToEnum(this string sValue, Type type, bool bIgnoreCase = true)
        {
            return Enum.Parse(type, sValue, bIgnoreCase);
        }

        public static string RemoveWhitespaces(this string input)
        {
            return sWhitespace.Replace(input, String.Empty);
        }

        public static string RemoveAllNonLatinAlphanumeric(this string s)
        {
            return LatinAlphaNumericRegExp.Replace(s, string.Empty);
        }



        #endregion
        #region Http Extensions



        public static Uri AddQuery(this Uri uri, string name, string value)
        {
            UriBuilder ub = new UriBuilder(uri);
            NameValueCollection httpValueCollection = HttpUtility.ParseQueryString(uri.Query);
            httpValueCollection.Add(name, value);
            ub.Query = httpValueCollection.ToString();

            return ub.Uri;
        }

        /// <summary>
        /// добавление параметров в запрос из объекта
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="parameters">объект, который преобразуется в параметры</param>
        /// <returns></returns>
        public static Uri AddQuery(this Uri uri, IUriQuery parameters)
        {
            UriBuilder ub = new UriBuilder(uri);
            NameValueCollection httpValueCollection = parameters.ToNameValueCollection();
            ub.Query = httpValueCollection.GetQueryString();

            return ub.Uri;
        }

        public static Uri IdnDecode(this Uri uri)
        {
            if (uri.IsNull())
                return null;

            var ub = new UriBuilder(uri);

            try
            {
                ub.Host = m_idn.GetUnicode(ub.Host);
            }
            catch
            {
                //Ignore
            }

            return ub.Uri;
        }

        public static Uri IdnEncode(this Uri uri)
        {
            if (uri.IsNull())
                return null;

            var ub = new UriBuilder(uri);
            ub.Host = m_idn.GetAscii(ub.Host);
            return ub.Uri;
        }

        #endregion
        #region Decimal extensions



        public static decimal Normalize(this decimal d)
        {
            return d / 1.000000000000000000000000000000000M;
        }



        #endregion
        #region Enum extensions



        public static T ConvertEnum<T>(this Enum src)
        {
            return (T)Enum.Parse(typeof(T), src.ToString());
        }

        /// <summary>
        /// Получить значение атрибута Description у элемента перечисления
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetDescription(this Enum value)
        {
            var enumType = value.GetType();
            var field = enumType.GetField(value.ToString());
            var attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return !attributes.Any() ? value.ToString() : ((DescriptionAttribute)attributes[0]).Description;
        }

        public static IEnumerable<T> GetFlags<T>(this T src) where T : System.Enum
        {
            foreach (Enum value in Enum.GetValues(src.GetType()))
                if (((Enum)src).HasFlag(value))
                    yield return (T)value;
        }
        #endregion
        #region Stream extensions
        public static string ConvertToBase64(this Stream stream)
        {
            if (stream is MemoryStream memoryStream)
            {
                return Convert.ToBase64String(memoryStream.ToArray());
            }

            var bytes = new byte[(int)stream.Length];

            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(bytes, 0, (int)stream.Length);

            return Convert.ToBase64String(bytes);
        }
        #endregion
        #region IEnumerable extensions
        /// <summary>
        /// получение элементов коллекции и их индеков
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
        {
            return source
                .Select((item, index) => (item, index));
        }
        #endregion IEnumerable extensions
        #region NameValueCollection extensions
        /// <summary>
        /// получение строки запроса
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static string GetQueryString(this NameValueCollection parameters)
        {
            List<string> items = new List<string>();

            foreach (string name in parameters)
                items.Add(string.Concat(name, "=", System.Web.HttpUtility.UrlEncode(parameters[name], Encoding.UTF8)));

            return string.Join("&", items.ToArray());
        }
        #endregion NameValueCollection extensions
        #region JObject extensions
        public static T GetEnumValue<T>(this JObject obj, string key) where T : Enum, new()
        {
            return (T)Enum.Parse(typeof(T), obj[key].Value<string>());
        }
        #endregion JObject extensions
    }
}

