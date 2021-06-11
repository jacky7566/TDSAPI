using Dapper;
using KaizenTDSMvcAPI.Models;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Web;

namespace KaizenTDSMvcAPI.Utils
{
    /// <summary>
    /// Stored Procedure Helper
    /// </summary>
    public class StoredProcedureHelper
    {
        public static SPOutputClass GetResBySPFunc(string StoredProcedureName, string Criteria = null)
        {
            //string out_message = string.Empty;
            SPOutputClass rsp = new SPOutputClass();
            try
            {
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                {
                    OracleDynamicParameters dps = new OracleDynamicParameters();
                    dps.Add("in_criteria", string.IsNullOrEmpty(Criteria) ? "" : Criteria, OracleDbType.Varchar2, ParameterDirection.Input);
                    dps.Add("oRefCursor", "", OracleDbType.RefCursor, ParameterDirection.Output);
                    var m = sqlConn.QueryMultiple(StoredProcedureName, dps, commandType: System.Data.CommandType.StoredProcedure);
                    var data = m.Read();
                    rsp = new SPOutputClass()
                    {
                        OutputList = data.ToList(),
                        OutTotalPage = 0,
                        OutMessage = "NA"
                    };
                }
            }
            catch (Exception ex)
            {
                //out_message = System.Reflection.MethodBase.GetCurrentMethod().Name + ":" + ex.Message;
                throw ex;
            }

            return rsp;
        }

        public static SPOutputClass GetResBySpAndPagedFunc(string StoredProcedureName, int page = 1, int pageSize = 1000, string Criteria = null)
        {
            SPOutputClass rsp;
            try
            {
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                {
                    OracleDynamicParameters dps = new OracleDynamicParameters();
                    dps.Add("in_criteria", string.IsNullOrEmpty(Criteria) ? "" : Criteria, OracleDbType.Varchar2, ParameterDirection.Input);
                    dps.Add("in_page_number", page, OracleDbType.Int32, ParameterDirection.Input);
                    dps.Add("in_page_count", pageSize, OracleDbType.Int32, ParameterDirection.Input);
                    dps.Add("out_total_page", "", OracleDbType.Int32, ParameterDirection.Output);
                    dps.Add("out_message", "", OracleDbType.Varchar2, ParameterDirection.Output);
                    dps.Add("oRefCursor", "", OracleDbType.RefCursor, ParameterDirection.Output);

                    var m = sqlConn.QueryMultiple(StoredProcedureName, dps, commandType: System.Data.CommandType.StoredProcedure);
                    var data = m.Read();
                    rsp = new SPOutputClass()
                    {
                        OutputList = data.ToList(),
                        OutMessage = dps.Get<OracleString>("out_message").ToString(),
                        OutTotalPage = int.Parse(dps.Get<OracleDecimal>("out_total_page").ToString())
                    };
                }
            }
            catch (Exception ex)
            {
                //out_message = System.Reflection.MethodBase.GetCurrentMethod().Name + ":" + ex.Message;
                throw ex;
            }

            return rsp;
        }

        public static object ExecuteSpFunc(List<QueryParamClass> Input, string StoredProcedureName)
        {
            object output;
            int out_is_success = 0;
            string out_message = string.Empty;
            try
            {
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                {
                    if (Input != null)
                    {
                        //var dps = ConnectionHelper.CreateDPInstance(Input);
                        if (Input.Exists(r => r.Name.ToUpper().Contains("API")) == false)
                        {
                            Input = ConnectionHelper.AddDefaultApi2Input(Input, "api/Generic/Post");
                        }
                        //split package name and procedure name
                        var exeSpArry = StoredProcedureName.Split('.');
                        if (exeSpArry.Count() > 1)
                        {
                            //Get parameters from SYS.ALL_ARGUMENTS by PkgName + Stored Procedure name
                            var arguments = LookupHelper.GetSPArguments(exeSpArry[1], exeSpArry[0]);

                            if (arguments != null && arguments.Count() > 0)
                            {
                                //var dps = ConnectionHelper.CreateODPInstance(Input);
                                var dps = ConnectionHelper.CreateODPEasyInput(Input, arguments);
                                sqlConn.Execute(StoredProcedureName, dps, commandType: System.Data.CommandType.StoredProcedure);
                                out_is_success = dps.Get<OracleDecimal>("OUT_IS_SUCCESS").ToInt32();
                                out_message = dps.Get<OracleString>("OUT_MESSAGE").ToString();
                                if (out_is_success == 1)
                                {
                                    if (string.IsNullOrEmpty(out_message))
                                        out_message = "Excute success!";
                                }
                                else
                                {
                                    out_message = dps.Get<OracleString>("OUT_MESSAGE").ToString();
                                    ExtensionHelper.LogExpSPMessageToDB(null, out_message, HttpStatusCode.Conflict, StoredProcedureName + "_" + "ExecuteSpFunc", 2, out_message, null);
                                }                                    
                            }

                        }
                        else
                        {
                            out_message = string.Format("Wrong stored procedure format on lookup table! Stored Procedure: {0}", StoredProcedureName);
                        }


                   
                    }
                    else
                    {
                        out_message = "Please check your input!";                        
                    }
                    output = new { out_is_success, out_message };
                    return output;
                }
            }
            catch (Exception ex)
            {
                throw ex;
                //out_message = System.Reflection.MethodBase.GetCurrentMethod().Name + ":" + ex.Message;
                //output = new { out_is_success, out_message };
                //return output;
            }
        }

