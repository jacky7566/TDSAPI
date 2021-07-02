using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KaizenTDSMvcAPI.Models.KaizenTDSClasses
{
    public class TestFileDownloadClass
    {
        public string TESTHEADERID { get; set; }
        public bool ARCHIVEFILECLEANUPSTATUS { get; set; }
        public string ARCHIVEFOLDER { get; set; }
        public string FILENAME { get; set; }
        public string ARCHIVEFILENAME { get; set; }
    }
}