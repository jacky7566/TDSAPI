using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KaizenTDSMvcAPI.Models
{
    /// <summary>
    /// GetDataBySQLClass
    /// </summary>
    public class GetDataBySQLClass
    {
        /// <summary>
        /// System name
        /// </summary>
        public string apiConnName { get; set; }
        /// <summary>
        /// SQL
        /// </summary>
        public string sql { get; set; }
        /// <summary>
        /// Default = false; Athena data check only, will ingore check from Oracle
        /// </summary>
        public bool isCheckAthena { get; set; } = false;
        /// <summary>
        /// Return format (xml or json), default = json
        /// </summary>
        public string format { get; set; } = "json";
    }
}