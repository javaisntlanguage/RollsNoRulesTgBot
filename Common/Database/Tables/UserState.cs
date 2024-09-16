using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Tables
{
    public class UserState
    {
        [Key]
        [ForeignKey("User")]
        public long UserId { get; set; }
        public int StateId { get; set; }
        public string? Data { get; set; }
        public User? User { get; set; }
        public int? LastMessageId { get; set; }
    }
}
