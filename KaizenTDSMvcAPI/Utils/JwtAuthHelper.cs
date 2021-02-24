using Dapper;
using Jose;
using KaizenTDSMvcAPI.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using Oracle.ManagedDataAccess.Client;
using System.Linq;
using System.Text;
using System.Web;

namespace KaizenTDSMvcAPI.Utils
{
    public class JwtAuthHelper
    {
        /// <summary>
        /// Expired Time (Second)
        /// </summary>
        public static string expSec = ConfigurationManager.AppSettings["TokExpSec"].ToString();
        /// <summary>
        /// Generate Token by using UserName
        /// </summary>
        /// <param name="userName">xxx12345</param>
        /// <returns></returns>
        public bool GenerateToken(string userName)
        {
            string secret = ConfigurationManager.AppSettings["SercurityKey"].ToString();//加解密的key,如果不一樣會無法成功解密
            Dictionary<string, Object> claim = new Dictionary<string, Object>();//payload 需透過token傳遞的資料
            claim.Add("UserName", userName);
            //claim.Add("Password", userInfo.Password);
            claim.Add("GUID", Guid.NewGuid().ToString());
            //claim.Add("Exp", DateTime.Now.AddSeconds(Convert.ToInt32(expSec)).ToString());//Token 時效設定100秒
            var payload = claim;
            var token = Jose.JWT.Encode(payload, Encoding.UTF8.GetBytes(secret), JwsAlgorithm.HS512);//產生token
            if (string.IsNullOrEmpty(token) == false)
            {
                return UpdateAPIKeyToTDSUser(userName, token);
            }
            
            return false;
        }

        /// <summary>
        /// Update Token By UserName
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="token"></param>
        public static bool UpdateAPIKeyToTDSUser(string userName, string token)
        {
            try
            {
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DEFAULTCONNECTIONSTRING))
                {
                    string sql = string.Format("UPDATE TDSUSER SET APIKEY = '{0}', LASTMODIFIEDDATE = SYSDATE WHERE UPPER(EMPLOYEEID) = '{1}' ", token, userName.ToUpper());
                    if (sqlConn.Execute(sql) > 0) return true;
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