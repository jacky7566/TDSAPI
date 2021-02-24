using Dapper;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using SystemLibrary.Utility;

namespace KaizenTDSDLL.TableDataIngestionUtils
{
    public class TableDataIngestUtil
    {
        private static OracleConnection _OracleConn;
        private static string _TargetSchema;
        const int BATCH_SIZE = 5000;

        public TableDataIngestUtil(OracleConnection oracleConn, string targetSchema)
        {
            _OracleConn = oracleConn;
            _TargetSchema = targetSchema;
        }

        public string ProcessTableData(TableDataVClass tblData)
        {
            string errorMsg = string.Empty;
            try
            {
                LogHelper.WriteLine(string.Format("ProcessTableData starting... File: {0}", tblData.ARCHIVEFILENAME));
                if (File.Exists(tblData.ARCHIVEFILENAME))
                {
                    var result = ExcelOperator.GetCsvDataToDic(tblData.ARCHIVEFILENAME, false);
                    if (result != null && result.Count() > 0)
                    {
                        var fileCols = result[0].Keys.Select(r => r).ToList();
                        var configCols = GetTableColumnsN(tblData.TABLENAME);
                        if (ListsContainAMatchingValue(configCols.Select(r => r.COLUMN_NAME).ToList(), fileCols))
                        {
                            tblData.TABLENAME = string.Format("{0}.{1}", configCols.FirstOrDefault().SCHEMA_NAME, tblData.TABLENAME);
                            errorMsg = InsertTableData(result, tblData, configCols);
                        }
                        else
                        {
                            errorMsg = string.Format("File columns error, file path: {0}", tblData.ARCHIVEFILENAME);
                        }
                    }
                    else
                    {
                        errorMsg = string.Format("TableData empty, file path: {0}", tblData.ARCHIVEFILENAME);
                    }
                }
                else errorMsg = string.Format("File not exsit, file path: {0}", tblData.ARCHIVEFILENAME);
            }
            catch (Exception ex)
            {
                return Utilities.GetAllFootprints(ex);
            }
            
            return errorMsg;
        }

