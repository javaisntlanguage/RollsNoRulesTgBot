using Database.Tables;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class StartTakingOrderResult
    {
        public Order TargetOrder { get; set; }
        public IDbContextTransaction Transaction { get; set; }
    }
}
