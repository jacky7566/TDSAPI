using Dapper;
using KaizenTDSMvcAPI.Models;
using KaizenTDSMvcAPI.Models.APIConnectionClasses;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SystemLibrary.Utility;

namespace KaizenTDSMvcAPI.Utils
{
    public class ConnectionHelper
    {
        public static ConnectionClass ConnectionInfo;        
        //public static string.ConnectionInfo.DATABASECONNECTIONSTRING;
        //public static string SystemName;
        public ConnectionHelper(string sysKey)
        {
            ConnectionInfo = GetConnectionInfo(sysKey);
            if (ConnectionInfo == null)
            {
                ConnectionInfo = new ConnectionClass();
                var conKey = ConfigurationManager.AppSettings["DEFAULT"];

                if (string.IsNullOrEmpty(sysKey) == false && ConfigurationManager.AppSettings.AllKeys.Contains(sysKey))
                {
                    conKey = ConfigurationManager.AppSettings[sysKey];
                }
                //Keep Default Settings to get from web.config
                ConnectionInfo.APICONNECTIONNAME = conKey;
                ConnectionInfo.DATABASECONNECTIONSTRING = ConfigurationManager.AppSettings[conKey];
                ConnectionInfo.DEFAULTCONNECTIONSTRING = ConfigurationManager.AppSettings[ConfigurationManager.AppSettings["DEFAULT"]];
            }   
        }

        private static ConnectionClass GetConnectionInfo(string conName)
        {
            var defaultConnStr = ConfigurationManager.AppSettings[ConfigurationManager.AppSettings["DEFAULT"]];
            if (string.IsNullOrEmpty(conName))
            {
                conName = ConfigurationManager.AppSettings["DEFAULT"];           
            }

            List<ConnectionClass> list = new List<ConnectionClass>();
            //Fast way to set default info into
            string sql = string.Format(@"SELECT APICONNECTIONNAME, DATABASECONNECTIONSTRING, '{0}' DEFAULTCONNECTIONSTRING FROM APICONNECTION_V WHERE APICONNECTIONNAME = '{1}' ", defaultConnStr, conName);
            try
            {
                using (var sqlConn = new OracleConnection(defaultConnStr))
                {
                    list = sqlConn.Query<ConnectionClass>(sql).ToList();
                }
                if (list != null && list.Count() > 0) return list.FirstOrDefault();
            }
            catch (Exception ex)
            {
                return new ConnectionClass()
                {
                    APICONNECTIONNAME = conName,
                    DATABASECONNECTIONSTRING = defaultConnStr,
                    DEFAULTCONNECTIONSTRING = defaultConnStr
                };
            }

            return null;
        }
        
        public static DynamicParameters CreateDPInstance(List<QueryParamClass> input)
        {
            DynamicParameters odps = new DynamicParameters();
            foreach (var paramItem in input)
            {
                if (paramItem.DbType == null)
                {
                    odps.Add(paramItem.Name, paramItem.Value);
                }
                else
                {
                    if (paramItem.Size == 0)
                    {

                        odps.Add(paramItem.Name, paramItem.Value, paramItem.DbType.Value, paramItem.ParameterDirection);
                    }
                    else
                    {
                        odps.Add(paramItem.Name, paramItem.Value, paramItem.DbType.Value, paramItem.ParameterDirection, paramItem.Size);
                    }
                }
            }
            return odps;
        }

        public static OracleDynamicParameters CreateODPInstance(List<QueryParamClass> input)
        {
            OracleDynamicParameters odps = new OracleDynamicParameters();
            foreach (var paramItem in input)
            {
                if (paramItem.OracleDbType == null)
                {
                    odps.Add(paramItem.Name, paramItem.Value);
                }
                else if (paramItem.OracleDbType == Oracle.ManagedDataAccess.Client.OracleDbType.Date)
                {
                    var dt = DateTime.Parse(paramItem.Value.ToString());
                    odps.Add(paramItem.Name, dt, paramItem.OracleDbType.Value, paramItem.ParameterDirection);
                }
                else
                {
                    if (paramItem.Size == 0)
                    {
                        odps.Add(paramItem.Name, paramItem.Value, paramItem.OracleDbType.Value, paramItem.ParameterDirection);
                    }
                    else
                    {
                        odps.Add(paramItem.Name, paramItem.Value, paramItem.OracleDbType.Value, paramItem.ParameterDirection, paramItem.Size);
                    }
                }
            }
            return odps;
        }

