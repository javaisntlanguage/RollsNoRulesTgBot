using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Enums
{
    public enum OrderState
    {
        None = 0,
        New = 1,
        Completed = 1 << 1,
        Declined = 1 << 2,
        Approved = 1 << 3,
        Error = 1 << 4
	}
}
