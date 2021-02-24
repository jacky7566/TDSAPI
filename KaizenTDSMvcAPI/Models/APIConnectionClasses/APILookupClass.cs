using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KaizenTDSMvcAPI.Models.APIConnectionClasses
{
    public class APILookupClass
    {
        public int APILOOKUPID { get; set; }
        public string APILOOKUPNAME { get; set; }
        public string APICOMMANDTYPE { get; set; }
        public string COMMANDOPERATION { get; set; }
        public string COMMANDVALUE { get; set; }
        public string VERSION { get; set; }
        public string BASETAG { get; set; }
    }
}