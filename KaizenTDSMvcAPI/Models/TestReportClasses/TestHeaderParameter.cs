using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KaizenTDSMvcAPI.Models.TestReportClasses
{
    public class TestHeaderParameter
    {
        public string No { get; set; }
        public string OPERATIONSTEPNAME { get; set; }
        public string PARAMETERNAME { get; set; }
        public string UNITS { get; set; }
        public double VALUE { get; set; }
        public double SPECMIN { get; set; }
        public double SPECMAX { get; set; }
        public string STATUS { get; set; }
    }
}