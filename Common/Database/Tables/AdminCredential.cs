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
        public const int NAME_MAX_LENGTH = 32;

		public int Id { get; set; }
        [MaxLength(LOGIN_MAX_LENGTH)]
        [Required]
        public string Login { get; set; }
        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; }
        [Required]
        [MaxLength(NAME_MAX_LENGTH)]
        public string Name { get; set; }
    }
}