        /// <summary>
        /// Create Oracle Dynamic Parameters Easy Inputs function
        /// </summary>
        /// <param name="clientInput">Client input</param>
        /// <param name="argList"></param>
        /// <returns></returns>
        public static OracleDynamicParameters CreateODPEasyInput(List<QueryParamClass> clientInput, List<SpArgumentsClass> argList)
        {
            OracleDynamicParameters odps = new OracleDynamicParameters();
            object value = null;
            foreach (var paramItem in clientInput)
            {                                
                var argItem = argList.Where(r => r.ARGUMENT_NAME.Equals(paramItem.Name.ToUpper())).FirstOrDefault();
                if (argItem != null)
                {
                    if (argItem.DATA_TYPE.Equals("DATE") && paramItem.Value != null && paramItem.Value.ToString() != "NA")
                        value = DateTime.Parse(paramItem.Value.ToString());                    
                    else
                        value = paramItem.Value;
                    //var value = argItem.DATA_TYPE.Equals("DATE") ? DateTime.Parse(paramItem.Value.ToString()) : paramItem.Value;
                    if (argItem.DATA_LENGTH > 0)
                    {       
                        odps.Add(paramItem.Name.ToUpper(), value,
                            StoredProcedureHelper.OracleDbTypeParser(argItem.DATA_TYPE),
                            StoredProcedureHelper.GetParameterDirection(argItem.IN_OUT),
                            argItem.DATA_LENGTH);
                    }
                    else
                    {
                        var oType = StoredProcedureHelper.OracleDbTypeParser(argItem.DATA_TYPE);
                        if (oType == OracleDbType.Blob)
                        {
                            byte[] blobValue = value == null ? null : System.Convert.FromBase64String(value.ToString());
                            odps.Add(paramItem.Name.ToUpper(), blobValue, oType,
                                StoredProcedureHelper.GetParameterDirection(argItem.IN_OUT), blobValue != null ? blobValue.Length : 0);
                        }
                        else
                        {
                            odps.Add(paramItem.Name.ToUpper(), value, oType,
                                StoredProcedureHelper.GetParameterDirection(argItem.IN_OUT));
                        }

                    }
                }
                else
                {
                    odps.Add(paramItem.Name, paramItem.Value);
                }
            }
            //Check if not exist OUT setting
            if (clientInput.Where(r => r.Name.ToUpper().StartsWith("OUT")).Count() <= 0)
            {
                foreach (var argItem in argList.Where(r => r.IN_OUT.Equals("OUT")))
                {
                    if (argItem.DATA_LENGTH > 0)
                    {
                        odps.Add(argItem.ARGUMENT_NAME.ToUpper(), DBNull.Value,
                            StoredProcedureHelper.OracleDbTypeParser(argItem.DATA_TYPE),
                            StoredProcedureHelper.GetParameterDirection(argItem.IN_OUT),
                            argItem.DATA_LENGTH);
                    }
                    else
                    {
                        odps.Add(argItem.ARGUMENT_NAME.ToUpper(), DBNull.Value,
                            StoredProcedureHelper.OracleDbTypeParser(argItem.DATA_TYPE),
                            StoredProcedureHelper.GetParameterDirection(argItem.IN_OUT));
                    }

                }
            }

            return odps;            
        }

        public static DynamicParameters AddDefaultApiDps(DynamicParameters dps, string apiName)
        {
            dps.Add("in_api_name", apiName);
            dps.Add("in_api_version", typeof(Controller).Assembly.GetName().Version.ToString());

            return dps;
        }

        public static OracleDynamicParameters AddDefaultApiODps(OracleDynamicParameters dps, string apiName)
        {
            dps.Add("in_apiname", apiName);
            dps.Add("in_apiversion", typeof(Controller).Assembly.GetName().Version.ToString());

            return dps;
        }

