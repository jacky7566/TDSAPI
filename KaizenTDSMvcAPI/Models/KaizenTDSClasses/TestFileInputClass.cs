using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KaizenTDSMvcAPI.Models.KaizenTDSClasses
{
    public class TestFileInputClass
    {
        public string APIConnectionName { get; set; }
        public List<TestFileInputDetailClass> InputList { get; set; }
    }

    public class TestFileInputDetailClass
    {
        public string TESTHEADERID { get; set; }
        public string FILENAME { get; set; }
        public string TABLENAME { get; set; }
    }
}