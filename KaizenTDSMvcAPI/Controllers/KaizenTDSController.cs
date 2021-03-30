using Dapper;
using KaizenTDSMvcAPI.Models.KaizenTDSClasses;
using KaizenTDSMvcAPI.Utils;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Results;
using System.Xml;
using SystemLibrary.Utility;

namespace KaizenTDSMvcAPI.Controllers
{
    /// <summary>
    /// Kaizen TDS Specific function Get/Post API
    /// </summary>
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("api/KaizenTDS")]
    public class KaizenTDSController : ApiController
    {
        /// <summary>
        /// Get ERP Product Hierachy
        /// </summary>
        /// <param name="partnumber">Part Number</param>
        /// <param name="format">Return format (xml or json), default = json</param>
        /// <returns></returns>
        [HttpGet]
        [Route("ERPProductHierarchy/{partnumber}")]
        public HttpResponseMessage ERPProductHierarchy(string partnumber, string format = "json")
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                ConnectionHelper conHelper = new ConnectionHelper("ERPProductHierarchy");
                string sql = string.Format(@"select distinct 
                                            ITEM as PARTNUMBER ,
                                            ITEM_DESC as PRODUCTDESC ,
                                            L1_TOTAL_PROD_LINE_DESC as L1TOTALPRODUCTLINE ,
                                            L2_MARKET_SEGMENT_DESC as L2MARKETSEGMENT,
                                            L3_BUSINESS_GROUP_DESC as L3BUSINESSGROUP,
                                            L4_BUSINESS_UNIT_DESC as L4BUSINESSUNITDESC,
                                            L5_PRODUCT_LINE_DESC as L5PRODUCTLINEDESC,
                                            L6_PRODUCT_FAMILY_DESC as L6PRODUCTFAMILY,
                                            GL_PRODUCT_LINE_DESC L7PRODUCTLINE
                                            from apps.XXSC_ITEM_PRODUCT_HIERARCHY_F
                                            where ITEM = '{0}' ", partnumber);

                var res = ConnectionHelper.QueryDataBySQL(sql);
                DataTable dt = new DataTable();

               if (res.Count() == 0)
                {
                    conHelper = new ConnectionHelper(string.Empty);
                    sql = string.Format(@"select distinct 
                                            ITEM_NUMBER as PARTNUMBER,
                                            DESCRIPTION as PRODUCTDESC,
                                            L1_TOTAL_PROD_LINE as L1TOTALPRODUCTLINE,
                                            L2_MARKET_SEGMENT as L2MARKETSEGMENT,
                                            L3_BUSINESS_GROUP as L3BUSINESSGROUP,
                                            L4_BUSINESS_UNIT as L4BUSINESSUNITDESC,
                                            L5_PRODUCT_LINE as L5PRODUCTLINEDESC,
                                            L6_PRODUCT_FAMILY as L6PRODUCTFAMILY,
                                            L7_PRODUCT_LINE as L7PRODUCTLINE
                                            from Agile.Tbl_lpn_data where ITEM_NUMBER = '{0}'", partnumber);

                    DBHelper dBHelper = new DBHelper();
                    var athenaConnStr = LookupHelper.GetConfigValueByName("KaizenTDSAthenaConn");
                    var dbConn = conHelper.GetODBCDBConn(athenaConnStr);
                    dt = dBHelper.GetDataTable(dbConn, sql);
                    res = ExtensionHelper.ToDynamicList(dt);
                }

                var rtnObj = new
                {
                    OutputList = res.ToList(),
                    TotalCount = res.Count()
                };
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
            }
            catch (Exception ex)
            {
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.Conflict, ExtensionHelper.GetAllFootprints(ex), ex);
                ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, "ERPProductHierarchy", 2, partnumber, null);
            }

            return resp;
        }

        /// <summary>
        /// Test File download
        /// </summary>
        /// <param name="testheaderId">Test Header Id</param>
        /// <param name="filename"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("TestFileDownload/{testheaderId}")]
        public HttpResponseMessage TestFileDownload(string testheaderId, string filename, string tableName = "")
        {
            return TestFileDownload(string.Empty, testheaderId, tableName, filename);
        }

        /// <summary>
        /// TestFileDownload
        /// </summary>
        /// <param name="APIConnectionName"></param>
        /// <param name="testheaderId"></param>
        /// <param name="tableName"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("TestFileDownload/{APIConnectionName}/{testheaderId}/{tableName}/{filename}")]
        public HttpResponseMessage TestFileDownload(string APIConnectionName, string testheaderId, string tableName, string filename)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                ConnectionHelper conHelper = new ConnectionHelper(APIConnectionName);
                //var root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");
                //var exists = Directory.Exists(root);
                //if (!exists)
                //{
                //    Directory.CreateDirectory("App_Data");
                //}

                var fileNameItem = TDSFileHelper.GetFileNameByTestHeaderId(testheaderId, tableName, filename);
                if (fileNameItem == null)
                {
                    resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotFound, "Data Not Exists in database");
                    return resp;
                }
                var fileName = string.Format("{0}\\{1}", fileNameItem.ARCHIVEFOLDER, fileNameItem.FILENAME);

                //var filePath = Path.Combine(root, fileName);
                //LogHelper.WriteLine("Test");
                LogHelper.WriteLine(fileName);
                if (File.Exists(fileName) == false)
                {
                    //return null;
                    //Download from AWS
                    AWSS3Helper awsHelper = new AWSS3Helper();
                    var bucketName = LookupHelper.GetConfigValueByName("Archive_BucketName"); //lum-tds
                    var archiveFolderName = LookupHelper.GetConfigValueByName("Archive_Folder"); //\\thaapptdsdev03\Archive
                    var awsFolderName = LookupHelper.GetConfigValueByName("AWSFolderPath"); //Dev
                    var awsFilePath = fileNameItem.ARCHIVEFOLDER.Replace(archiveFolderName, awsFolderName).Replace(@"\", "/");
                    if (awsHelper.Download_from_s3(bucketName, awsFilePath, fileNameItem.FILENAME, fileNameItem.ARCHIVEFOLDER) == false)
                    {
                        resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotFound, "File Not Exists");
                        return resp;
                    }
                }

                //LogHelper.WriteLine("Exsit!");

                var fileStream = new FileStream(fileName, FileMode.Open);
                var content = new StreamContent(fileStream);
                content.Headers.ContentType = new MediaTypeHeaderValue(MimeMapping.GetMimeMapping(fileName));

                var origFileName = new FileInfo(fileName).Name;
                content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = origFileName
                };
                resp.Content = content;
            }
            catch (Exception ex)
            {
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.InternalServerError, ExtensionHelper.GetAllFootprints(ex), ex);
            }
            return resp;
        }

        /// <summary>
        /// TestFileDownloadNew
        /// </summary>
        /// <param name="APIConnectionName"></param>
        /// <param name="testheaderId"></param>
        /// <param name="tableName"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("TestFileDownloadNew/{APIConnectionName}/{testheaderId}")]
        public HttpResponseMessage TestFileDownloadNew(string APIConnectionName, string testheaderId, string tableName, string filename)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                ConnectionHelper conHelper = new ConnectionHelper(APIConnectionName);
                var fileNameItem = TDSFileHelper.GetFileNameByTestHeaderId(testheaderId, tableName, filename);
                if (fileNameItem == null)
                {
                    resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotFound, "Data Not Exists in database");
                    return resp;
                }
                var fileName = string.Format("{0}\\{1}", fileNameItem.ARCHIVEFOLDER, fileNameItem.FILENAME);

                //var filePath = Path.Combine(root, fileName);
                //LogHelper.WriteLine("Test");
                //LogHelper.WriteLine(fileName);
                string fileUrl = string.Empty;
                StreamContent content = null;
                if (File.Exists(fileName) == false)
                {
                    AWSS3Helper awsHelper = new AWSS3Helper();
                    var bucketName = LookupHelper.GetConfigValueByName("Archive_BucketName"); //lum-tds
                    var awsFolderName = LookupHelper.GetConfigValueByName("AWSFolderPath"); //Dev
                    List<string> archiveChars = fileNameItem.ARCHIVEFOLDER.Split('\\').ToList();
                    int startIdx = archiveChars.IndexOf("Archive");
                    string[] s3Chars = archiveChars.Where(x => archiveChars.IndexOf(x) > startIdx).ToArray();
                    var awsFilePath = awsFolderName + "/" + string.Join("/", s3Chars) + "/" + fileNameItem.FILENAME;

                    var stream = awsHelper.Download_from_s3(bucketName, awsFilePath);
                    content = new StreamContent(stream);
                }
                else
                {
                    var fileStream = new FileStream(fileName, FileMode.Open);
                    content = new StreamContent(fileStream);
                }

                content.Headers.ContentType = new MediaTypeHeaderValue(MimeMapping.GetMimeMapping(fileNameItem.FILENAME));
                content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = fileNameItem.FILENAME
                };
                resp.Content = content;
            }
            catch (Exception ex)
            {
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.InternalServerError, ExtensionHelper.GetAllFootprints(ex), ex);
            }
            return resp;
        }

        /// <summary>
        /// GetAllTestFiles (will check the file from s3 if the data has been deleted)
        /// </summary>
        /// <param name="APIConnectionName"></param>
        /// <param name="testheaderId"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("GetAllTestFiles/{APIConnectionName}/{testheaderId}/{tableName}")]
        public HttpResponseMessage GetAllTestFiles(string APIConnectionName, string testheaderId, string tableName)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                ConnectionHelper conHelper = new ConnectionHelper(APIConnectionName);
                var fileNameItem = TDSFileHelper.GetFileNameByTestHeaderId(testheaderId, string.Empty, string.Empty);
                if (fileNameItem == null)
                {
                    resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotFound, "Data Not Exists in database");
                    return resp;
                }
                


                Dictionary<string, string> list = new Dictionary<string, string>();
                var resultList = ConnectionHelper.QueryDataBySQL(string.Format("Select * From {0} where TestHeaderId = {1}", tableName, testheaderId));

                if (string.IsNullOrEmpty(fileNameItem.ARCHIVEFOLDER) == false)
                {
                    bool isCheckS3 = false;
                    if (Directory.Exists(fileNameItem.ARCHIVEFOLDER))
                    {
                        DirectoryInfo di = new DirectoryInfo(fileNameItem.ARCHIVEFOLDER);
                        if (di.GetFiles().Count() == 0)
                            isCheckS3 = true;
                    }
                    else isCheckS3 = true;
                    if (isCheckS3)
                    {
                        AWSS3Helper awsHelper = new AWSS3Helper();
                        var bucketName = LookupHelper.GetConfigValueByName("Archive_BucketName"); //lum-tds
                        var awsFolderName = LookupHelper.GetConfigValueByName("AWSFolderPath"); //Dev

                        List<string> archiveChars = fileNameItem.ARCHIVEFOLDER.Split('\\').ToList();
                        int startIdx = archiveChars.IndexOf("Archive");
                        string[] s3Chars = archiveChars.Where(x => archiveChars.IndexOf(x) > startIdx).ToArray();
                        var awsFilePath = awsFolderName + "/" + string.Join("/", s3Chars);
                        //var awsFilePath = awsFolderName + "/" + fileNameItem.STARTTIME.ToString("yyyyMM") 
                        //    + "/" + fileNameItem.STARTTIME.ToString("dd") + "/" + testheaderId;

                        list = awsHelper.GenerateAllURL_from_s3(bucketName, awsFilePath);
                    }
                }
             
                TDSFileHelper.FileExistChecker(resultList, list);
                //var imageDataList = ConnectionHelper.QueryDataBySQL(string.Format("Select * From ImageData_v where TestHeaderId = {0}", testheaderId));
                //TDSFileHelper.FileExistChecker(imageDataList, list);
                //var attachmentDataList = ConnectionHelper.QueryDataBySQL(string.Format("Select * From AttachmentData_v where TestHeaderId = {0}", testheaderId));
                //TDSFileHelper.FileExistChecker(attachmentDataList, list);
                //var tableDataList = ConnectionHelper.QueryDataBySQL(string.Format("Select * From TableData_v where TestHeaderId = {0}", testheaderId));
                //TDSFileHelper.FileExistChecker(tableDataList, list);

                var rtnObj = new { ResultList = resultList };
                resp = ExtensionHelper.LogAndResponse(new ObjectContent<object>(rtnObj, new JsonMediaTypeFormatter()));
            }
            catch (Exception ex)
            {
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.InternalServerError, ExtensionHelper.GetAllFootprints(ex), ex);
            }
            return resp;
        }

        /// <summary>
        /// Get All Dll Version
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("GetAllDLLVersion")]
        public HttpResponseMessage GetAllDLLVersion()
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);

            try
            {
                var apiVersion = Assembly.GetExecutingAssembly().GetName().Version;
                var ingestionDllVer = AppDomain.CurrentDomain.GetAssemblies().Where(r => r.FullName.StartsWith("IngestionDLL")).FirstOrDefault().GetName().Version;
                var dataUploadDllVer = AppDomain.CurrentDomain.GetAssemblies().Where(r => r.FullName.StartsWith("TDS_Data_Upload")).FirstOrDefault().GetName().Version;
                var rtnObj = new { APIVersion = apiVersion.ToString(), IngestionDLLVersion = ingestionDllVer.ToString(), DataUploaderDLLVersion = dataUploadDllVer.ToString() };
                resp = ExtensionHelper.LogAndResponse(new ObjectContent<dynamic>(rtnObj, new JsonMediaTypeFormatter()));
            }
            catch (Exception ex)
            {
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.Conflict, ExtensionHelper.GetAllFootprints(ex), ex);
                ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, "GetAllDLLVersion", 2, "ExpLog", null);
            }
            return resp;
        }


        [HttpPost]
        [Route("TableDataIngestion/{apiConnName}/{tableId}")]
        public HttpResponseMessage TableDataIngestion(string apiConnName, string tableId)
        {
            ConnectionHelper conHelper = new ConnectionHelper(apiConnName);
            var resp = new HttpResponseMessage(HttpStatusCode.OK);

            try
            {
                string sql = string.Format(@"select tableid, tablename, filenamelong as archivefilename, productid, testheaderid, testheaderstepid from tabledata_v where tableid = {0}", tableId);
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DEFAULTCONNECTIONSTRING))
                {
                    var list = sqlConn.Query<TableDataVClass>(sql).ToList();
                    if (list.Count() > 0)
                    {
                        string msg = TableDataIngestUtil.ProcessTableData(list.FirstOrDefault());
                        if (string.IsNullOrEmpty(msg))
                        {
                            var res = new { IsSuccess = true, ErrorMessage = "Success" };
                            resp = ExtensionHelper.LogAndResponse(new ObjectContent<object>(res, new JsonMediaTypeFormatter()));
                        }
                        else
                        {
                            var res = new { IsSuccess = false, ErrorMessage = msg };
                            resp = ExtensionHelper.LogAndResponse(new ObjectContent<object>(res, new JsonMediaTypeFormatter()));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.Conflict, ExtensionHelper.GetAllFootprints(ex), ex);
                ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, "TableDataIngestion", 2, tableId, null);
            }

            return resp;
        }

        /// <summary>
        /// ProductGenealogyExpansion
        /// </summary>
        /// <param name="serialNumber">input SN</param>
        /// <param name="recursive">Is Recursive</param>
        /// <returns></returns>
        [HttpGet]
        [Route("ProductGenealogyExpansion/{serialNumber}/{recursive}")]
        public HttpResponseMessage ProductGenealogyExpansion(string serialNumber, bool recursive = false)
        {
            ConnectionHelper conHelper = new ConnectionHelper(string.Empty);
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                List<DeviceRelationClass> result = DeviceRelationsHelper.RecursiveDeviceRelations(new List<string> { serialNumber }, new List<string>(), recursive);
                resp = ExtensionHelper.LogAndResponse(new ObjectContent<object>(result, new JsonMediaTypeFormatter()));
            }
            catch (Exception ex)
            {
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.Conflict, ExtensionHelper.GetAllFootprints(ex), ex);
                ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, "ProductGenealogyExpansion", 2, serialNumber, null);
            }

            return resp;
        }

        [HttpGet]
        [Route("GetAllPackagesByAPIConnName/{apiConnName}")]
        public HttpResponseMessage GetAllPackagesByAPIConnName(string apiConnName, string format = "json")
        {
            ConnectionHelper conHelper = new ConnectionHelper(apiConnName);
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                string sql = @"select OWNER, OBJECT_NAME, PROCEDURE_NAME FROM dba_procedures 
                            WHERE owner IN(select sys_context('userenv', 'current_schema') from dual) and object_type='PACKAGE' 
                            and PROCEDURE_NAME IS NOT NULL  ORDER BY OBJECT_NAME, SUBPROGRAM_ID ";

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
                ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, "GetCurrentSchemaAllPackages", 2, apiConnName, null);
            }

            return resp;
        }

        /// <summary>
        /// GenerateTestReport
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("GenerateTestReport/{testHeaderId}")]
        public HttpResponseMessage GenerateTestReport(string testHeaderId)
        {
            ConnectionHelper conHelper = new ConnectionHelper(string.Empty);
            TestReportHelper pDFHelper = new TestReportHelper();
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                var stream = pDFHelper.CreateTestReport(testHeaderId);
                if (stream.ToArray().Count() > 0)
                {
                    resp.Content = new ByteArrayContent(stream.ToArray());
                    resp.Content.Headers.ContentDisposition =
                        new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                        {
                            FileName = string.Format("TestReport_{0}_{1}.pdf", pDFHelper._TestHeader.PRODUCTFAMILYNAME, pDFHelper._TestHeader.SERIALNUMBER)
                        };
                    //result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    resp.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                }
                else
                    resp.StatusCode = HttpStatusCode.NotFound;
            }
            catch (Exception ex)
            {
                ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, "GenerateTestReport", 2, testHeaderId, null);
                resp.StatusCode = HttpStatusCode.BadRequest;
            }

            return resp;
        }

    }
}
