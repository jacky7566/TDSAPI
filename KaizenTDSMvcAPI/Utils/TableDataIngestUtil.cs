using Dapper;
using KaizenTDSMvcAPI.Models.KaizenTDSClasses;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Transactions;
using System.Web;
using SystemLibrary.Utility;

namespace KaizenTDSMvcAPI.Utils
{
    public class TableDataIngestUtil
    {
        const int BATCH_SIZE = 1024;
        public static string ProcessTableData(TableDataVClass tblData)
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
                        if (ExtensionHelper.ListsContainAMatchingValue(configCols.Select(r => r.COLUMN_NAME).ToList(), fileCols))
                        {
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
                throw ex;
            }
            
            return errorMsg;
        }

        private static List<string> GetTableColumns(string tableName)
        {
            List<string> result = new List<string>();
            string sql = string.Format(@"select upper(value) from table_lookup 
                                        where table_name = '{0}' and type = 'TableMapping' ", tableName);
            using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DEFAULTCONNECTIONSTRING))
            {
                result = sqlConn.Query<string>(sql).ToList();
            }

            return result;
        }

        private static List<AllTblColClass> GetTableColumnsN(string tableName)
        {
            List<AllTblColClass> result = new List<AllTblColClass>();
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
                                where t.table_name = '{0}' 
                                and col.column_name not in ('ROWNUMBER','TABLEID','TESTHEADERID','TESTHEADERSTEPID', 'CREATEDDATE', 'LASTMODIFIEDDATE')
                                order by column_id asc ", tableName);
            using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
            {
                result = sqlConn.Query<AllTblColClass>(sql).ToList();
            }

            return result;
        }

        private static string InsertTableData(List<Dictionary<string, string>> listDic, TableDataVClass tblData, List<AllTblColClass> allColList)
        {
            var errMsg = string.Empty;
            string createdDate = string.Empty;

            using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
            {
                var sql = string.Format(@"SELECT CREATEDDATE FROM {0} 
                                    WHERE TABLEID = {1}", tblData.TABLENAME.ToUpper(), tblData.TABLEID);
                var list = sqlConn.Query<string>(sql).ToList();

                if (list.Count() > 0)
                {
                    DateTime parsedDate = DateTime.Parse(list.FirstOrDefault());
                    createdDate = string.Format("TO_DATE('{0}', 'YYYY-MM-DD HH24:MI:SS')", parsedDate.ToString("yyyy-MM-dd HH:mm:ss"));
                    sql = string.Format("DELETE {0} WHERE TABLEID = {1}", tblData.TABLENAME, tblData.TABLEID);
                    sqlConn.Execute(sql);
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
                //pCols = listDic[0].Keys.Aggregate((res, next) => res + ", :" + next);
                //pCols = "[" + pCols + "]";
                //pCols = string.Join(",", listDic[0].Keys.Select(o => $":{o}").ToArray());
                for (int i = 0; i < listDic[0].Count; i++)
                {
                    if (i == listDic[0].Count - 1)
                        pCols = pCols + ":COLUMN" + (i + 1);
                    else
                        pCols = pCols + ":COLUMN" + (i + 1) + ",";
                }
                //pCols = listDic[0].Keys.Aggregate((res, next) => res + @""", :""" + next);
                //pCols = @":""" + pCols + @"""";
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
                        //dp.Add(":COLUMN" + (k + 1), listDic[i][key]);
                        //k++;
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

                    //LogHelper.WriteLine(string.Format(@"INSERT INTO {0} (ROWNUMBER, TABLEID, TESTHEADERID, TESTHEADERSTEPID, {1}, CREATEDDATE,                                              LASTMODIFIEDDATE) 
                    //                                    VALUES ({2}, {3}, {4}, {5}, '{6}', SYSDATE, SYSDATE)",
                    //                                    tblData.TABLENAME,
                    //                                    cols.ToUpper(),
                    //                                    i + 1,
                    //                                    tblData.TABLEID, tblData.TESTHEADERID, tblData.TESTHEADERSTEPID,
                    //                                    listDic[i].Values.Aggregate((res, next) => res + "', '" + next)));
                    dynamicList.Add(dp);
                }

                sqlConn.Open();

                var count = 0;
                LogHelper.WriteLine("BeginTransaction - Count: " + dynamicList.Count());


                using (var tran = sqlConn.BeginTransaction())
                {
                    try
                    {
                        foreach (var batchData in SplitBatch<OracleDynamicParameters>(dynamicList, BATCH_SIZE))
                        {
                            count += batchData.Length;
                            sqlConn.Execute(sql, batchData, tran);
                            LogHelper.WriteLine($"\r{count}/{dynamicList.Count()}({count * 1.0 / dynamicList.Count():p0})");
                        }
                        tran.Commit();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        throw ex;
                    }

                }


                //using (var tran = sqlConn.BeginTransaction())
                //{
                //    try
                //    {
                //        sqlConn.Execute(sql, dynamicList, tran);
                //        tran.Commit();
                //    }
                //    catch (Exception ex)
                //    {
                //        tran.Rollback();
                //        throw ex;
                //    }
                //}
                //using (var scope = new TransactionScope())
                //{
                //    try
                //    {
                //        sqlConn.Execute(sql, dynamicList);
                //        scope.Complete();
                //    }
                //    catch (Exception ex)
                //    {
                //        throw ex;
                //    }
                //}
            }
            return errMsg;
        }

        static IEnumerable<T[]> SplitBatch<T>(IEnumerable<T> items, int batchSize)
        {
            return items.Select((item, idx) => new { item, idx })
                .GroupBy(o => o.idx / batchSize)
                .Select(o => o.Select(p => p.item).ToArray());
        }
    }
}