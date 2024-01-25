using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Tables
{
    public class Category
    {
        public int Id { get; set; }
        [MaxLength(32)]
        [Required]
        public string Name { get; set; }
        public bool IsVisible { get; set; }
    }
}
