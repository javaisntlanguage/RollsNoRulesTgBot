using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Tables
{
    public class OutboxMessage
    {
        public Guid Id { get; set; }
        [Required]
        [MaxLength(255)]
        public string Queue { get; set; }
        [Required]
        public string Message { get; set; }
        [Required]
        public DateTime CreationDateTime { get; set; }
        public string? Error { get; set; }
    }
}
