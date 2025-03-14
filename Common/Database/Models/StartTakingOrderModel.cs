using Database.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class StartTakingOrderModel
    {
        public long UserId { get; set; }
        public IEnumerable<OrderCart> Cart { get; set; }
        public decimal Sum { get; set; }
        public string Phone { get; set; }
        public long? AddressId { get; set; }
        public int? SellLocationId { get; set; }
    }
}
