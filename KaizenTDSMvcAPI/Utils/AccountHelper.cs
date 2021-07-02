using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Web;
using SystemLibrary.Utility;
using Dapper;
using System.DirectoryServices.AccountManagement;
using KaizenTDSMvcAPI.Models.KaizenTDSClasses;

namespace KaizenTDSMvcAPI.Utils
{
    public class AccountHelper
    {
        /// <summary>
        /// LDAP Server
        /// </summary>
        public static string[] _ldaps = { "lidc15", "lidc01", "lidc03", "lidc05", "lidc13", "lidc17", "lidc16", "lidc20", "lidc18", "lidc19" };

        /// <summary>
        /// CheckADByUserPwd
        /// </summary>
        /// <param name="userName">UserName</param>
        /// <param name="password">Password</param>
        /// <returns></returns>
        public static SearchResult GetADSearchResult(string userName, string password)
        {
            SearchResult result = null;
            var searchingLdapName = string.Empty;
            try
            {
                var ldapMainServer = LookupHelper.GetConfigValueByName("LDAP_Main_Server");

                if (string.IsNullOrEmpty(ldapMainServer) == false)
                {
                    searchingLdapName = ldapMainServer;
                    result = ExcuteADSearch(userName, password, ldapMainServer);
                }

                if (result == null)
                {
                    foreach (string ldap in _ldaps)
                    {
                        searchingLdapName = ldap;
                        result = ExcuteADSearch(userName, password, ldap);
                        if (result != null) break;
                    }
                }
            }
            catch (Exception)
            {
                LogHelper.WriteLine(searchingLdapName + " search failed");
                //throw ex;
            }
            return result;
        }

        private static SearchResult ExcuteADSearch(string userName, string password, string ldap)
        {
            SearchResult result = null;
            //use AD authurzation for user authurzation
            string strDomain = "{0}.li.lumentuminc.net/DC=li,DC=lumentuminc,DC=net";

            try
            {
                string path = string.Format(strDomain, ldap);
                LogHelper.WriteLine("check ldap path: " + path + ", Doman: " + strDomain + ", UserName: " + userName);
                DirectoryEntry adEntry = new DirectoryEntry("LDAP://" + path, userName, password);
                DirectorySearcher adSearcher = new DirectorySearcher(adEntry);
                adSearcher.SearchScope = SearchScope.OneLevel;
                result = adSearcher.FindOne();
                if (result != null)
                {
                    LogHelper.WriteLine("AD authurzation pass:" + ldap);
                }
                else
                {
                    LogHelper.WriteLine(ldap + " search result is null");
                }
            }
            catch (Exception)
            {
                LogHelper.WriteLine(ldap + " search failed");
                //throw ex;
            }
            return result;
        }

