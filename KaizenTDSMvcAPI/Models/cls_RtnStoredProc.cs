using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KaizenTDSMvcAPI.Models
{
    /// <summary>
    /// Stored Procedure Output Class
    /// </summary>
    public class SPOutputClass
    {
        /// <summary>
        /// Output Cursor
        /// </summary>
        public List<dynamic> OutputList { get; set; }
        /// <summary>
        /// Output Total Page
        /// </summary>
        public int OutTotalPage { get; set; }
        /// <summary>
        /// Output Message
        /// </summary>
        public string OutMessage { get; set; }
    }
}