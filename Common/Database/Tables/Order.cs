using Database.Enums;
using Database.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Database.Tables
{
    public class Order
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int Number { get; set; }
        public OrderState State { get; set; } = OrderState.New;
        public DateTimeOffset DateFrom { get; set; }
        [Required]
        [MaxLength(11)]
        public string Phone {  get; set; }
        public decimal Sum { get; set; }

        [ForeignKey("Address")]
        public long? AddressId { get; set; }
        public Address? Address { get; set; }

        [ForeignKey("SellLocation")]
        public int? SellLocationId { get; set; }
        public SellLocation? SellLocation { get; set; }

        [ForeignKey("User")]
        public long UserId { get; set; }
        [JsonIgnore()]
        public User? User { get; set; }

        public List<OrderCart>? OrderCartList { get; set; }
	}
}
