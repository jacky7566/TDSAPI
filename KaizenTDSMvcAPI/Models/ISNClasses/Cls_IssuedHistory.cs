using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KaizenTDSMvcAPI.Models.Classes
{
    public class Cls_IssuedHistory
    {
        public string Vendor_Name { get; set; }
        public string Part_Number { get; set; }
        public string Po_Number { get; set; }
        public string SerialNo { get; set; }
        public bool Group { get; set; }
        public int start { get; set; }
        public int end { get; set; }
    }
}