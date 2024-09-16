using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Tables
{
    public class User
    {
        public long Id { get; set; }
        public ICollection<Address>? Addresses { get; set; }
    }
}
