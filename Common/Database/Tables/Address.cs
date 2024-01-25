using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Tables
{
    public class Address
    {
        public long Id { get; set; }
        [MaxLength(255)]
        public string City { get; set; }
        [MaxLength(255)]
        public string Street { get; set; }
        public int? HouseNumber { get; set; }
        [MaxLength(255)]
        public string Building { get; set; }
        [MaxLength(255)]
        public string Flat { get; set; }
        [MaxLength(255)]
        public string Comment { get; set; }
        public virtual User User { get; set; }
    }
}
