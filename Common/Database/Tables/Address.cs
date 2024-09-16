using Database.Resources;
using Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Tables
{
    public class Address
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        [Required]
        [MaxLength(255)]
        public string City { get; set; }
		[Required]
		[MaxLength(255)]
		public string Street { get; set; }
		[Required]
		[MaxLength(255)]
        public string? HouseNumber { get; set; }
        [MaxLength(255)]
        public string? Building { get; set; }
        [MaxLength(255)]
        public string? Flat { get; set; }
        [MaxLength(255)]
        public string? Comment { get; set; }

        [ForeignKey("User")]
        public long UserId { get; set; }
        public User? User { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (City.IsNotNullOrEmpty())
            {
                sb.Append($"{ApplicationContextText.City}: {City} ");
            }
            if (Street.IsNotNullOrEmpty())
            {
                sb.Append($"{ApplicationContextText.Street}: {Street} ");
            }
            if (HouseNumber.IsNotNullOrEmpty())
            {
                sb.Append($", {HouseNumber} ");
            }
            if (Building.IsNotNullOrEmpty())
            {
                sb.Append($"{ApplicationContextText.Building}: {Building} ");
            }
            if (Flat.IsNotNullOrEmpty())
            {
                sb.Append($"{ApplicationContextText.Flat}: {HouseNumber} ");
            }

            return sb.ToString();
        }
    }
}
