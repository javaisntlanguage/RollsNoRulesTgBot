using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Tables
{
    public class AdminWork
    {
        public int Id { get; set; }
        [ForeignKey("Admin")]
        public int AdminId { get; set; }
        public DateTimeOffset WorkDate { get; set; }

        public AdminCredential? Admin {  get; set; }
    }
}
