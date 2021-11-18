using Dapper;
using KaizenTDSMvcAPI.Models;
using KaizenTDSMvcAPI.Models.KaizenTDSClasses;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Xml.Serialization;
using SystemLibrary.Utility;
using static OracleDynamicParameters;

namespace KaizenTDSMvcAPI.Utils
{
    public static class ExtensionHelper
    {
        public static HttpResponseMessage LogAndResponse(HttpContent hc, HttpStatusCode hsc = HttpStatusCode.OK,
            string message = "", Exception ex = null)
        {
            var resp = new HttpResponseMessage(hsc);
            if (string.IsNullOrEmpty(message) == false)
            {
                if (hsc != HttpStatusCode.OK)
                    LogHelper.WriteLine(message);
                resp.Content = new StringContent(message, Encoding.UTF8, "text/html");
            }
            else
            {
                resp.Content = hc;
            }
                
            return resp;
        }

        public static string GetAllFootprints(Exception x)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex">Exception object</param>
        /// <param name="message">Message</param>
        /// <param name="hsc">HttpStatusCode</param>
        /// <param name="function">SP Function</param>
        /// <param name="logTypeId">1:Information only, 2 Warning, 3 Error</param>
        /// /// <param name="textInput">For log the text input</param>
        /// <param name="input"></param>
        public static void LogExpSPMessageToDB(Exception ex, string message, HttpStatusCode hsc, 
            string function, int logTypeId, string textInput, List<QueryParamClass> input = null)
        {
            //For Distinct Basic columns
            List<string> basicColsList = new List<string>() { "IN_FUNCTIONMETHOD", "IN_LOGDATE", "IN_LOGTYPEID", "IN_ALERTSENTDATE", "IN_USERID", "IN_LOGID", "IN_MESSAGE", "IN_MESSAGEMISC" };
            var msgMiscStr = input == null ? textInput : JsonConvert.SerializeObject(input);
            //Log to Centrallized Database
            //var defaultConnStr = ConfigurationManager.AppSettings["MESDEV_TDSMFG"];

            //Inhirit from original Json
            var arguments = LookupHelper.GetSPArguments("LOG_INS", "PKG_SPECIALFUNCTION", ConnectionHelper.ConnectionInfo.DEFAULTCONNECTIONSTRING);
            List<QueryParamClass> logInput = new List<QueryParamClass>();
            //UserId
            var userId = GetTDSUserID("CIMSQL"); //Get one default user
            foreach (var arg in arguments)
            {

                var inputItem = input != null ? input.Find(r => r.Name.ToUpper().Equals(arg.ARGUMENT_NAME)) : null;
                if (inputItem != null)
                {
                    if (arg.ARGUMENT_NAME == "IN_EMPLOYEEID")
                    {
                        userId = GetTDSUserID(inputItem.Value.ToString());
                    }
                    logInput.Add(inputItem);
                }                    
                else
                {                    
                    if (basicColsList.Contains(arg.ARGUMENT_NAME))
                        continue;
                    logInput.Add(new QueryParamClass()
                    {
                        Name = arg.ARGUMENT_NAME,
                        Value = arg.ARGUMENT_NAME == "IN_EMPLOYEEID" ? "CIMSQL" : "SysExcep" 
                    });
                }
            }
            var inputDps = ConnectionHelper.CreateODPEasyInput(logInput, arguments);
           
            //Basic Log Info
            if (ex != null)
            {
                var st = new StackTrace(ex, true);
                inputDps.Add("IN_FUNCTIONMETHOD",
                    string.Format("{0}.{1} - System Name: {2}", function, st.GetFrames().FirstOrDefault().GetMethod().Name, ConnectionHelper.ConnectionInfo.APICONNECTIONNAME),
                    OracleDbType.Varchar2, System.Data.ParameterDirection.Input);
            }
            else
            {
                inputDps.Add("IN_FUNCTIONMETHOD",
                    string.Format("{0} - System Name: {1}", function, ConnectionHelper.ConnectionInfo.APICONNECTIONNAME),
                    OracleDbType.Varchar2, System.Data.ParameterDirection.Input);
            }

            inputDps.Add("IN_LOGDATE", DateTime.Now, OracleDbType.Date, System.Data.ParameterDirection.Input);
            inputDps.Add("IN_LOGTYPEID", logTypeId, OracleDbType.Int32, System.Data.ParameterDirection.Input);

            inputDps.Add("IN_ALERTSENTDATE", DBNull.Value, OracleDbType.Date, System.Data.ParameterDirection.Input);
            inputDps.Add("IN_USERID", userId, OracleDbType.Int32, System.Data.ParameterDirection.Input);
            inputDps.Add("IN_LOGID", DBNull.Value, OracleDbType.Int32, System.Data.ParameterDirection.InputOutput);            
            inputDps.Add("IN_MESSAGEMISC", msgMiscStr,
                OracleDbType.Clob, System.Data.ParameterDirection.Input);
            inputDps.Add("IN_MESSAGE", message, OracleDbType.Clob, System.Data.ParameterDirection.Input);

            using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DEFAULTCONNECTIONSTRING))
            {
                try
                {
                    sqlConn.Execute("PKG_SPECIALFUNCTION.LOG_INS", inputDps, commandType: System.Data.CommandType.StoredProcedure);
                    var out_is_success = inputDps.Get<OracleDecimal>("OUT_IS_SUCCESS").ToInt32();
                    var out_message = inputDps.Get<OracleString>("OUT_MESSAGE").ToString();
                    if (out_is_success == 1)
                    {
                        if (string.IsNullOrEmpty(out_message))
                            out_message = "Excute success!";
                    }
                    else
                        out_message = inputDps.Get<OracleString>("OUT_MESSAGE").ToString();

                    LogHelper.WriteLine(string.Format("LogTypeId: {0}, Message: {1}, Function: {2}, Out_is_success: {3}, Out_message: {4}", logTypeId, message, function, out_is_success, out_message));
                }
                catch (Exception e)
                {
                    LogHelper.WriteLine(string.Format("Log Exception error: {0}", e.StackTrace));
                    //throw e;
                }
            }

        }

        public static void LogMessageToDB(Exception ex, string message, HttpStatusCode hsc,
            string function, int logTypeId, object input)
        {
            //For Distinct Basic columns
            List<string> basicColsList = new List<string>() { "IN_FUNCTIONMETHOD", "IN_LOGDATE", "IN_LOGTYPEID", "IN_ALERTSENTDATE", "IN_USERID", "IN_LOGID", "IN_MESSAGE", "IN_MESSAGEMISC" };
            var msgMiscStr = input == null ? string.Empty : JsonConvert.SerializeObject(input);

            try
            {
                //Inhirit from original Json
                var arguments = LookupHelper.GetSPArguments("LOG_INS", "PKG_GENERIC");
                List<QueryParamClass> logInput = new List<QueryParamClass>();
                if (input != null)
                {
                    foreach (var arg in arguments)
                    {
                        logInput.Add(new QueryParamClass()
                        {
                            Name = arg.ARGUMENT_NAME,
                            Value = arg.ARGUMENT_NAME == "IN_EMPLOYEEID" ? "CIMSQL" : "NA" 
                        });
                    }
                }
                var inputDps = ConnectionHelper.CreateODPEasyInput(logInput, arguments);

                //Basic Log Info
                var st = new StackTrace(ex, true);
                inputDps.Add("IN_FUNCTIONMETHOD",
                    string.Format("{0}:{1}", function, st.GetFrames().FirstOrDefault().GetMethod().Name),
                    OracleDbType.Varchar2, System.Data.ParameterDirection.Input);
                inputDps.Add("IN_LOGDATE", DateTime.Now, OracleDbType.Date, System.Data.ParameterDirection.Input);
                inputDps.Add("IN_LOGTYPEID", logTypeId, OracleDbType.Int32, System.Data.ParameterDirection.Input);

                inputDps.Add("IN_ALERTSENTDATE", DBNull.Value, OracleDbType.Date, System.Data.ParameterDirection.Input);
                inputDps.Add("IN_USERID", 2, OracleDbType.Int32, System.Data.ParameterDirection.Input);
                inputDps.Add("IN_LOGID", DBNull.Value, OracleDbType.Int32, System.Data.ParameterDirection.InputOutput);
                inputDps.Add("IN_MESSAGEMISC", msgMiscStr,
                    OracleDbType.Clob, System.Data.ParameterDirection.Input);
                inputDps.Add("IN_MESSAGE", message, OracleDbType.Clob, System.Data.ParameterDirection.Input);

                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                {

                    sqlConn.Execute("PKG_GENERIC.LOG_INS", inputDps, commandType: System.Data.CommandType.StoredProcedure);
                    var out_is_success = inputDps.Get<OracleDecimal>("OUT_IS_SUCCESS").ToInt32();
                    var out_message = inputDps.Get<OracleString>("OUT_MESSAGE").ToString();
                    if (out_is_success == 1)
                    {
                        if (string.IsNullOrEmpty(out_message))
                            out_message = "Excute success!";
                    }
                    else
                        out_message = inputDps.Get<OracleString>("OUT_MESSAGE").ToString();

                    LogHelper.WriteLine(string.Format("LogTypeId: {0}, Message: {1}, Function: {2}, Out_is_success: {3}, Out_message: {4}", logTypeId, message, function, out_is_success, out_message));

                }
            }
            catch (Exception e)
            {
                LogHelper.WriteLine(string.Format("Log Exception error: {0}", e.StackTrace));
                throw e;
            }           
        }

        public static OracleClob StringToClob(string input, OracleConnection conn)
        {
            if (conn.State != System.Data.ConnectionState.Open)
                conn.Open();

            byte[] newvalue = System.Text.Encoding.Unicode.GetBytes(input);
            var clob = new OracleClob(conn);
            clob.Write(newvalue, 0, newvalue.Length);
            return clob;
        }

        public static int GetTDSUserID(string empId)
        {
            var userId = 0;
            try
            {
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DEFAULTCONNECTIONSTRING))
                {
                    var sql = string.Format("SELECT USERID FROM TDSUSER WHERE UPPER(EMPLOYEEID) = '{0}'", empId.ToUpper());
                    var res = sqlConn.Query<string>(sql);
                    if (res.Count() > 0) userId = int.Parse(res.FirstOrDefault());
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return userId;
        }
        public static bool ListsContainAMatchingValue(IEnumerable<string> listA, IEnumerable<string> listB)
        {
            //return listA.Except(listB).Any() == false;
            foreach (var item in listB)
            {
                if (listA.Contains(item) == false) return false;
            }
            return true;
        }

        public static List<dynamic> ToDynamicList(this DataTable table, bool reverse = true, params string[] FilterField)
        {
            var modelList = new List<dynamic>();
            foreach (DataRow row in table.Rows)
            {
                dynamic model = new ExpandoObject();
                var dict = (IDictionary<string, object>)model;
                foreach (DataColumn column in table.Columns)
                {
                    if (FilterField.Length != 0)
                    {
                        if (reverse == true)
                        {
                            if (!FilterField.Contains(column.ColumnName))
                            {
                                dict[column.ColumnName] = row[column];
                            }
                        }
                        else
                        {
                            if (FilterField.Contains(column.ColumnName))
                            {
                                dict[column.ColumnName] = row[column];
                            }
                        }
                    }
                    else
                    {
                        dict[column.ColumnName] = row[column];
                    }
                }
                modelList.Add(model);
            }
            return modelList;
        }

        /// <summary>
        /// Calculate Directry Size
        /// </summary>
        /// <param name="d">DirectoryInfo</param>
        /// <returns></returns>
        public static long DirSize(DirectoryInfo d)
        {
            long size = 0;
            // Add file sizes.
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis)
            {
                size += fi.Length;
            }
            // Add subdirectory sizes.
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                size += DirSize(di);
            }
            return size;
        }

        public static bool LogDLLVersion(string uIVersion, string apiVersion, string ingestionDllVer, string dataUploadDllVer, string baseURL)
        {
            try
            {
                var serverName = System.Environment.MachineName;
                var systemName = ConfigurationManager.AppSettings.AllKeys.Where(r => r == "SystemName").Count() > 0 ? ConfigurationManager.AppSettings["SystemName"].ToString() : string.Empty;

                var sql = string.Format("SELECT * FROM SYSTEMVERSION WHERE SYSTEMNAME = '{0}' AND SERVERNAME = '{1}' ", systemName, serverName);

                using (var sqlConn = new OracleConnection(ConfigurationManager.AppSettings["MESDEV_TDSMFG"].ToString()))
                {
                    var list = sqlConn.Query<SystemVersionClass>(sql).ToList();

                    if (list.Count() > 0)
                    {
                        var svId = list.FirstOrDefault().SYSTEMVERSIONID;
                        if (uIVersion.Equals(list.FirstOrDefault().UIVERSION) == false ||
                            apiVersion.Equals(list.FirstOrDefault().APIVERSION) == false ||
                            ingestionDllVer.Equals(list.FirstOrDefault().INGESTIONDLLVERSION) == false ||
                            dataUploadDllVer.Equals(list.FirstOrDefault().DATAUPLOADERVERSION) == false)
                        {
                            sql = string.Format(@"INSERT INTO TDSMFGHIST.SYSTEMVERSION 
                                    (SYSTEMVERSIONID, SYSTEMNAME, SERVERNAME, SYSTEMURL, UIVERSION, APIVERSION, INGESTIONDLLVERSION, DATAUPLOADERVERSION, CREATEDBY, CREATEDDATE, LASTMODIFIEDBY, LASTMODIFIEDDATE, TRANSACTIONDATE, TRANSACTIONBY, TRANSACTIONTYPE)
                                    SELECT S.*, SYSDATE TRANSACTIONDATE, 'SYS' TRANSACTIONBY, 'UPDATE' TRANSACTIONTYPE FROM SYSTEMVERSION S
                                    WHERE SYSTEMVERSIONID = {0} ", svId);

                            sqlConn.Execute(sql);
                            LogHelper.WriteLine(string.Format("Insert DLL Version History => SQL: {0}", sql));

                            sql = string.Format("UPDATE SYSTEMVERSION SET APIVERSION = '{0}', INGESTIONDLLVERSION = '{1}', DATAUPLOADERVERSION = '{2}', UIVERSION = '{3}', LASTMODIFIEDDATE = SYSDATE  WHERE SYSTEMVERSIONID = {4} ", apiVersion, ingestionDllVer, dataUploadDllVer, uIVersion, svId);
                            LogHelper.WriteLine(string.Format("Update DLL Version => SQL: {0}", sql));
                        }
                        else
                        {
                            LogHelper.WriteLine(string.Format("No need to update version => UI Version: {0}, API Version: {1}, Ingestion DLL Version: {2}, Data Uploader DLL Version: {3}", uIVersion, apiVersion, ingestionDllVer, dataUploadDllVer));
                        }
                    }
                    else
                    {
                        sql = string.Format("INSERT INTO SYSTEMVERSION (SYSTEMVERSIONID, SYSTEMNAME, SERVERNAME, SYSTEMURL, UIVERSION, APIVERSION, INGESTIONDLLVERSION, DATAUPLOADERVERSION, CREATEDBY, CREATEDDATE, LASTMODIFIEDBY, LASTMODIFIEDDATE) VALUES (SEQ_SYSTEMVERSION.NEXTVAL, '{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', 'SYS', SYSDATE, 'SYS', SYSDATE) ", systemName, System.Environment.MachineName, baseURL, uIVersion, apiVersion, ingestionDllVer, dataUploadDllVer);

                        LogHelper.WriteLine(string.Format("Insert DLL Version => SQL: {0}", sql));
                    }
                    return sqlConn.Execute(sql) > 0 ? true : false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static string GetUIVersion()
        {
            var configFilePath = ConfigurationManager.AppSettings["KaizenTDSUIConfigFile"].ToString();
            if (File.Exists(configFilePath))
            {
                string[] lines = File.ReadAllLines(configFilePath);
                var versionLine = lines.Where(r => r.StartsWith("Version"));
                if (versionLine.Count() > 0)
                {
                    return versionLine.FirstOrDefault().Split(':').LastOrDefault().TrimStart();
                }
            }
            return "1.0.0";
        }
    }
}