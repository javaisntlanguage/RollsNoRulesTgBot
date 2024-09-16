using Database.Classes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Tables
{
    [Index(nameof(Login), nameof(PasswordHash), IsUnique = true)]
    public class AdminCredential
    {
        public const int LOGIN_MAX_LENGTH = 32;
        public const int LOGIN_MIN_LENGTH = 3;
        public const int PASSWORD_MIN_LENGTH = 6;
        public const int PASSWORD_MAX_LENGTH = 64;
        public const int NAME_MAX_LENGTH = 32;
        public const int NAME_MIN_LENGTH = 3;

		public int Id { get; set; }
        [MaxLength(LOGIN_MAX_LENGTH)]
        [Required]
        public string Login { get; set; }
		/// <summary>
		/// устанавливать только через метод SetPassword
		/// </summary>
		[Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; }
        [Required]
        [MaxLength(NAME_MAX_LENGTH)]
        public string Name { get; set; }
        public List<AdminsInGroup>? AdminsInGroups { get; set; }
        public List<AdminRight>? AdminRights { get; set; }
		public void SetPassword(string messageText)
		{
            PasswordHash = DBHelper.GetPasswordHash(messageText);
		}
	}
}
