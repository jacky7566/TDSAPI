using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KaizenTDSMvcAPI.Models.KaizenTDSClasses
{
    public class DeviceRelationClass
    {
        public string DEVICETYPE { get; set; }

        public string DEVICERELATIONID { get; set; }

        public string SERIALNUMBER { get; set; }

        public string PARTNUMBER { get; set; }

        public string PRODUCTID { get; set; }

        public string RELATETOSERIALNUMBER { get; set; }

        public string RELATETOPARTNUMBER { get; set; }

        public string RELATETOPARTTYPE { get; set; }

        public string RELATETOREV { get; set; }

        public string CREATEDBY { get; set; }

        public string CREATEDDATE { get; set; }

        public string LASTMODIFIEDBY { get; set; }

        public string LASTMODIFIEDDATE { get; set; }

        public string DEVICEID { get; set; }

    }
}