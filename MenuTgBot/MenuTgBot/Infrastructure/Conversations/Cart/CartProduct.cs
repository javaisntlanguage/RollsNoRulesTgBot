using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuTgBot.Infrastructure.Conversations.Cart
{
    internal class CartProduct
    {
        public CartProduct(int id, int count = 1)
        {
            Id = id;
            Count = count;
        }

        public int Id { get; set; }
        public int Count { get; set; }
    }
}
