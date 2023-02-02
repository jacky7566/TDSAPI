using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KaizenTDSMvcAPI.Models.KaizenTDSClasses
{
    public class SystemVersionClass
    {
        public int SYSTEMVERSIONID { get; set; }
        public string SYSTEMNAME { get; set; }
        public string SERVERNAME { get; set; }
        public string SYSTEMURL { get; set; }
        public string UIVERSION { get; set; }
        public string APIVERSION { get; set; }
        public string INGESTIONDLLVERSION { get; set; }
        public string DATAUPLOADERVERSION { get; set; }
        public string CREATEDBY { get; set; }
        public DateTime CREATEDDATE { get; set; }
        public string LASTMODIFIEDBY { get; set; }
        public DateTime LASTMODIFIEDDATE { get; set;        }
    }
}