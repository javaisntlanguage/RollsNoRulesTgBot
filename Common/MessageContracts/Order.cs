using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageContracts
{
    public class Order : IOrder
    {
        public int OrderId { get; set; }
        public Guid Id { get; set; }
    }
}