        public static List<dynamic> CheckAccountIsExists(string userName)
        {
            List<dynamic> list = new List<dynamic>();
            try
            {
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                {
                    string sql = string.Format("SELECT * FROM TDSMFG.TDSUSER WHERE UPPER(EMPLOYEEID) = '{0}' AND ROWNUM = 1 ORDER BY LASTMODIFIEDDATE DESC ", userName.ToUpper());
                    var m = sqlConn.Query(sql);
                    list = m.ToList();
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLine(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING);
                throw ex;
            }

            return list;
        }
        public static string base64Encode(string sData) // Encode    
        {
            try
            {
                byte[] encData_byte = new byte[sData.Length];
                encData_byte = System.Text.Encoding.UTF8.GetBytes(sData);
                string encodedData = Convert.ToBase64String(encData_byte);
                return encodedData;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in base64Encode" + ex.Message);
            }
        }
        public static string base64Decode(string sData) //Decode    
        {
            try
            {
                var encoder = new System.Text.UTF8Encoding();
                System.Text.Decoder utf8Decode = encoder.GetDecoder();
                byte[] todecodeByte = Convert.FromBase64String(sData);
                int charCount = utf8Decode.GetCharCount(todecodeByte, 0, todecodeByte.Length);
                char[] decodedChar = new char[charCount];
                utf8Decode.GetChars(todecodeByte, 0, todecodeByte.Length, decodedChar, 0);
                string result = new String(decodedChar);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in base64Decode" + ex.Message);
            }
        }
        public static bool DoesUserExist(string userName)
        {
            string strDomain = "{0}.li.lumentuminc.net/DC=li,DC=lumentuminc,DC=net";

            foreach (string ldap in _ldaps)
            {
                string path = string.Format(strDomain, ldap);
                using (var domainContext = new PrincipalContext(ContextType.Domain, null, path))
                {
                    using (var foundUser = UserPrincipal.FindByIdentity(domainContext, IdentityType.SamAccountName, userName))
                    {
                        return foundUser != null;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// GetProductFamilyOwnerMails
        /// </summary>
        /// <param name="productFamilyId"></param>
        /// <returns></returns>
        public static List<ProductOwnerClass> GetProductFamilyOwnerMails(string productFamilyId)
        {
            try
            {
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DEFAULTCONNECTIONSTRING))
                {
                    string sql = string.Format(@"select upf.username CONTACTNAME, tu.email from userproductfamily_v upf
                                                inner join tdsuser tu on upf.userid = tu.userid
                                                where upf.isowner = 1 and upf.productfamilyid = {0} ", productFamilyId);
                    var list = sqlConn.Query<ProductOwnerClass>(sql);
                    if (list != null && list.Count() > 0)
                    {
                        return list.ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return null;
        }

        /// <summary>
        /// Query Access Request Infor by Id
        /// </summary>
        /// <param name="accessRequestId">accessRequestId</param>
        /// <returns></returns>
        public static AccessRequestClass GetAccessRequestInfo(string accessRequestId)
        {
            try
            {
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DEFAULTCONNECTIONSTRING))
                {
                    string sql = string.Format("SELECT * FROM ACCESSREQUEST_V where ACCESSREQUESTID = {0}", accessRequestId);
                    var list = sqlConn.Query<AccessRequestClass>(sql);
                    if (list != null && list.Count() > 0)
                    {
                        return list.FirstOrDefault();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return null;
        }

        /// <summary>
        /// ApproveOrRejectAccessRequest
        /// </summary>
        /// <param name="isApprove">Approve Or Reject</param>
        /// <param name="approverEmpId">Approver Employee ID</param>
        /// <param name="acrItem">Access Request Instance</param>
        /// <returns></returns>
        public static bool ApproveOrRejectAccessRequest(bool isApprove, string approverEmpId, AccessRequestClass acrItem)
        {
            try
            {
                //Update Access Request Status
                if (UpdateAccessRequest(isApprove, approverEmpId, acrItem.ACCESSREQUESTID))
                {
                    //Insert TDSUser if is approved
                    if (isApprove)
                    {
                        var account = CheckAccountIsExists(acrItem.EMPLOYEEID.Trim());
                        var isSetToken = true;
                        using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DEFAULTCONNECTIONSTRING))
                        {
                            string sql = string.Empty;
                            //If TDS User Not exist
                            if (account.Count() == 0)
                            {
                                sql = string.Format(@"INSERT INTO TDSUSER(USERID,EMPLOYEEID,FIRSTNAME,LASTNAME,EMAIL,PCNAME,ISADMIN,ISSUPERUSER,USERDESC,CREATEDBY,CREATEDDATE,ROLEID,APIKEY) 
                                            VALUES (SEQ_TDSUSER.NEXTVAL,'{0}','{1}','{2}','{3}','',0,0,'','{4}',SYSDATE,{5}, '') ",
                                    acrItem.EMPLOYEEID, acrItem.FIRSTNAME,
                                    acrItem.LASTNAME, acrItem.EMAILID, approverEmpId, acrItem.ROLEID);
                                if (sqlConn.Execute(sql) > 0)
                                {
                                    //Set API KEY
                                    JwtAuthHelper jwtHelper = new JwtAuthHelper();
                                    //If return false, will affect to the result
                                    isSetToken = jwtHelper.GenerateToken(acrItem.EMPLOYEEID);
                                }
                            }
                            //Check If Account created
                            account = CheckAccountIsExists(acrItem.EMPLOYEEID.Trim());
                            if (account.Count() > 0 && isSetToken)
                            {
                                sql = string.Format(@"INSERT INTO USERPRODUCTFAMILY(USERPRODUCTFAMILYID,USERID,PRODUCTFAMILYID,
                                            ISOWNER,ISUSER,ISSPECAUTHORIZER,TDSUSERPRODUCTFAMILYDESC,
                                            CREATEDBY,CREATEDDATE) 
                                            VALUES (SEQ_USERPRODUCTFAMILY.NEXTVAL,
                                            (SELECT USERID FROM TDSUSER WHERE UPPER(EMPLOYEEID) = '{0}'),
                                            {1},0,1,0,'','{2}',SYSDATE) ",
                                            acrItem.EMPLOYEEID.ToUpper(), acrItem.PRODUCTFAMILYID, approverEmpId);
                                if (sqlConn.Execute(sql) > 0) return true;
                            }
                        }
                    }
                    else return true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
            return false;
        }

        private static bool UpdateAccessRequest(bool isApprove, string approverEmpId, int accessRequestId)
        {
            var result = isApprove ? "APPROVED" : "REJECTED";
            using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DEFAULTCONNECTIONSTRING))
            {
                string sql = string.Format(@"UPDATE ACCESSREQUEST SET RESULT = '{0}',
                        SUBMITTEDBY = '{1}', SUBMITTEDDATE = SYSDATE WHERE ACCESSREQUESTID = {2}  ", result, approverEmpId, accessRequestId);
                if (sqlConn.Execute(sql) > 0) return true;
            }
            return false;
        }

        public static List<string> GetEmpIdByMail(string email)
        {
            List<string> list = new List<string>();
            try
            {
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                {
                    string sql = string.Format("SELECT EMPLOYEEID FROM TDSMFG.TDSUSER WHERE UPPER(EMAIL) = '{0}' AND ROWNUM = 1 ORDER BY LASTMODIFIEDDATE DESC ", email.ToUpper());
                    var m = sqlConn.Query<string>(sql);
                    list = m.ToList();
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLine(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING);
                throw ex;
            }

            return list;
        }
    }
}