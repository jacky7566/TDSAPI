using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KaizenTDSMvcAPI.Models.PSMReport
{
    public class PSMReport
    {
        public string JDE_PRODUCT_ID { get; set; }
        public string CRITERIA_MASTER_ID { get; set; }
        public string SPEC_VALUE { get; set; }
        public string CRIT_BINNING_SPEC_YN { get; set; }
        public string MAST_NAME { get; set; }
    }
}