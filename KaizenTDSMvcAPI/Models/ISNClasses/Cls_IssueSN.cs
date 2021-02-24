using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KaizenTDSMvcAPI.Models.Classes
{
    public class Cls_IssueSN
    {
 
        public string po_number { get; set; }
        public string order_type { get; set; }
        public string po_line_id { get; set; }
        public string part_number { get; set; }
        public string vendor_id { get; set; }
        public int ACT_QTY { get; set; }
        public int Bth_Qty { get; set; }
    }
}