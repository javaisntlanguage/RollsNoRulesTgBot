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
        public int Id { get; set; }
        [MaxLength(32)]
        [Required]
        public string Login { get; set; }
        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; }
        [Required]
        [MaxLength(32)]
        public string Name { get; set; }
    }
}
