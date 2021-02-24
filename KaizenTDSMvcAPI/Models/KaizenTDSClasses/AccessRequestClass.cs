using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KaizenTDSMvcAPI.Models.KaizenTDSClasses
{
    public class AccessRequestClass
    {
        public int ACCESSREQUESTID { get; set; }
        public string EMPLOYEEID { get; set; }
        public string EMAILID { get; set; }
        public string FIRSTNAME { get; set; }
        public string LASTNAME { get; set; }
        public int ROLEID { get; set; }
        public int PRODUCTFAMILYID { get; set; }
        public string DESCRIPTION { get; set; }
        public string CREATEDBY { get; set; }
        public DateTime CREATEDDATE { get; set; }
        public string LASTMODIFIEDBY { get; set; }
        public DateTime LASTMODIFIEDDATE { get; set; }
        public string RESULT { get; set; }
        public string ROLENAME { get; set; }
        public string PRODUCTFAMILYNAME { get; set; }
    }
}