using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Tables
{
    public class OutboxMessage
    {
        public Guid Id { get; set; }
        public string Queue { get; set; }
        public string Message { get; set; }
        public DateTime CreationDateTime { get; set; }
        public DateTime? ProcessingDateTime { get; set; }
        public string? Error { get; set; }
    }
}