        public static List<QueryParamClass> AddDefaultApi2Input(List<QueryParamClass> input, string apiName)
        {
            var assemblyInfo = System.Reflection.Assembly.GetExecutingAssembly().GetName();
            //var apiVer = string.Format("{0}_{1}", assemblyInfo.Name, assemblyInfo.Version);

            input.Add(new QueryParamClass()
            {
                Name = "in_apiname",
                Value = apiName
            });
            input.Add(new QueryParamClass()
            {
                Name = "in_apiversion",
                Value = assemblyInfo.Version.ToString()
            });

            return input;
        }

        /// <summary>
        /// Query Data By SQL
        /// </summary>
        /// <param name="sql">input sql</param>
        /// <param name="isCheckAthena">is need to check Athena or not</param>
        /// <param name="ingoreOracleQuery">Ignore Oracle Query</param>
        /// <returns></returns>
        public static List<dynamic> QueryDataBySQL(string sql, bool isCheckAthena)
        {
            List<dynamic> list = new List<dynamic>();
            try
            {                
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                {
                    //If Force to check Athena, then ignore oracle query
                    if (isCheckAthena == false)
                    {
                        list = sqlConn.Query<dynamic>(sql).ToList();
                    }
                    else //20210407 Jacky Add Athena Query function
                    {
                        var athenaSchema = LookupHelper.GetConfigValueByName("KaizenTDSAthenaSchema").ToUpper();
                        if (string.IsNullOrEmpty(athenaSchema) == false)
                        {
                            LogHelper.WriteLine("Get Data From Athena schema: " + athenaSchema);
                            sql = AthenaSQLSchemaModification(sql, athenaSchema);
                            var athenaConn = LookupHelper.GetConfigValueByName("KaizenTDSAthenaConn");
                            using (var odbcConn = new OdbcConnection(athenaConn))
                            {
                                list = odbcConn.Query<dynamic>(sql).ToList();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            } 
            return list;
        }

        public OdbcConnection GetODBCDBConn(string connStr)
        {
            OdbcConnection odbcConn = new OdbcConnection(connStr);
            try
            {                
                if (odbcConn.State == ConnectionState.Closed)
                {
                    try
                    {
                        odbcConn.Open();
                    }
                    catch (Exception ex)
                    {
                        throw (ex);
                    }
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            return odbcConn;
        }

        /// <summary>
        /// AthenaSQLSchemaModification: To Modify extra Athena schema information befor "FROM" script
        /// </summary>
        /// <param name="sql">original sql</param>
        /// <param name="athenaSchema">athena schema name, can query from config table key = "KaizenTDSAthenaSchema"</param>
        /// <returns></returns>
        public static string AthenaSQLSchemaModification(string sql, string athenaSchema)
        {            
            sql = sql.ToUpper();
            if (string.IsNullOrEmpty(athenaSchema) == false && sql.Contains("FROM"))
            {                
                sql = sql.Replace(" FROM ", string.Format(" FROM {0}.", athenaSchema));
                sql = sql.Replace(" JOIN ", string.Format(" JOIN {0}.", athenaSchema));
                sql = sql.Replace(" INNER JOIN ", string.Format(" INNER JOIN {0}.", athenaSchema));
                sql = sql.Replace(" LEFT JOIN ", string.Format(" LEFT JOIN {0}.", athenaSchema));
            }

            return sql;
        }

        /// <summary>
        /// To check the view before query
        /// </summary>
        /// <param name="tableName">Equal to API input APILookupName</param>
        /// <returns></returns>
        public static bool CheckAthenaViewExistOrNot(string tableName)
        {
            try
            {
                var athenaSchema = LookupHelper.GetConfigValueByName("KaizenTDSAthenaSchema").ToUpper();
                if (string.IsNullOrEmpty(athenaSchema))
                    return false;
                var athenaConn = LookupHelper.GetConfigValueByName("KaizenTDSAthenaConn");

                string sql = string.Format(@"SELECT table_name FROM information_schema.tables
            WHERE upper(information_schema.tables.table_schema) = '{0}' and upper(table_name) = '{1}_V' ", athenaSchema, tableName.ToUpper());

                using (var odbcConn = new OdbcConnection(athenaConn))
                {
                    var list = odbcConn.Query<string>(sql).ToList();
                    if (list.Count() > 0)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return false;
        }
    }
}