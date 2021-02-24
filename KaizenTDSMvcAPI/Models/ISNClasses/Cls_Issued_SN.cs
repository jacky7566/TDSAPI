using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KaizenTDSMvcAPI.Models.Classes
{
    public class Cls_Issued_SN
    {
        public string po_number { set; get; }
        public string ordertype { set; get; }
        public string po_line { set; get; }
        public string prdfamily { set; get; }
        public string part_number { set; get; }
        public int ActQty { set; get; }
        public int BchQty { set; get; }
        public string strCalcFromSN { set; get; }
        public string strCalcTOSN { set; get; }
        public string Venuid { set; get; }
        public string Ventransferdate { set; get; }
        public List<string> ListSN { set; get; } = new List<string>();
        public string strMFGId { set; get; }

        public string Filename { set; get;   }
        

    }
}