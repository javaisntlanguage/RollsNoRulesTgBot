using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Tables
{
    public class Product
    {
        public int Id { get; set; }
        [MaxLength(64)]
        [Required]
        public string? Name { get; set; }
        [MaxLength(255)]
        public string? Description { get; set; }
        public decimal Price { get; set; }
		[Column(TypeName = "varchar(MAX)")]
		public string? Photo { get; set; }
        public bool IsVisible { get; set; }
        [JsonIgnore()]
        public ICollection<ProductCategory>? ProductCategories { get; set; }
    }
}
