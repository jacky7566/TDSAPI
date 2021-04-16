using Dapper;
using Jose;
using KaizenTDSMvcAPI.Utils;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace KaizenTDSMvcAPI.Filters
{
    public class JwtAuthFilter : ActionFilterAttribute
    {
        public static string TokenEnabled = ConfigurationManager.AppSettings["EnableToken"] == null ? "N" : ConfigurationManager.AppSettings["EnableToken"].ToString();
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            string secret = ConfigurationManager.AppSettings["SercurityKey"].ToString();//加解密的key,如果不一樣會無法成功解密
            var request = actionContext.Request;
            var queryStrToken = request.GetQueryNameValuePairs().Where(r => r.Key.ToUpper().Equals("TOKEN"));
            if (!WithoutVerifyToken(request.RequestUri.ToString()))
            {
                if ((request.Headers.Authorization == null || request.Headers.Authorization.Scheme != "Bearer") && queryStrToken.Count() == 0)
                {
                    actionContext.Response = actionContext.Request.CreateResponse(
                        HttpStatusCode.BadRequest,
                        new { ErrorMessage = "Lost Token" },
                        actionContext.ControllerContext.Configuration.Formatters.JsonFormatter
                    );
                    return;
                }
                else
                {
                    string token = string.Empty;
                    if (request.Headers.Authorization != null && request.Headers.Authorization.Scheme == "Bearer") token = request.Headers.Authorization.Parameter;
                    else if (string.IsNullOrEmpty(token) && request.Method == HttpMethod.Get) token = queryStrToken.FirstOrDefault().Value;
                    else
                    {
                        actionContext.Response = actionContext.Request.CreateResponse(
                            HttpStatusCode.BadRequest,
                            new { ErrorMessage = "Lost Token" },
                            actionContext.ControllerContext.Configuration.Formatters.JsonFormatter
                        );
                        return;
                    }
                    try
                    {
                        //解密後會回傳Json格式的物件(即加密前的資料)
                        var jwtObject = Jose.JWT.Decode<Dictionary<string, Object>>(token, Encoding.UTF8.GetBytes(secret), JwsAlgorithm.HS512);                        

                        var userName = jwtObject["UserName"].ToString();
                        if (ValidateTokenFromDB(token, userName) == false)
                        {
                            actionContext.Response = actionContext.Request.CreateResponse(
                                HttpStatusCode.Conflict,
                                new { ErrorMessage = "Token Not Available" },
                                actionContext.ControllerContext.Configuration.Formatters.JsonFormatter
                            );
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        actionContext.Response = actionContext.Request.CreateResponse(
                            HttpStatusCode.ExpectationFailed,
                            new { ErrorMessage = ex.Message },
                            actionContext.ControllerContext.Configuration.Formatters.JsonFormatter
                        );
                        return;
                    }

                }
            }

            base.OnActionExecuting(actionContext);
        }

        /// <summary>
        /// Login不需要驗證因為還沒有token
        /// </summary>
        /// <param name="requestUri"></param>
        /// <returns></returns>
        public bool WithoutVerifyToken(string requestUri)
        {
            if (TokenEnabled == "N") return true; //Always returns ture if is disable
            
            if (requestUri.ToUpper().EndsWith("/API/TOKEN") || requestUri.ToUpper().EndsWith("API/ACCOUNT") 
                || requestUri.ToUpper().EndsWith("/API/GENERIC/1.0/PRODUCTFAMILY") || requestUri.ToUpper().EndsWith("/API/GENERIC/1.0/ROLES")
                || requestUri.ToUpper().Contains("/API/GENERIC/1.0/ACCESSREQUEST")
                || requestUri.ToUpper().Contains("/API/ACCOUNT/VALIDATEEMPLOYEEID")
                || requestUri.ToUpper().Contains("/API/ACCOUNT/ACCOUNTREQUESTAPPROVE")
                || requestUri.ToUpper().Contains("/API/KAIZENTDS/TESTFILEDOWNLOAD"))
                return true;
            return false;
        }

        /// <summary>
        /// 驗證token時效
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public bool IsTokenExpired(string dateTime)
        {
            return Convert.ToDateTime(dateTime) < DateTime.Now;
        }
        
        /// <summary>
        /// 驗證資料庫裡的token是否符合
        /// </summary>
        /// <param name="token"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public bool ValidateTokenFromDB(string token, string userName)
        {
            try
            {
                ConnectionHelper connectionHelper = new ConnectionHelper(string.Empty);
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                {
                    string sql = string.Format("SELECT USERID FROM TDSUSER WHERE APIKEY = '{0}' AND UPPER(EMPLOYEEID) = '{1}' ", token, userName.ToUpper());
                    var res = ConnectionHelper.QueryDataBySQL(sql, false);
                    if (res.Count() > 0) return true;
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