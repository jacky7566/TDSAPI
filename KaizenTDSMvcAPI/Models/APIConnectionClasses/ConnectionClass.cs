using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KaizenTDSMvcAPI.Models.APIConnectionClasses
{
    public class ConnectionClass
    {
        public string APICONNECTIONNAME { get; set; }
        public string DATABASECONNECTIONSTRING { get; set; }        
        public string DEFAULTCONNECTIONSTRING { get; set; }
    }
}