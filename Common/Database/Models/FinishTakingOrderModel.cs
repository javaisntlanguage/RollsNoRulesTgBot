using Database.Tables;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class FinishTakingOrderModel
    {
        public Order TargetOrder { get; set; }
        public IDbContextTransaction Transaction { get; set; }
        public IEnumerable<OutboxMessage>? EventsToPublish { get; set; }
        public IEnumerable<OrderCart> Cart {  get; set; }
    }
}
