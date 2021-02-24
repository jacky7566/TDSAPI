using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KaizenTDSDLL.TableDataIngestionUtils
{
    /// <summary>
    /// TableData_V Class mapping
    /// </summary>
    public sealed class TableDataVClass
    {
        public int TABLEID { get; set; }
        public int TESTHEADERID { get; set; }
        public int TESTHEADERSTEPID { get; set; }
        public string TABLENAME { get; set; }
        public string ARCHIVEFILENAME { get; set; }
        public int PRODUCTID { get; set; }
    }
}