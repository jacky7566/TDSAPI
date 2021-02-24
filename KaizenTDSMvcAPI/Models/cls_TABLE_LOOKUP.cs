using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KaizenTDSMvcAPI.Models
{
    /// <summary>
    /// Table for Lookup
    /// </summary>
    public class TABLE_LOOKUP
    {
        /// <summary>
        /// API Lookup Name
        /// </summary>
        public string TABLE_NAME { get; set; }
        /// <summary>
        /// Purpose for Lookup
        /// </summary>
        public string TYPE { get; set; }
        /// <summary>
        /// Lookup Key
        /// </summary>
        public string KEY { get; set; }
        /// <summary>
        /// Lookup Value
        /// </summary>
        public string VALUE { get; set; }
        /// <summary>
        /// Created By
        /// </summary>
        public string CREATED_BY { get; set; }
        /// <summary>
        /// Created Date
        /// </summary>
        public DateTime CREATED_DATE { get; set; }
        /// <summary>
        /// Modified By
        /// </summary>
        public string MODIFIED_BY { get; set; }
        /// <summary>
        /// Modified Date
        /// </summary>
        public DateTime MODIFIED_DATE { get; set; }
        /// <summary>
        /// Lookup Version
        /// </summary>
        public string VERSION { get; set; }
        /// <summary>
        /// Attribute for the lookup (ex: Products)
        /// </summary>
        public string ATTRIBUTE { get; set; }
        /// <summary>
        /// Lookup Description
        /// </summary>
        public string DERCRIPTION { get; set; }
    }
}