using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Week2_Assign1.Models
{
    public class User : IdentityUser
    {
        public Boolean isDelete { get; set; }
    }
}
