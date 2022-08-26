using Dapper;
using KaizenTDSMvcAPI.Filters;
using KaizenTDSMvcAPI.Models;
using KaizenTDSMvcAPI.Models.KaizenTDSClasses;
using KaizenTDSMvcAPI.Utils;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;

namespace KaizenTDSMvcAPI.Controllers
{
    /// <summary>
    /// Account Get/Post API
    /// </summary>
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("api/Account")]
    public class AccountController : ApiController
    {
        /// <summary>
        /// Post Account Info to check AD and TDSUser is exist (System name is default setting)
        /// </summary>
        /// <param name="userInfo">UserName and Password Class</param>
        /// <returns></returns>
        [HttpPost]
        public HttpResponseMessage Post([FromBody] UserInfo userInfo)
        {
            return Post(userInfo, string.Empty);
        }

        /// <summary>
        /// Post Account Info by System name to check AD and TDSUser is exist
        /// </summary>
        /// <param name="userInfo">UserName and Password Class</param>
        /// <param name="apiConnName">System name, default is Kaizen TDS</param>
        /// <returns></returns>
        [HttpPost]
        [Route("{apiConnName}")]
        public HttpResponseMessage Post([FromBody] UserInfo userInfo, string apiConnName)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            ConnectionHelper connectionHelper = new ConnectionHelper(apiConnName);
            try
            {
                string decPwd = AccountHelper.base64Decode(userInfo.Password);
                if (AccountHelper.GetADSearchResult(userInfo.UserName, decPwd) != null)
                {
                    var list = AccountHelper.CheckAccountIsExists(userInfo.UserName);
                    if (list.Count() > 0)
                    {                                  
                        resp = ExtensionHelper.LogAndResponse(new ObjectContent<object>(list, new JsonMediaTypeFormatter()));
                    }
                    else
                    {
                        resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotFound,
                            string.Format("User account is not exist in database! UserName: {0}", userInfo.UserName));
                    }
                }
                else
                {
                    resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotFound,
                    string.Format("The user name or password is incorrect in AD! UserName: {0}", userInfo.UserName));
                }


            }
            catch (Exception ex)
            {
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.ExpectationFailed, ex.Message);
                ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, "Account", 2, string.Format("Get Account Info Error Error, User Name: {0}", userInfo.UserName), null);
            }

            return resp;
        }

        /// <summary>
        /// Validate User Account Info and Returns Token Info
        /// </summary>
        /// <param name="userName">UserName</param>
        /// <returns></returns>
        [HttpPost]
        [Route("UserTokenRefresh/{userName}")]
        public HttpResponseMessage UserTokenRefresh(string userName)
        {
            return UserTokenRefresh(userName, string.Empty);
        }

        /// <summary>
        /// Validate User Account Info and Returns Token Info (SystemName)
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="apiConnName"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("UserTokenRefresh/{userName}/{apiConnName}")]
        public HttpResponseMessage UserTokenRefresh(string userName, string apiConnName)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            ConnectionHelper connectionHelper = new ConnectionHelper(apiConnName);
            try
            {
                JwtAuthHelper jah = new JwtAuthHelper();
                if (jah.GenerateToken(userName) == false)
                {
                    resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotFound,
                        string.Format("Generate or Update new token failed! UserName: {0}", userName));
                }
                else
                {
                    var list = AccountHelper.CheckAccountIsExists(userName);
                    if (list.Count() > 0)
                    {
                        resp = ExtensionHelper.LogAndResponse(new ObjectContent<object>(list, new JsonMediaTypeFormatter()));
                    }
                    else
                    {
                        resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotFound,
                            string.Format("User account is not exist in database! UserName: {0}", userName));
                    }
                }
            }
            catch (Exception ex)
            {
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.ExpectationFailed, ex.Message);
                ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, "Account - UserTokenRefresh", 2, "UserTokenRefresh Error", null);
            }

            return resp;
        }

        /// <summary>
        /// Generate or Refresh all token in once
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("GenerateAllUsersToken")]
        public HttpResponseMessage GenerateAllUsersToken()
        {
            return GenerateAllUsersToken(string.Empty);
        }

        /// <summary>
        /// Generate or Refresh all token in once
        /// </summary>
        /// <param name="apiConnName"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("GenerateAllUsersToken/{apiConnName}")]
        public HttpResponseMessage GenerateAllUsersToken(string apiConnName)
        {
            StringBuilder errStr = new StringBuilder();
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            ConnectionHelper connectionHelper = new ConnectionHelper(apiConnName);

            try
            {
                string sql = "SELECT EMPLOYEEID FROM TDSUSER WHERE EMPLOYEEID IS NOT NULL";
                List<string> list = new List<string>();
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                {
                    list = sqlConn.Query<string>(sql).ToList();
                    if (list != null && list.Count() > 0)
                    {
                        foreach (var empId in list)
                        {
                            //JwtAuthHelper jah = new JwtAuthHelper();
                            //if (jah.GenerateToken(empId.ToString()) == false)
                            //{
                            //    errStr.AppendFormat("Generate or Update new token failed! UserName: {0} \n", empId);
                            //}
                        }
                    }
                    if (errStr.Length > 0)
                    {
                        resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.InternalServerError, errStr.ToString());
                    }
                    else
                    {
                        resp = ExtensionHelper.LogAndResponse(new StringContent("Generated all token successful"));
                    }
                }                
            }
            catch (Exception ex)
            {
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.ExpectationFailed, ex.Message);
                ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, "Account - GenerateAllUsersToken", 2, "GenerateAllUsersToken Error", null);
            }

            return resp;
        }


        /// <summary>
        /// Remove User Token By UserName
        /// </summary>
        /// <param name="userName">User AD account</param>
        /// <returns></returns>
        [HttpPost]
        [Route("RemoveUserToken/{userName}")]
        public HttpResponseMessage RemoveUserToken(string userName)
        {
            return RemoveUserToken(userName, string.Empty);
        }

        /// <summary>
        /// Remove User Token By UserName
        /// </summary>
        /// <param name="userName">User AD account</param>
        /// <param name="apiConnName">System Name</param>
        /// <returns></returns>
        [HttpPost]
        [Route("RemoveUserToken/{userName}/{apiConnName}")]
        public HttpResponseMessage RemoveUserToken(string userName, string apiConnName)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            ConnectionHelper connectionHelper = new ConnectionHelper(apiConnName);

            try
            {
                if (JwtAuthHelper.UpdateAPIKeyToTDSUser(userName, string.Empty))
                {
                    resp = ExtensionHelper.LogAndResponse(new StringContent("Remove token successful"));
                }
                else
                {
                    resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotFound,
                        string.Format("Remove token failed! UserName: {0}", userName));
                }
            }
            catch (Exception ex)
            {
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.ExpectationFailed, ex.Message);
                ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, "Account - RemoveUserToken", 2, string.Format("RemoveUserToken Error, User Name: {0}", userName), null);
            }
            return resp;
        }

        /// <summary>
        /// Validate Employee Id
        /// </summary>
        /// <param name="employeeId">Employee Id</param>
        /// <returns></returns>
        [HttpPost]
        [Route("ValidateEmployeeId/{employeeId}")]
        public HttpResponseMessage ValidateEmployeeId(string employeeId)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            ConnectionHelper connectionHelper = new ConnectionHelper(string.Empty);
            try
            {
                var isSpecialAD = ConfigurationManager.AppSettings.AllKeys.Contains("ADSpecialCheck")
                    ? bool.Parse(ConfigurationManager.AppSettings["ADSpecialCheck"].ToString()) : false;

                bool isValidate = false;
                if (isSpecialAD == false)
                    isValidate = AccountHelper.DoesUserExist(employeeId);
                else
                    isValidate = AccountHelper.DoesUserExistSpecial(employeeId);                              

                if (isValidate)
                {
                    resp = ExtensionHelper.LogAndResponse(new StringContent("Employee Id exsit!"));
                }
                else // For FBN server usage
                {
                    resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotFound,
                        string.Format("Employee Id not exist! UserName: {0}", employeeId));
                }                
            }
            catch (Exception ex)
            {
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.ExpectationFailed, ex.Message);
                ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, "Account - ValidateEmployeeId", 2, string.Format("ValidateEmployeeId Error, User Name: {0}", employeeId), null);
            }
            return resp;
        }

        /// <summary>
        /// Send Notice Mail after User Reqeust account
        /// </summary>
        /// <param name="accessRequestId">AccessRequestId</param>
        /// <returns></returns>
        [HttpPost]
        [Route("AccountRequestNotice/{accessRequestId}")]
        public HttpResponseMessage AccountRequestNotice(string accessRequestId)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            ConnectionHelper connectionHelper = new ConnectionHelper(string.Empty);

            try
            {
                var mailSubject = ConfigurationManager.AppSettings["mailTitle"].Trim();
                var accReq = AccountHelper.GetAccessRequestInfo(accessRequestId);
                if (accReq != null)
                {
                    var apiURL = LookupHelper.GetConfigValueByName("Centralized_API_Server");

                    //Send to Approver
                    var pocList = AccountHelper.GetProductFamilyOwnerMails(accReq.PRODUCTFAMILYID.ToString());
                    bool isToAppr = false;
                    bool isToReq = false;
                    if (pocList != null && pocList.Count() > 0)
                    {
                        foreach (var item in pocList)
                        {
                            var approverContent = MailHelper.Build2ApproverMail(apiURL, accReq, item);
                            var approverMails = new List<string>() { item.EMAIL };
                            isToAppr = MailHelper.SendMail(string.Empty, approverMails,
                                string.Format("{0} - Approval Notice", mailSubject), approverContent, true);
                        }
                    }
                    else
                    {
                        var approverContent = MailHelper.Build2ApproverMail(apiURL, accReq, null);
                        var approverMails = new List<string>() { ConfigurationManager.AppSettings["receiveMails"].ToString() };
                        isToAppr = MailHelper.SendMail(string.Empty, approverMails,
                            string.Format("{0} - Approval Notice", mailSubject), approverContent, true);
                        //resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotFound,
                        //    string.Format("Missing Product Family: {0} Owner setup!", accReq.PRODUCTFAMILYNAME));
                    }

                    //Send to Requestor
                    if (isToAppr)
                    {
                        var reqContent = MailHelper.Build2RequestorMail(apiURL, accReq);
                        var reqMailList = new List<string>() { accReq.EMAILID };
                        isToReq = MailHelper.SendMail(string.Empty, reqMailList, string.Empty, reqContent, true);
                    }
                    
                    object rtnObj = new { SuccessToReq = isToReq, SuccessToAppr = isToAppr };
                    resp = ExtensionHelper.LogAndResponse(new ObjectContent<object>(rtnObj, new JsonMediaTypeFormatter()));
                }
                else
                {
                    resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotFound,
                        string.Format("AccessRequest Info not exist! AccessRequestId: {0}", accessRequestId));
                }
            }
            catch (Exception ex)
            {
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.ExpectationFailed, ex.Message);
                ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, "Account - ValidateEmployeeId", 2, string.Format("RequestAccountNotice Error, AccessRequestId: {0}", accessRequestId), null);
            }
            return resp;
        }

        /// <summary>
        /// Approve Reqeust account
        /// </summary>
        /// <param name="accessRequestId">AccessRequestId</param>
        /// <param name="isApprove">Approve Or Reject</param>
        /// <param name="approver">Approver</param>
        /// <returns></returns>
        [HttpGet]
        [Route("AccountRequestApprove")]
        public HttpResponseMessage AccountRequestApprove(string accessRequestId, bool isApprove, string approver)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            ConnectionHelper connectionHelper = new ConnectionHelper(string.Empty);

            try
            {
                var mailSubject = ConfigurationManager.AppSettings["mailTitle"].Trim() + " - Approval Result";
                var accReq = AccountHelper.GetAccessRequestInfo(accessRequestId);
                if (accReq != null)
                {
                    if (string.IsNullOrEmpty(accReq.RESULT) || accReq.RESULT.ToUpper().Equals("APPROVED") == false)
                    {
                        var apiURL = LookupHelper.GetConfigValueByName("Centralized_API_Server");
                        //Update Tables
                        if (AccountHelper.ApproveOrRejectAccessRequest(isApprove, approver, accReq))
                        {
                            //Send Approve or Reject Mail
                            var noticeContent = MailHelper.BuildAccountResultMail(apiURL, accReq, isApprove);
                            //var requestorMails = new List<string>() { "jacky.li@lumentum.com" };
                            var requestorMails = new List<string>() { accReq.EMAILID };
                            var isSend = MailHelper.SendMail(string.Empty, requestorMails, mailSubject, noticeContent, true);
                            var result = isApprove ? "Approved" : "Rejected";
                            if (isSend)
                            {
                                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.OK, result + " successful!");
                            }
                            else
                            {
                                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.BadRequest,
                                    "System approval fail. Please contact to IT owner.");
                            }
                        }
                        else
                        {
                            resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.BadRequest,
                                "System approval fail. Please contact to IT owner.");
                        }
                    }
                    else
                    {
                        resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotAcceptable,
                                string.Format("EmployeeId already exist in TDSUser! EmployeeId: {0}", accReq.EMPLOYEEID));
                    }
                }
                else
                {
                    resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotFound,
                        string.Format("AccessRequest Info not exist! AccessRequestId: {0}", accessRequestId));
                }
            }
            catch (Exception ex)
            {
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.ExpectationFailed, ex.Message);
                ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, "Account - ValidateEmployeeId", 2, string.Format("RequestAccountNotice Error, AccessRequestId: {0}", accessRequestId), null);
            }
            return resp;
        }
    }
}
