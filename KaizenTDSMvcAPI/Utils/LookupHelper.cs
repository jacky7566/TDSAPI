using Dapper;
using KaizenTDSMvcAPI.Models;
using KaizenTDSMvcAPI.Models.APIConnectionClasses;
using KaizenTDSMvcAPI.Models.KaizenTDSClasses;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using SystemLibrary.Utility;

namespace KaizenTDSMvcAPI.Utils
{
    public class LookupHelper
    {
        public static List<TABLE_LOOKUP> GetTableStoredProcMap(string APILookupName, string action, string version)
        {
            List<TABLE_LOOKUP> result = new List<TABLE_LOOKUP>();
            var sql = string.Format(@"SELECT * FROM TABLE_LOOKUP WHERE (UPPER(TABLE_NAME) = '{0}' OR UPPER(ATTRIBUTE) = '{0}') AND KEY = '{1}' AND VERSION = '{2}'  ",
                APILookupName.Trim().ToUpper(), action.Trim().ToUpper(), version.Trim());
            try
            {
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                {
                    result = sqlConn.Query<TABLE_LOOKUP>(sql).ToList();
                }
            }
            catch (Exception ex)
            {
                ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, APILookupName + "_" + action, 2, sql, null);
                //Always return null
            }

            return result;
        }

        public static List<APILookupClass> GetAPILookupList(string lookupName, string action, string version, string cmdType)
        {
            List<APILookupClass> list = new List<APILookupClass>();
            var sql = string.Format(@"SELECT * FROM APILOOKUP WHERE (UPPER(APILOOKUPNAME) = '{0}' OR UPPER(BASETAG) = '{0}') AND COMMANDOPERATION = '{1}' AND VERSION = '{2}' AND APICOMMANDTYPE = '{3}'  ",
                        lookupName.Trim().ToUpper(), action.Trim().ToUpper(), version.Trim(), cmdType.Trim().ToUpper());
            try
            {
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DEFAULTCONNECTIONSTRING))
                {                    
                    list = sqlConn.Query<APILookupClass>(sql).ToList();
                }
            }
            catch (Exception ex)
            {
                //ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, lookupName + "_" + action, 2, sql, null);
                LogHelper.WriteLine(ExtensionHelper.GetAllFootprints(ex));
                return null;
                //Always returns null
            }

            return list;
        }

        /// <summary>
        /// Get Stored Procedure Arguments
        /// </summary>
        /// <param name="sPName">Stored Procedure Name</param>
        /// <param name="pkgName">Pacakge Name</param>
        /// <returns></returns>
        public static List<SpArgumentsClass> GetSPArguments(string sPName, string pkgName)
        {
            return GetSPArguments(sPName, pkgName, ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sPName"></param>
        /// <param name="pkgName"></param>
        /// <param name="connStr"></param>
        /// <returns></returns>
        public static List<SpArgumentsClass> GetSPArguments(string sPName, string pkgName, string connStr)
        {
            List<SpArgumentsClass> list = new List<SpArgumentsClass>();
            using (var sqlConn = new OracleConnection(connStr))
            {
                string sql = string.Format(@"SELECT * FROM SYS.ALL_ARGUMENTS WHERE OBJECT_NAME = '{0}' AND PACKAGE_NAME = '{1}' ",
                    sPName.ToUpper(), pkgName.ToUpper());
                list = sqlConn.Query<SpArgumentsClass>(sql).ToList();
            }
            return list;
        }

        public static string GetConfigValueByName(string configName)
        {
            string configValue = string.Empty;
            var sql = string.Format("select configvalue from config where configname = '{0}' ", configName);
            var res = new List<string>();

            using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
            {
                res = sqlConn.Query<string>(sql).ToList();
            }

            try
            {
                if (res != null && res.Count() > 0)
                {
                    configValue = res.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLine(ex.ToString());
                throw ex;
            }
            return configValue;
        }
    }
}