using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KaizenTDSMvcAPI.Models
{
    public class SpArgumentsClass
    {
        public string OWNER { get; set; }
        public string OBJECT_NAME { get; set; }
        public string PACKAGE_NAME { get; set; }
        public int SEQUENCE { get; set; }
        public string ARGUMENT_NAME { get; set; }
        public string DATA_TYPE { get; set; }
        public string IN_OUT { get; set; }
        public int DATA_LENGTH { get; set; }
    }
}