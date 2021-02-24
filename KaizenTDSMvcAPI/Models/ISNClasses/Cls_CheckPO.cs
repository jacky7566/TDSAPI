using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KaizenTDSMvcAPI.Models.Classes
{
    public class Cls_CheckPO
    {
        public string Po_Number { get; set; }
       public string Vendor_id { get; set; }
        public int start { get; set; }
        public int end { get; set; }
         
    }
}