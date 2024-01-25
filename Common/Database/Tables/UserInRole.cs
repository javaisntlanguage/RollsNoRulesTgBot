using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Tables
{
    [PrimaryKey(nameof(UserId), nameof(RoleId))]
    public class UserInRole
    {
        [ForeignKey("User")]
        public long UserId { get; set; }
        [ForeignKey("Role")]
        public int RoleId { get; set; }
        public virtual User User { get; set; }
        public virtual Role Role { get; set; }
    }
}
