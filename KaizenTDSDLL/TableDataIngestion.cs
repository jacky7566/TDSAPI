using Dapper;
using KaizenTDSDLL.TableDataIngestionUtils;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KaizenTDSDLL
{
    public class TableDataIngestion
    {
        #region Auto-implemented Properties
        public string ErrorMessage;
        #endregion

        private OracleConnection oraConn;
        private string target_schema;
        private int tableid;
        
        public TableDataIngestion(OracleConnection oraConn, string target_schema, int tableid)
        {
            this.oraConn = oraConn;
            this.target_schema = target_schema;
            this.tableid = tableid;
        }

        public bool Inserting()
        {
            try
            {
                TableDataIngestUtil util = new TableDataIngestUtil(this.oraConn, this.target_schema);
                string sql = string.Format(@"select tableid, tablename, filenamelong as archivefilename, productid, testheaderid, testheaderstepid from tabledata_v where tableid = {0}", this.tableid);

                var list = this.oraConn.Query<TableDataVClass>(sql).ToList();
                if (list != null && list.Count() > 0)
                {
                    string msg = util.ProcessTableData(list.FirstOrDefault());
                    if (string.IsNullOrEmpty(msg))
                    {
                        this.ErrorMessage = "Success";
                        return true;
                    }
                    else
                    {
                        this.ErrorMessage = msg;
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                this.ErrorMessage = string.Format("Connection State: {0}, {1}", this.oraConn.State.ToString(),  this.GetAllFootprints(ex));
                //ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, "TableDataIngestion", 2, tableId, null);
            }
            return false;
        }

        private string GetAllFootprints(Exception x)
        {
            var st = new StackTrace(x, true);
            var frames = st.GetFrames();
            var traceString = new StringBuilder();

            foreach (var frame in frames)
            {
                if (frame.GetFileLineNumber() < 1)
                    continue;

                //traceString.Append("File: " + frame.GetFileName());
                traceString.Append("Method:" + frame.GetMethod().Name);
                traceString.Append(", LineNumber: " + frame.GetFileLineNumber());
                traceString.Append("  -->  ");
                traceString.AppendLine();
            }
            traceString.Append("Message: " + x.Message);
            traceString.AppendLine();
            traceString.Append("StackTrace: " + x.StackTrace);
            traceString.AppendLine();
            traceString.Append("InnerException: " + x.InnerException);
            traceString.AppendLine();

            return traceString.ToString();
        }
    }
}
