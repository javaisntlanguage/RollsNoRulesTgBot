using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Tables
{
    public class SellLocation
    {
        public const int MAX_NAME_LENGTH = 255;
        public const int MIN_NAME_LENGTH = 3;
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
		[Required]
		[MaxLength(MAX_NAME_LENGTH)]
		[MinLength(MIN_NAME_LENGTH)]
        public string Name { get; set; }
    }
}