        /// <summary>
        /// Call Stored Procedure By Request
        /// </summary>
        /// <param name="kvpList"></param>
        /// <param name="spName"></param>
        /// <param name="argsList"></param>
        /// <returns></returns>
        public static SPOutputClass GetSPResByReqstr(List<KeyValuePair<string, string>> kvpList,
            string spName,
            List<SpArgumentsClass> argsList)
        {
            //string out_message = string.Empty;
            SPOutputClass rsp = new SPOutputClass();
            try
            {
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                {
                    OracleDynamicParameters dps = new OracleDynamicParameters();
                    SpArgumentsClass arg = null;
                    foreach (var kvp in kvpList)
                    {
                        arg = argsList.Find(r => r.ARGUMENT_NAME == kvp.Key.ToUpper().Trim());
                        if (arg != null)
                        {
                            var direction = arg.IN_OUT == "IN" ? ParameterDirection.Input : ParameterDirection.Output;
                            var dbType = OracleDbTypeParser(arg.DATA_TYPE);
                            if (direction == ParameterDirection.Output)
                                dps.Add(arg.ARGUMENT_NAME, kvp.Value, dbType, direction, 500);
                            else
                                dps.Add(arg.ARGUMENT_NAME, kvp.Value, dbType, direction, arg.DATA_LENGTH);
                        }
                    }
                    //dps.Add("in_criteria", string.IsNullOrEmpty(Criteria) ? "" : Criteria, OracleDbType.Varchar2, ParameterDirection.Input);
                    if (kvpList.Where(r => r.Key == "oRefCursor").Count() == 0)
                    {
                        arg = argsList.Find(r => r.ARGUMENT_NAME == "OREFCURSOR");
                        if (arg != null)
                        {
                            dps.Add(arg.ARGUMENT_NAME, "", OracleDbType.RefCursor, ParameterDirection.Output, arg.DATA_LENGTH);
                        }
                        else
                            dps.Add("oRefCursor", "", OracleDbType.RefCursor, ParameterDirection.Output);
                    }
                        
                    var m = sqlConn.QueryMultiple(spName, dps, commandType: System.Data.CommandType.StoredProcedure);
                    var data = m.Read();

                    rsp = new SPOutputClass()
                    {
                        OutputList = data.ToList(),
                        OutTotalPage = 0,
                        OutMessage = argsList.Find(r => r.ARGUMENT_NAME.ToUpper() == "OUT_MESSAGE") != null 
                        ? dps.Get<OracleString>("OUT_MESSAGE").ToString() : "NA"
                    };
                }
            }
            catch (Exception ex)
            {
                //out_message = System.Reflection.MethodBase.GetCurrentMethod().Name + ":" + ex.Message;
                throw ex;
            }

            return rsp;
        }

        public static OracleDbType OracleDbTypeParser(string dataType)
        {
            switch(dataType)
            {
                case "VARCHAR2":                    
                    return OracleDbType.Varchar2;
                case "DATE":
                    return OracleDbType.Date;
                case "CLOB":
                    return OracleDbType.Clob;
                case "FLOAT":
                    return OracleDbType.Decimal;
                case "REF CURSOR":
                    return OracleDbType.RefCursor;
                case "ROWID":
                    return OracleDbType.Varchar2;
                case "NVARCHAR2":
                    return OracleDbType.NVarchar2;
                case "LONG":
                    return OracleDbType.Long;
                case "NUMBER":
                    return OracleDbType.Decimal;
                case "INT":
                    return OracleDbType.Int32;
                case "BLOB":
                    return OracleDbType.Blob;
            }
            return OracleDbType.Varchar2;
        }

        public static ParameterDirection GetParameterDirection(string value)
        {
            switch(value.ToUpper())
            {
                case "IN":
                    return ParameterDirection.Input;
                case "OUT":
                    return ParameterDirection.Output;
                case "IN/OUT":
                    return ParameterDirection.Output;
            }
            return ParameterDirection.Input;
        }

        public static string ValueTypeParser(string value, OracleDbType type)
        {
            string res = string.Empty;
            if (type == OracleDbType.Date)
            {
                res = string.Format("TO_DATE('{0}','yyyy-MM-dd')", value);
                //res = value.Replace("-", "/");
            }
            else res = value;

            return res;
        }

        public static string ReplaceSPQueryBySQLForAthena(string tableName, string in_criteria)
        {
            var sql = string.Format("SELECT * FROM {0}_V WHERE 1 = 1 ", tableName.ToUpper());

            if (tableName.Trim().ToUpper() == "TESTHEADER")
            {
                if (string.IsNullOrEmpty(in_criteria) == false)
                {
                    var hasProductFamilyName = in_criteria.ToUpper().IndexOf("PRODUCTFAMILYNAME", 1) > 0;
                    var hasProductFamilyId = in_criteria.ToUpper().IndexOf("PRODUCTFAMILYID", 1) > 0;
                    //Replace "and" to special char then count the array
                    var noOtherCondition = in_criteria.ToUpper().Replace("AND ", "@").Split('@').Count() == 2;                    
                    if ((hasProductFamilyName || hasProductFamilyId) && noOtherCondition)
                    {
                        sql = sql + in_criteria + " order by testheaderid desc LIMIT 50";
                    }
                    else
                    {
                        sql = sql + in_criteria + " order by testheaderid desc";
                    }
                }
                else
                {
                    sql = sql + " order by testheaderid desc LIMIT 50";
                }                
            }
            else
            {
                if (string.IsNullOrEmpty(in_criteria) == false)
                {
                    sql = sql + in_criteria;
                }
            }

            return sql;
        }
    }
}