using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KaizenTDSMvcAPI.Models.Classes
{
    public class Cls_Return
    {
       public bool Success { get; set; }
        public string Error_Message { get; set; }

        public object Data { get; set; }

        public int datacount { get; set; }
    }
}