        private static List<string> GetTableColumns(string tableName)
        {
            List<string> result = new List<string>();
            string sql = string.Format(@"select upper(value) from table_lookup 
                                        where table_name = '{0}' and type = 'TableMapping' ", tableName);
            using (var sqlConn = _OracleConn)
            {
                result = sqlConn.Query<string>(sql).ToList();
            }

            return result;
        }

        private static List<AllTblColClass> GetTableColumnsN(string tableName)
        {
            List<AllTblColClass> result = new List<AllTblColClass>();
            try
            {
                string sql = string.Format(@"select col.column_id, 
                                       col.owner as schema_name,
                                       col.table_name, 
                                       col.column_name, 
                                       col.data_type, 
                                       col.data_length, 
                                       col.data_precision, 
                                       col.data_scale, 
                                       col.nullable
                                from sys.all_tab_columns col
                                inner join sys.all_tables t on col.owner = t.owner 
                                and col.table_name = t.table_name
                                where t.table_name = '{0}'  and col.owner = '{1}'
                                and col.column_name not in ('ROWNUMBER','TABLEID','TESTHEADERID','TESTHEADERSTEPID', 'CREATEDDATE', 'LASTMODIFIEDDATE')
                                order by column_id asc ", tableName, _TargetSchema);

                result = _OracleConn.Query<AllTblColClass>(sql).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("GetTableColumnsN", ex);
            }
            
            return result;
        }

        private string InsertTableData(List<Dictionary<string, string>> listDic, TableDataVClass tblData, List<AllTblColClass> allColList)
        {
            var errMsg = string.Empty;
            string createdDate = string.Empty;
            var sql = string.Format(@"SELECT CREATEDDATE FROM {0} 
                                    WHERE TABLEID = {1}", tblData.TABLENAME.ToUpper(), tblData.TABLEID);

            try
            {
                
                var list = _OracleConn.Query<string>(sql).ToList();

                if (list.Count() > 0)
                {
                    DateTime parsedDate = DateTime.Parse(list.FirstOrDefault());
                    createdDate = string.Format("TO_DATE('{0}', 'YYYY-MM-DD HH24:MI:SS')", parsedDate.ToString("yyyy-MM-dd HH:mm:ss"));
                    sql = string.Format("DELETE {0} WHERE TABLEID = {1}", tblData.TABLENAME, tblData.TABLEID);
                    _OracleConn.Execute(sql);
                }
                else
                {
                    createdDate = "SYSDATE";
                }

                var cols = string.Empty;
                var pCols = string.Empty;
                var vals = string.Empty;

                cols = listDic[0].Keys.Aggregate((res, next) => res + @""", """ + next);
                cols = @"""" + cols + @"""";

                for (int i = 0; i < listDic[0].Count; i++)
                {
                    if (i == listDic[0].Count - 1)
                        pCols = pCols + ":COLUMN" + (i + 1);
                    else
                        pCols = pCols + ":COLUMN" + (i + 1) + ",";
                }

                sql = string.Format(@"INSERT INTO {0} (ROWNUMBER, TABLEID, TESTHEADERID, TESTHEADERSTEPID, {1}, CREATEDDATE, LASTMODIFIEDDATE) 
                                        VALUES (:ROWNUMBER, :TABLEID, :TESTHEADERID, :TESTHEADERSTEPID, {2}, {3}, SYSDATE)",
                                        tblData.TABLENAME, cols, pCols, createdDate);

                List<OracleDynamicParameters> dynamicList = new List<OracleDynamicParameters>();
                OracleDynamicParameters dp;
                DateTime dTimeTemp;
                int k = 0;
                for (int i = 0; i < listDic.Count(); i++)
                {
                    dp = new OracleDynamicParameters();
                    dp.Add(":ROWNUMBER", i + 1, OracleDbType.Int32);
                    dp.Add(":TABLEID", tblData.TABLEID, OracleDbType.Int32);
                    dp.Add(":TESTHEADERID", tblData.TESTHEADERID, OracleDbType.Int32);
                    dp.Add(":TESTHEADERSTEPID", tblData.TESTHEADERSTEPID, OracleDbType.Int32);

                    foreach (var key in listDic[i].Keys)
                    {
                        #region TestCode
                        var colInfo = allColList.Where(r => r.COLUMN_NAME.ToUpper() == key.ToUpper()).FirstOrDefault();
                        if (colInfo != null)
                        {
                            if (colInfo.DATA_TYPE.ToUpper() == "DATE")
                            {
                                DateTime.TryParse(listDic[i][key], out dTimeTemp);
                                dp.Add(":COLUMN" + (k + 1), dTimeTemp);
                            }
                            else
                            {
                                dp.Add(":COLUMN" + (k + 1), listDic[i][key]);
                            }
                        }
                        else
                        {
                            dp.Add(":COLUMN" + (k + 1), listDic[i][key]);
                        }
                        k++;
                        #endregion
                    }
                    k = 0;

                    dynamicList.Add(dp);
                }
                var count = 0;

                if (dynamicList.Count() < BATCH_SIZE)
                    _OracleConn.Execute(sql, dynamicList);
                else
                {
                    var tran = _OracleConn.BeginTransaction();
                    try
                    {
                        var splitBatch = SplitBatch<OracleDynamicParameters>(dynamicList, BATCH_SIZE);
                        foreach (var batchData in SplitBatch<OracleDynamicParameters>(dynamicList, BATCH_SIZE))
                        {
                            count += batchData.Length;
                            _OracleConn.Execute(sql, batchData, tran);
                            LogHelper.WriteLine($"\r{count}/{dynamicList.Count()}({count * 1.0 / dynamicList.Count():p0})");
                        }
                        tran.Commit();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return Utilities.GetAllFootprints(ex);
                    }
                    //using (var tran = _OracleConn.BeginTransaction())
                    //{

                    //}
                }
            }
            catch (Exception ex)
            {
                return Utilities.GetAllFootprints(ex);
            }

            return errMsg;
        }

        private IEnumerable<T[]> SplitBatch<T>(IEnumerable<T> items, int batchSize)
        {
            return items.Select((item, idx) => new { item, idx })
                .GroupBy(o => o.idx / batchSize)
                .Select(o => o.Select(p => p.item).ToArray());
        }

        private bool ListsContainAMatchingValue(IEnumerable<string> listA, IEnumerable<string> listB)
        {
            foreach (var item in listB)
            {
                if (listA.Contains(item) == false) return false;
            }
            return true;
        }

    }
}