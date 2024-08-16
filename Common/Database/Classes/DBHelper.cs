using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using NLog.Time;
using Database.Resources;
using Database.Tables;

namespace Database.Classes
{
    public class DBHelper
    {
        private DBHelper()
        {

        }

        private static readonly MD5 _MD5 = MD5.Create();

        public static string GetPasswordHash(string password)
        {
            byte[] bPassword = Encoding.ASCII.GetBytes(password);
            string result = Convert.ToHexString(_MD5.ComputeHash(bPassword));

            return result;
        }

        public static string PhonePrettyPrint(string phone)
        {
            if (phone.Length != 11 || phone[0] != '7')
            {
                throw new ArgumentException($"Неверный формат телефона: {phone}");
            }

            string result = $"+7 ({phone[1..4]})-{phone[4..7]}-{phone[7..9]}-{phone[9..]}";

            return result;
        }
    }
}
