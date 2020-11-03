using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ContractAPI.Models
{
    public class User
    {
       
        public string user_id { get; set; }
        public int user_role { get; set; }
        public int user_status { get; set; }
    }
}