using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KaizenTDSMvcAPI.Models.TestReportClasses
{
    public class TestHeader
    {
        public string SERIALNUMBER { get; set; }
        public string PRODUCTFAMILYNAME { get; set; }
        public string PARTNUMBER { get; set; }
        public string OPERATIONNAME { get; set; }
        public DateTime ENDTIME { get; set; }
        public string RESULT { get; set; }
    }
}