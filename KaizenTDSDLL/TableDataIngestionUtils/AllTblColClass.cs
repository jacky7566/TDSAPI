using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KaizenTDSDLL.TableDataIngestionUtils
{
    public class AllTblColClass
    {
        public int COLUMN_ID { get; set; }
        public string SCHEMA_NAME { get; set; }
        public string TABLE_NAME { get; set; }
        public string COLUMN_NAME { get; set; }
        public string DATA_TYPE { get; set; }
        public int DATA_LENGTH { get; set; }
        public int DATA_SCALE { get; set; }
        public string NUALLABLE { get; set; }
    }
}