using Dapper;
using KaizenTDSMvcAPI.Models;
using KaizenTDSMvcAPI.Utils;
using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Data;
using System.Net.Http.Formatting;
using System.Xml.Linq;
using Newtonsoft.Json;
using System.Xml;
using System.Threading.Tasks;
using System.IO;
using SystemLibrary.Utility;

namespace KaizenTDSMvcAPI.Controllers
{
    /// <summary>
    /// Generic Stored Procedue Get/Post API
    /// </summary>
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("api/Generic")]    
    public class GenericController : ApiController
    {
        /// <summary>
        /// Get Data from Stored Procedure
        /// </summary>
        /// <param name="version">Stored Procedure Version</param>
        /// <param name="apiLookupName">API Lookup Name</param>
        /// <param name="format">Return format (xml or json), default = json</param>
        /// <param name="in_criteria">Query Criteria</param>
        /// <returns></returns>
        [HttpGet]
        [Route("{version}/{apiLookupName}")]
        public HttpResponseMessage Get(string version, string apiLookupName,
            string format = "json", string in_criteria = null)
        {
            return Get(string.Empty, version, apiLookupName, format, in_criteria);
        }

        /// <summary>
        /// Get Data from Stored Procedure include System Name
        /// </summary>
        /// <param name="apiConnName">API Connection Name</param>
        /// <param name="version">Stored Procedure Version</param>
        /// <param name="apiLookupName">API Lookup Name</param>
        /// <param name="format">Return format (xml or json), default = json</param>
        /// <param name="in_criteria">Query Criteria</param>
        /// <returns></returns>
        [HttpGet]
        [Route("{apiConnName}/{version}/{apiLookupName}")]
        public HttpResponseMessage Get(string apiConnName, string version, string apiLookupName,
            string format = "json", string in_criteria = null)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                string packageName = string.Empty;
                string xmlBaseTag = string.Empty;
                ConnectionHelper conHelper = new ConnectionHelper(apiConnName);
                var apiLkupList = LookupHelper.GetAPILookupList(apiLookupName, "GET", version, "SP");
                if (apiLkupList != null && apiLkupList.Count() > 0)
                {
                    packageName = apiLkupList.FirstOrDefault().COMMANDVALUE;
                    xmlBaseTag = apiLkupList.FirstOrDefault().BASETAG;
                }
                //else
                //{
                //    var lkList = LookupHelper.GetTableStoredProcMap(apiLookupName, "GET", version);
                //    if (lkList != null && lkList.Count() > 0)
                //    {
                //        packageName = lkList.FirstOrDefault().VALUE;
                //        xmlBaseTag = lkList.FirstOrDefault().ATTRIBUTE;
                //    }
                //}

                if (string.IsNullOrEmpty(packageName) == false && string.IsNullOrEmpty(xmlBaseTag) == false)
                {
                    //split package name and procedure name
                    var exeSpArry = packageName.Split('.');
                    if (exeSpArry.Count() > 1)
                    {
                        var arguments = LookupHelper.GetSPArguments(exeSpArry[1], exeSpArry[0]);
                        if (arguments != null)
                        {
                            SPOutputClass spOutput = new SPOutputClass();
                            var reqParamList = Request.GetQueryNameValuePairs().ToList(); 
                            var itemToRemove = reqParamList.SingleOrDefault(r => r.Key == "format");
                            reqParamList.Remove(itemToRemove); //This is extra parameter for check, no need send to stored procedure

                            if (reqParamList.Count() > 0)
                            {
                                //if exisit token, remove it
                                if (reqParamList.Where(r => r.Key.ToUpper().Equals("TOKEN")).Count() > 0)
                                {
                                    reqParamList.Remove(reqParamList.Where(r => r.Key.ToUpper().Equals("TOKEN")).SingleOrDefault());
                                }
                                spOutput = StoredProcedureHelper.GetSPResByReqstr(reqParamList, packageName, arguments); //By using request string to send extra parameter to stored procedure
                            }
                            else //No request string and only use default setup
                            {
                                spOutput = StoredProcedureHelper.GetResBySPFunc(packageName, in_criteria);
                            }

                            //Decide output Format xml or json
                            if (format.ToLower() == "xml")
                            {
                                var jsonStr = JsonConvert.SerializeObject(spOutput);
                                //XmlDocument doc = (XmlDocument)JsonConvert.DeserializeXmlNode("{'" + APILookupName.ToUpper()  + "':" + jsonStr + "}",
                                jsonStr = jsonStr.Replace("OutputList", apiLookupName.ToUpper());
                                XmlDocument doc = (XmlDocument)JsonConvert.DeserializeXmlNode(jsonStr,
                                    string.IsNullOrEmpty(xmlBaseTag) ? "ROOT" : xmlBaseTag);
                                resp = ExtensionHelper.LogAndResponse(new StringContent(doc.InnerXml, System.Text.Encoding.UTF8, "application/xml"));
                            }
                            else
                            {
                                resp = ExtensionHelper.LogAndResponse(new ObjectContent<SPOutputClass>(spOutput, new JsonMediaTypeFormatter()));
                            }                            
                        }
                    }
                    else
                    {
                        resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotAcceptable, 
                            string.Format("Wrong stored procedure format on lookup table! Stored Procedure: {0}", packageName));
                    }
                }
                else
                {
                    resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotFound,
                        string.Format("No stored procedure found! Version: {0}, API Lookup Name: {1}, Format: {2}", version, apiLookupName, format));
                }
            }
            catch (Exception ex)
            {
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.Conflict, ExtensionHelper.GetAllFootprints(ex));
                ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, apiLookupName + "_" + "GET", 2, string.Empty);
            }

            return resp;
        }

        /// <summary>
        /// Post Data into Stored Procedure by JSON format
        /// </summary>
        /// <param name="jsonInput">JSON input</param>
        /// <param name="version">Stored Procedure Version</param>
        /// <param name="apiLookupName">API Lookup Name</param>
        /// <param name="operation">Operation of Stored Procedure (ex: insert/update/delete)</param>
        /// <returns></returns>
        [HttpPost]
        [Route("{version}/{apiLookupName}/{operation}")]
        public HttpResponseMessage Post([FromBody] List<QueryParamClass> jsonInput, string version, string apiLookupName, string operation)
        {
            return Post(jsonInput, string.Empty, version, apiLookupName, operation);
        }

        /// <summary>
        /// Post Data into Stored Procedure by JSON format include System Name
        /// </summary>
        /// <param name="jsonInput">JSON input</param>
        /// <param name="apiConnName">System name, default is Kaizen TDS</param>
        /// <param name="version">Stored Procedure Version</param>
        /// <param name="apiLookupName">API Lookup Name</param>
        /// <param name="operation">Operation of Stored Procedure (ex: insert/update/delete)</param>
        /// <returns></returns>
        [HttpPost]
        [Route("{apiConnName}/{version}/{apiLookupName}/{operation}")]
        public HttpResponseMessage Post([FromBody] List<QueryParamClass> jsonInput, string apiConnName, string version, string apiLookupName, string operation)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                LogHelper.WriteLine(JsonConvert.SerializeObject(jsonInput)); //For Test purpose
                string packageName = string.Empty;
                ConnectionHelper conHelper = new ConnectionHelper(apiConnName);

                var apiLkupList = LookupHelper.GetAPILookupList(apiLookupName, operation, version, "SP");
                if (apiLkupList != null && apiLkupList.Count() > 0)
                {
                    packageName = apiLkupList.FirstOrDefault().COMMANDVALUE;
                }
                else
                {
                    var lkList = LookupHelper.GetTableStoredProcMap(apiLookupName, operation, version);
                    if (lkList != null && lkList.Count() > 0)
                    {
                        packageName = lkList.FirstOrDefault().VALUE;
                    }
                }

                if (string.IsNullOrEmpty(packageName) == false)
                {
                    var res = StoredProcedureHelper.ExecuteSpFunc(jsonInput, packageName);
                    resp = ExtensionHelper.LogAndResponse(new ObjectContent<object>(res, new JsonMediaTypeFormatter()));
                }
                else
                {
                    resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotFound,
                        string.Format("No stored procedure found! Version: {0}, API Lookup Name: {1}", version, apiLookupName));
                }
            }
            catch (Exception ex)
            {                
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.Conflict, ExtensionHelper.GetAllFootprints(ex), ex);
                ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, apiLookupName + "_" + operation, 2, string.Empty, jsonInput);
            }
            return resp;
        }


        ///// <summary>
        ///// Execute Stored Procedure
        ///// </summary>
        ///// <param name="Input">Input/Output parameters for Stored Procedure</param>
        ///// <param name="StoredProcedureName">Stored Procedure Name(ex: pkg_generic.xxx_ins)</param>
        ///// <returns></returns>
        //[HttpPost]
        //[Route("ExecuteSP")]
        //public object ExecuteStoredProcedure([FromBody] List<QueryParamClass> Input, string StoredProcedureName)
        //{
        //    return StoredProcedureHelper.ExecuteSpFunc(Input, StoredProcedureName);
        //}

        ///// <summary>
        ///// GetResult By Stored Procedure
        ///// </summary>
        ///// <param name="StoredProcedureName">Stored Procedure Name(ex: pkg_generic.get_xxx)</param>
        ///// <param name="Criteria">Query criteria</param>
        ///// <returns></returns>
        //[HttpGet]
        //[Route("GetResultBySP/{StoredProcedureName}/{Criteria?}")]
        //public SPOutputClass GetResultByStoredProcedure(string StoredProcedureName, string Criteria = null)
        //{
        //    return StoredProcedureHelper.GetResBySPFunc(StoredProcedureName, Criteria);
        //}

        ///// <summary>
        ///// GetResult By Stored Procedure
        ///// </summary>
        ///// <param name="StoredProcedureName">Stored Procedure Name(ex: pkg_generic.get_xxx)</param>
        ///// <param name="page">Query Page</param>
        ///// <param name="pageSize">Query Page Size</param>
        ///// <param name="Criteria">Query criteria</param>
        ///// <returns></returns>
        //[HttpGet]
        //[Route("GetResultBySPAndPaged/{StoredProcedureName}/{page}")]
        //public SPOutputClass GetResultBySPAndPaged(string StoredProcedureName, int page, int pageSize, string Criteria)
        //{
        //    return StoredProcedureHelper.GetResBySpAndPagedFunc(StoredProcedureName, page, pageSize, Criteria);
        //}

        /// <summary>
        /// Uplodad File
        /// </summary>
        /// <param name="folderName">FolderName</param>
        /// <returns></returns>
        [HttpPost]
        [Route("FileUpload/{folderName}")]
        public HttpResponseMessage FileUpload(string folderName)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                ConnectionHelper conHelper = new ConnectionHelper(string.Empty);
                var isUpload = TDSFileHelper.FileUploadAsync(this, folderName);
                object rtnObj = new { FileName = TDSFileHelper.UploadFileName, IsSuccess = isUpload.Result };
                resp = ExtensionHelper.LogAndResponse(new ObjectContent<object>(rtnObj, new JsonMediaTypeFormatter()));
            }
            catch (Exception ex)
            {
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.Conflict, ExtensionHelper.GetAllFootprints(ex));
                ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, "FileUpload_" + folderName, 2, string.Empty);
            }
            return resp;
        }

        /// <summary>
        /// File Ingestion API
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("FileIngestion")]
        public HttpResponseMessage FileIngestion(string format = "json")
        {
            ConnectionHelper conHelper = new ConnectionHelper(string.Empty);
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            object rtnObj = null;
            try
            {
                var isUpload = TDSFileHelper.FileUploadAsync(this);
                if (isUpload.Result)
                {
                    var ingesRes = new TDS_Data_Upload.Upload(TDSFileHelper.UploadFileName, ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING, this.Url.Content("~/"));
                    if (ingesRes.Upload_File())
                    {
                        rtnObj = new { Id = ingesRes.TestHeaderid, IsSuccess = true };
                    }
                    else
                    {
                        rtnObj = new { ErrorMessage = ingesRes.Error_Message, IsSuccess = false, LastVisitedXml = ingesRes.last_visited_xml };
                    }

                    if (format.ToLower() == "xml")
                    {
                        var jsonStr = JsonConvert.SerializeObject(rtnObj);
                        XmlDocument doc = (XmlDocument)JsonConvert.DeserializeXmlNode(jsonStr, "root");
                        resp = ExtensionHelper.LogAndResponse(new StringContent(doc.InnerXml, System.Text.Encoding.UTF8, "application/xml"));
                    }
                    else
                    {
                        resp = ExtensionHelper.LogAndResponse(new ObjectContent<dynamic>(rtnObj, new JsonMediaTypeFormatter()));
                    }

                    //File.Delete(FileHelper.UploadFileName);
                }
            }
            catch (Exception ex)
            {
                rtnObj = new { ErrorMessage = ExtensionHelper.GetAllFootprints(ex), IsSuccess = false };
                resp = ExtensionHelper.LogAndResponse(new ObjectContent<object>(rtnObj, new JsonMediaTypeFormatter()));
                ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, "FileIngestion_POST", 2, string.Empty);
            }
            if (File.Exists(TDSFileHelper.UploadFileName))
                File.Delete(TDSFileHelper.UploadFileName);

            return resp;
        }

        /// <summary>
        /// Save Employee Photo via EmployeeId (ex: xxx12345)
        /// </summary>
        /// <param name="EmployeeId">EmployeeId</param>
        /// <param name="fileType">Default = jpg</param>
        /// <returns></returns>
        [HttpPost]
        [Route("SaveEmpPhoto/{EmployeeId}")]
        public HttpResponseMessage SaveEmpPhoto(string EmployeeId, string fileType = "jpg")
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                var result = TDSFileHelper.DownloadUserPhoto(EmployeeId, fileType);
                bool isSuccess = false;
                isSuccess = (string.IsNullOrEmpty(result) == false);

                var rtnObj = new { IsSuccess = isSuccess, FilePath = result };
                resp = ExtensionHelper.LogAndResponse(new ObjectContent<object>(rtnObj, new JsonMediaTypeFormatter()));
            }
            catch (Exception ex)
            {
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.Conflict, ExtensionHelper.GetAllFootprints(ex), ex);
                ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, "SaveEmpPhoto", 2, EmployeeId, null);
            }

            return resp;
        }

        /// <summary>
        /// Get Data By SQL
        /// </summary>
        /// <param name="apiConnName">System name</param>
        /// <param name="sql">SQL</param>
        /// <param name="format">Return format (xml or json), default = json</param>
        /// <returns></returns>
        [HttpPost]
        [Route("GetDataBySQL/{apiConnName}")]
        public HttpResponseMessage GetDataBySQL(string apiConnName, string sql, string format = "json")
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                ConnectionHelper conHelper = new ConnectionHelper(apiConnName);
                var res = ConnectionHelper.QueryDataBySQL(sql);

                var rtnObj = new
                {
                    OutputList = res.ToList(),
                    TotalCount = res.Count()
                };
                if (format.ToLower() == "xml")
                {
                    var jsonStr = JsonConvert.SerializeObject(rtnObj);
                    XmlDocument doc = (XmlDocument)JsonConvert.DeserializeXmlNode(jsonStr, "root");
                    //jsonStr = jsonStr.Replace("OutputList", "root");
                    resp = ExtensionHelper.LogAndResponse(new StringContent(doc.InnerXml, System.Text.Encoding.UTF8, "application/xml"));
                }
                else
                {
                    resp = ExtensionHelper.LogAndResponse(new ObjectContent<dynamic>(rtnObj, new JsonMediaTypeFormatter()));
                }
            }
            catch (Exception ex)
            {
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.Conflict, ExtensionHelper.GetAllFootprints(ex), ex);
                ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, "GetDataBySQL", 2, sql, null);
            }

            return resp;
        }


        //[HttpGet]
        //[Route("Test/{APIConnectionName}/{version}/{APILookupName}")]
        //public HttpResponseMessage Test(string APIConnectionName, string version, string APILookupName, string format = "json")
        //{
        //    var resp = new HttpResponseMessage(HttpStatusCode.OK);
        //    try
        //    {
        //        ConnectionHelper conHelper = new ConnectionHelper(APIConnectionName);
        //        List<TABLE_LOOKUP> lkList = LookupHelper.GetTableStoredProcMap(APILookupName, "GET", version);
        //        if (lkList != null && lkList.Count() > 0)
        //        {
        //            //split package name and procedure name
        //            var exeSpArry = lkList.FirstOrDefault().VALUE.Split('.');
        //            if (exeSpArry.Count() > 1)
        //            {
        //                //Get parameters from SYS.ALL_ARGUMENTS by PkgName + Stored Procedure name
        //                var arguments = LookupHelper.GetSPArguments(exeSpArry[1], exeSpArry[0]);
        //                if (arguments != null)
        //                {
        //                    var reqParamList = Request.GetQueryNameValuePairs().ToList();
        //                    var rtnSP = StoredProcedureHelper.GetSPResByReqstr(reqParamList, lkList.First().VALUE, arguments);

        //                    if (format.ToLower() == "xml")
        //                    {
        //                        var jsonStr = JsonConvert.SerializeObject(rtnSP);
        //                        //XmlDocument doc = (XmlDocument)JsonConvert.DeserializeXmlNode("{'" + APILookupName.ToUpper()  + "':" + jsonStr + "}",
        //                        jsonStr = jsonStr.Replace("OutputList", APILookupName.ToUpper());
        //                        XmlDocument doc = (XmlDocument)JsonConvert.DeserializeXmlNode(jsonStr, lkList.FirstOrDefault().ATTRIBUTE);
        //                        resp = ExtensionHelper.LogAndResponse(new StringContent(doc.InnerXml, System.Text.Encoding.UTF8, "application/xml"));
        //                    }
        //                    else
        //                    {
        //                        resp = ExtensionHelper.LogAndResponse(new ObjectContent<SPOutputClass>(rtnSP, new JsonMediaTypeFormatter()));
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotAcceptable,
        //                    string.Format("Wrong stored procedure format on lookup table! Stored Procedure: {0}", lkList.FirstOrDefault().VALUE));
        //            }
        //        }
        //        else
        //        {
        //            resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotFound,
        //                string.Format("No stored procedure found! Version: {0}, API Lookup Name: {1}, Format: {2}", version, APILookupName, format));
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.Conflict, ExtensionHelper.GetAllFootprints(ex));
        //    }

        //    return resp;
        //}
    }
}
