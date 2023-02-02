using Dapper;
using KaizenTDSMvcAPI.Models.KaizenTDSClasses;
using KaizenTDSMvcAPI.Utils;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Odbc;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.AccessControl;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Results;
using System.Xml;
using SystemLibrary.Utility;
using static KaizenTDSMvcAPI.Utils.FileHelper;

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

                var res = ConnectionHelper.QueryDataBySQL(sql, false);
                //DataTable dt = new DataTable();

               if (res.Count() == 0)
                {
                    conHelper = new ConnectionHelper(string.Empty);
                    var athenaConn = LookupHelper.GetConfigValueByName("KaizenTDSAthenaConn");                    
                    sql = string.Format(@"select distinct 
                                            ITEM_NUMBER as PARTNUMBER,
                                            DESCRIPTION as PRODUCTDESC,
                                            L1_TOTAL_PROD_LINE as L1TOTALPRODUCTLINE,
                                            L2_MARKET_SEGMENT as L2MARKETSEGMENT,
                                            L3_BUSINESS_GROUP as L3BUSINESSGROUP,
                                            L4_BUSINESS_UNIT as L4BUSINESSUNITDESC,
                                            L5_PRODUCT_LINE as L5PRODUCTLINEDESC,
                                            L7_PRODUCT_LINE as L7PRODUCTLINE
                                            from Agile.Tbl_lpn_data where ITEM_NUMBER = '{0}'", partnumber);

                    using (var sqlConn = new OdbcConnection(athenaConn))
                    {
                        res = sqlConn.Query<dynamic>(sql).ToList();
                    }
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
        /// <param name="isCheckAthena">Default = false; Athena data check only, will ingore check from Oracle</param>
        /// <returns></returns>
        /// 20210601 Add Athena Data check
        [HttpGet]
        [Route("TestFileDownload/{APIConnectionName}/{testheaderId}/{tableName}/{filename}")]
        public HttpResponseMessage TestFileDownload(string APIConnectionName, string testheaderId, string tableName, string filename, bool isCheckAthena = false)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                ConnectionHelper conHelper = new ConnectionHelper(APIConnectionName);
                string sql = string.Format(@"SELECT (SELECT ARCHIVELOCATION FROM TESTHEADER_V 
                                        WHERE TESTHEADERID = {1}) ARCHIVEFOLDER,
                                        FILENAME, ARCHIVEFILENAME
                                        FROM {0} WHERE TESTHEADERID = {1} {2} ORDER BY LASTMODIFIEDDATE DESC ", tableName, testheaderId, string.IsNullOrEmpty(filename) == false ? "AND FILENAME = '" + filename.Trim() + "'" : string.Empty);

                var fileNameItem = Utils.FileHelper.GetFileNameByTestHeaderId(sql, isCheckAthena);
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
                    LogHelper.WriteLine("Missing from local, download data from AWS path: " + awsFilePath);
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
        /// <param name="isCheckAthena">Default = false; Athena data check only, will ingore check from Oracle</param>
        /// <returns></returns>
        [HttpGet]
        [Route("TestFileDownloadNew/{APIConnectionName}/{testheaderId}")]
        public HttpResponseMessage TestFileDownloadNew(string APIConnectionName, string testheaderId, string tableName, string filename, bool isCheckAthena = false)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                ConnectionHelper conHelper = new ConnectionHelper(APIConnectionName);
                string sql = string.Format(@"SELECT (SELECT ARCHIVELOCATION FROM TESTHEADER_V 
                                        WHERE TESTHEADERID = {1}) ARCHIVEFOLDER,
                                        FILENAME, ARCHIVEFILENAME
                                        FROM {0} WHERE TESTHEADERID = {1} AND UPPER(FILENAME) = '{2}' ORDER BY LASTMODIFIEDDATE DESC ", tableName, testheaderId, filename.Trim().ToUpper());

                var fileNameItem = Utils.FileHelper.GetFileNameByTestHeaderId(sql, isCheckAthena);
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
                    LogHelper.WriteLine("Missing from local, download data from AWS path: " + awsFilePath);
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
        /// TestFileDownloadMulti
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("TestFileDownloadMulti")]
        public HttpResponseMessage GetAllTestFilesMulti(TestFileInputClass input)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                ConnectionHelper conHelper = new ConnectionHelper(input.APIConnectionName);
                var testheaderIds = input.InputList.Select(r => r.TESTHEADERID).Distinct().ToList();
                var inputFiles = input.InputList.Select(r => r.FILENAME).Distinct().ToList();
                string sql = string.Format(@"SELECT * FROM TESTHEADERATTACHMENT_V WHERE TESTHEADERID IN ({0}) ORDER BY TESTHEADERID DESC ", string.Join(",", testheaderIds));

                //Get from Oracle first time
                var fileNameItems = Utils.FileHelper.GetTestFilesByTestHeaderIds(sql, false);
                //Table data has been clean up, need to retrive back from Athena
                var athenaTestHeaderIds = fileNameItems.Where(r => r.TDSTABLECLEANUPSTATUS == true).Select(r => r.TESTHEADERID).Distinct();
                if (athenaTestHeaderIds.Count() > 0)
                {
                    sql = string.Format(@"SELECT * FROM TESTHEADERATTACHMENT_V WHERE TESTHEADERID IN ({0}) ORDER BY TESTHEADERID DESC ",
                        string.Join(",", athenaTestHeaderIds.ToList()));
                    fileNameItems.AddRange(Utils.FileHelper.GetTestFilesByTestHeaderIds(sql, true));
                }

                if (fileNameItems == null || fileNameItems.Count() == 0)
                {
                    resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotFound, "Data Not Exists in database");
                    return resp;
                }

                List<StreamContent> streamContents = new List<StreamContent>();
                List<Byte[]> streamByteList = new List<byte[]>();
                var files = new List<FileData>();
                var content = new Utils.FileHelper.MultipartContent();
                AWSS3Helper awsHelper = new AWSS3Helper();
                var bucketName = LookupHelper.GetConfigValueByName("Archive_BucketName"); //lum-tds
                var awsFolderName = LookupHelper.GetConfigValueByName("AWSFolderPath"); //Dev

                fileNameItems.AsParallel().ForAll(item =>
                {
                    if (input.InputList.Where(r=> r.FILENAME == item.FILENAME && r.TESTHEADERID == item.TESTHEADERID).ToList().Count() > 0)
                    {
                        if (item.ARCHIVEFILECLEANUPSTATUS)
                        {
                            List<string> archiveChars = item.ARCHIVEFILENAME.Split('\\').ToList();
                            int startIdx = archiveChars.IndexOf("Archive");
                            string[] s3Chars = archiveChars.Where(x => archiveChars.IndexOf(x) > startIdx).ToArray();
                            var awsFilePath = awsFolderName + "/" + string.Join("/", s3Chars);
                            //LogHelper.WriteLine("Missing from local, download data from AWS path: " + awsFilePath);
                            var stream = awsHelper.Download_from_s3(bucketName, awsFilePath);
                            //streamContents.Add(new StreamContent(stream));
                            //streamByteList.Add(TDSFileHelper.ReadStream(stream));
                            var fileData = new FileData { Content = Utils.FileHelper.ReadStream(stream), Name = item.FILENAME };
                            files.Add(fileData);
                        }
                        else
                        {
                            var fileStream = new FileStream(item.ARCHIVEFILENAME, FileMode.Open);
                            //streamByteList.Add(TDSFileHelper.ReadStream(fileStream));
                            //streamContents.Add(new StreamContent(fileStream));
                            var fileData = new FileData { Content = Utils.FileHelper.ReadStream(fileStream), Name = item.FILENAME };
                            files.Add(fileData);
                        }

                    }                    
                });

                //content.Headers.ContentType = new MediaTypeHeaderValue(MimeMapping.GetMimeMapping(fileNameItem.FILENAME));
                //content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                //{
                //    FileName = fileNameItem.FILENAME
                //};
                //resp.Content = content;

                content.files = files.ToArray();
                var str = JsonConvert.SerializeObject(content);
                //var rtnObj = new { OutputList = fileNameItems };
                resp = ExtensionHelper.LogAndResponse(new ObjectContent<object>(str, new JsonMediaTypeFormatter()));
            }
            catch (Exception ex)
            {
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.InternalServerError, ExtensionHelper.GetAllFootprints(ex), ex);
            }
            return resp;
        }

        /// <summary>
        /// TestFileDownloadMulti
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("GetAllTestFilesMultiZipped")]
        public HttpResponseMessage GetAllTestFilesMultiZipped(TestFileInputClass input)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                ConnectionHelper conHelper = new ConnectionHelper(input.APIConnectionName);
                var testheaderIds = input.InputList.Select(r => r.TESTHEADERID).Distinct().ToList();
                var inputFiles = input.InputList.Select(r => r.FILENAME).Distinct().ToList();
                string sql = string.Format(@"SELECT * FROM TESTHEADERATTACHMENT_V WHERE TESTHEADERID IN ({0}) ORDER BY TESTHEADERID DESC ", string.Join(",", testheaderIds));

                //Get from Oracle first time
                var fileNameItems = Utils.FileHelper.GetTestFilesByTestHeaderIds(sql, false);
                //Table data has been clean up, need to retrive back from Athena
                var athenaTestHeaderIds = fileNameItems.Where(r => r.TDSTABLECLEANUPSTATUS == true).Select(r => r.TESTHEADERID).Distinct();
                if (athenaTestHeaderIds.Count() > 0)
                {
                    sql = string.Format(@"SELECT * FROM TESTHEADERATTACHMENT_V WHERE TESTHEADERID IN ({0}) ORDER BY TESTHEADERID DESC ",
                        string.Join(",", athenaTestHeaderIds.ToList()));
                    fileNameItems.AddRange(Utils.FileHelper.GetTestFilesByTestHeaderIds(sql, true));
                }

                if (fileNameItems == null || fileNameItems.Count() == 0)
                {
                    resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotFound, "Data Not Exists in database");
                    return resp;
                }

                Dictionary<string, Stream> streams = new Dictionary<string, Stream>();

                AWSS3Helper awsHelper = new AWSS3Helper();
                var bucketName = LookupHelper.GetConfigValueByName("Archive_BucketName"); //lum-tds
                var awsFolderName = LookupHelper.GetConfigValueByName("AWSFolderPath"); //Dev

                fileNameItems.AsParallel().ForAll(item =>
                {
                    if (input.InputList.Where(r => r.FILENAME == item.FILENAME && r.TESTHEADERID == item.TESTHEADERID).ToList().Count() > 0)
                    {
                        if (item.ARCHIVEFILECLEANUPSTATUS)
                        {
                            List<string> archiveChars = item.ARCHIVEFILENAME.Split('\\').ToList();
                            int startIdx = archiveChars.IndexOf("Archive");
                            string[] s3Chars = archiveChars.Where(x => archiveChars.IndexOf(x) > startIdx).ToArray();
                            var awsFilePath = awsFolderName + "/" + string.Join("/", s3Chars);
                            //LogHelper.WriteLine("Missing from local, download data from AWS path: " + awsFilePath);
                            var stream = awsHelper.Download_from_s3(bucketName, awsFilePath);
                            //streamContents.Add(new StreamContent(stream));
                            //streamByteList.Add(TDSFileHelper.ReadStream(stream));
                            //var fileData = new FileData { Content = Utils.FileHelper.ReadStream(stream), Name = item.FILENAME };
                            //files.Add(fileData);
                            streams.Add(item.FILENAME, stream);
                            //streams.Add("1.csv", new FileStream(Path.Combine(fileDirectory, "1.csv"), FileMode.Open, FileAccess.Read));
                        }
                        else
                        {
                            if (File.Exists(item.ARCHIVEFILENAME) && streams.ContainsKey(item.FILENAME) == false)
                            {
                                var fileStream = new FileStream(item.ARCHIVEFILENAME, FileMode.Open, FileAccess.Read);
                                streams.Add(item.FILENAME, fileStream);
                            }
                        }

                    }
                });
                
                resp.Content = new StreamContent(PackageManyZip(streams));
                resp.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = string.Format("download_compressed_{0}.zip", DateTime.Now.ToString("yyyyMMddHHmmss")) };
                resp.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                //resp = ExtensionHelper.LogAndResponse(new ObjectContent<object>(streams, new JsonMediaTypeFormatter()));
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
        /// <param name="isCheckAthena">Default = false; Athena data check only, will ingore check from Oracle</param>
        /// <returns></returns>
        [HttpGet]
        [Route("GetAllTestFiles/{APIConnectionName}/{testheaderId}/{tableName}")]
        public HttpResponseMessage GetAllTestFiles(string APIConnectionName, string testheaderId, string tableName, bool isCheckAthena = false)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                ConnectionHelper conHelper = new ConnectionHelper(APIConnectionName);
                string sql = string.Format(@"SELECT (SELECT ARCHIVELOCATION FROM TESTHEADER_V 
                                        WHERE TESTHEADERID = {1}) ARCHIVEFOLDER,
                                        FILENAME, ARCHIVEFILENAME
                                        FROM {0} WHERE TESTHEADERID = {1} ", tableName, testheaderId);

                var fileNameItem = Utils.FileHelper.GetFileNameByTestHeaderId(sql, isCheckAthena);
                if (fileNameItem == null)
                {
                    resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotFound, "Data Not Exists in database");
                    return resp;
                }

                Dictionary<string, string> list = new Dictionary<string, string>();
                //FileInfo tempFi;
                ////Get Temp Folder Name
                //var downloadFolder = LookupHelper.GetConfigValueByName("UI_UploadPath");
                ////LookupHelper.GetConfigValueByName("UI_UploadPath");
                //var tempDownloadPath = Path.Combine(downloadFolder, "TempPDF", DateTime.Now.ToString("yyyyMMddHHmmss"));

                var resultList = ConnectionHelper.QueryDataBySQL(
                    string.Format("Select * From {0} where TestHeaderId = {1}", tableName, testheaderId), isCheckAthena);

                if (string.IsNullOrEmpty(fileNameItem.ARCHIVEFOLDER) == false)
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
 
                    //if (Directory.Exists(tempDownloadPath) == false)
                    //        Directory.CreateDirectory(tempDownloadPath);
                    //awsHelper.DownloadDirectory_from_s3(bucketName, awsFilePath, tempDownloadPath);
                    //foreach (var item in Directory.GetFiles(Path.Combine(tempDownloadPath, testheaderId)))
                    //{
                    //    tempFi = new FileInfo(item);
                    //    list.Add(tempFi.Name, tempFi.FullName);
                    //}
                    //20221212 Temp for comment
                    list = awsHelper.GenerateAllURL_from_s3(bucketName, awsFilePath);
                }

                Utils.FileHelper.FileExistChecker(resultList, list, isCheckAthena);

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
        /// GetAllTestFilesMulti
        /// </summary>
        /// <param name="APIConnectionName"></param>
        /// <param name="testheaderIds">Sperate by comma (,)</param>
        /// <returns></returns>
        [HttpGet]
        [Route("GetAllTestFilesMulti/{APIConnectionName}/{testheaderIds}")]
        public HttpResponseMessage GetAllTestFilesMulti(string APIConnectionName, string testheaderIds)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                ConnectionHelper conHelper = new ConnectionHelper(APIConnectionName);
                string sql = string.Format(@"SELECT * FROM TESTHEADERATTACHMENT_V WHERE TESTHEADERID IN ({0}) ORDER BY TESTHEADERID DESC ", testheaderIds);

                //Get from Oracle first time
                var fileNameItems = Utils.FileHelper.GetTestFilesByTestHeaderIds(sql, false);
                //Table data has been clean up, need to retrive back from Athena
                var athenaTestHeaderIds = fileNameItems.Where(r => r.TDSTABLECLEANUPSTATUS == true).Select(r => r.TESTHEADERID).Distinct();
                if (athenaTestHeaderIds.Count() > 0)
                {
                    sql = string.Format(@"SELECT * FROM TESTHEADERATTACHMENT_V WHERE TESTHEADERID IN ({0}) ORDER BY TESTHEADERID DESC ",
                        string.Join(",", athenaTestHeaderIds.ToList()));
                    fileNameItems.AddRange(Utils.FileHelper.GetTestFilesByTestHeaderIds(sql, true));
                }

                if (fileNameItems == null || fileNameItems.Count() == 0)
                {
                    resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotFound, "Data Not Exists in database");
                    return resp;
                }

                //File has been archived, need to get URL from S3         
                if (fileNameItems.Where(r => r.ARCHIVEFILECLEANUPSTATUS == true).Count() > 0)
                {
                    AWSS3Helper awsHelper = new AWSS3Helper();
                    var bucketName = LookupHelper.GetConfigValueByName("Archive_BucketName"); //lum-tds
                    var awsFolderName = LookupHelper.GetConfigValueByName("AWSFolderPath"); //Dev
                    Dictionary<string, string> list = new Dictionary<string, string>();

                    fileNameItems.AsParallel().ForAll(item =>
                    {
                        if (item.ARCHIVEFILECLEANUPSTATUS)
                        {
                            List<string> archiveChars = item.ARCHIVEFILENAME.Split('\\').ToList();
                            int startIdx = archiveChars.IndexOf("Archive");
                            string[] s3Chars = archiveChars.Where(x => archiveChars.IndexOf(x) > startIdx).ToArray();
                            var awsFilePath = awsFolderName + "/" + string.Join("/", s3Chars);

                            item.ARCHIVEFILENAME = awsHelper.GenerateFileURL_from_s3(bucketName, awsFilePath);
                        }
                    });
                }

                var rtnObj = new { OutputList = fileNameItems };
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
            ConnectionHelper conHelper = new ConnectionHelper(string.Empty);

            try
            {
                var apiVersion = Assembly.GetExecutingAssembly().GetName().Version;
                var ingestionDllVer = AppDomain.CurrentDomain.GetAssemblies().Where(r => r.FullName.StartsWith("IngestionDLL")).FirstOrDefault().GetName().Version;
                var dataUploadDllVer = AppDomain.CurrentDomain.GetAssemblies().Where(r => r.FullName.StartsWith("TDS_Data_Upload")).FirstOrDefault().GetName().Version;
                var rtnObj = new { APIVersion = apiVersion.ToString(), IngestionDLLVersion = ingestionDllVer.ToString(), DataUploaderDLLVersion = dataUploadDllVer.ToString() };

                //ExtensionHelper.LogDLLVersion(apiVersion.ToString(), ingestionDllVer.ToString(), dataUploadDllVer.ToString(), Url.Content("~/"));
                resp = ExtensionHelper.LogAndResponse(new ObjectContent<dynamic>(rtnObj, new JsonMediaTypeFormatter()));
            }
            catch (Exception ex)
            {
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.Conflict, ExtensionHelper.GetAllFootprints(ex), ex);
                ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, "GetAllDLLVersion", 2, "ExpLog", null);
            }
            return resp;
        }

        /// <summary>
        /// Get All Dll Version
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("LogAllDLLVersion")]
        public HttpResponseMessage LogAllDLLVersion()
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);

            try
            {
                var apiVersion = Assembly.GetExecutingAssembly().GetName().Version;
                var ingestionDllVer = AppDomain.CurrentDomain.GetAssemblies().Where(r => r.FullName.StartsWith("IngestionDLL")).FirstOrDefault().GetName().Version;
                var dataUploadDllVer = AppDomain.CurrentDomain.GetAssemblies().Where(r => r.FullName.StartsWith("TDS_Data_Upload")).FirstOrDefault().GetName().Version;
                var uiVersion = ExtensionHelper.GetUIVersion();
                var rtnObj = new { APIVersion = apiVersion.ToString(), IngestionDLLVersion = ingestionDllVer.ToString(), DataUploaderDLLVersion = dataUploadDllVer.ToString() };

                ExtensionHelper.LogDLLVersion(uiVersion, apiVersion.ToString(), ingestionDllVer.ToString(),
                    dataUploadDllVer.ToString(), Url.Content("~/"));
                resp = ExtensionHelper.LogAndResponse(new ObjectContent<dynamic>(rtnObj, new JsonMediaTypeFormatter()));
            }
            catch (Exception ex)
            {
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.Conflict, ExtensionHelper.GetAllFootprints(ex), ex);
                ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, "GetAllDLLVersion", 2, "ExpLog", null);
            }
            return resp;
        }

        /// <summary>
        /// Call Data Ingestion
        /// </summary>
        /// <param name="apiConnName"></param>
        /// <param name="tableId"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get All Packages By API Connection Name
        /// </summary>
        /// <param name="apiConnName">API Connection Name</param>
        /// <param name="format">Return Format</param>
        /// <returns></returns>
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

                var res = ConnectionHelper.QueryDataBySQL(sql, false);
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
        [Route("GenerateTestReport/{testHeaderId}/{isAddImage}")]
        public HttpResponseMessage GenerateTestReport(string testHeaderId, bool isAddImage = true)
        {
            ConnectionHelper conHelper = new ConnectionHelper(string.Empty);
            TestReportHelper pDFHelper = new TestReportHelper();
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                var stream = pDFHelper.CreateTestReport(testHeaderId, isAddImage);
                if (stream.ToArray().Count() > 0)
                {
                    resp.Content = new ByteArrayContent(stream.ToArray());
                    resp.Content.Headers.ContentDisposition =
                        new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                        {
                            FileName = string.Format("TestReport_{0}.pdf", testHeaderId)
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

        /// <summary>
        /// TestFileDownloader, will create individual folder for access user and service account
        /// </summary>
        /// <param name="apiConnName">API Connection Name</param>
        /// <param name="testHeaderIdList">TestHeaderId List</param>
        /// <param name="userMail">User Email Address</param>
        /// <returns></returns>        
        [HttpPost]
        [Route("TestFileDownloader/{apiConnName}")]
        public HttpResponseMessage TestFileDownloader(string apiConnName, List<string> testHeaderIdList,
            string userMail, bool forceFromS3 = false)
        {
            HttpResponseMessage resp = new HttpResponseMessage(HttpStatusCode.OK);
            ConnectionHelper conHelper = new ConnectionHelper(apiConnName);
            try
            {
                var empId = AccountHelper.GetEmpIdByMail(userMail).FirstOrDefault();

                if (string.IsNullOrEmpty(empId) == false)
                {
                    DirectorySecurity ds = new DirectorySecurity();
                    //LogHelper.WriteLine("Access User Name: " + string.Format(@"LI\{0}", empId));
                    //LogHelper.WriteLine("System.Security.Principal.WindowsIdentity.GetCurrent().Name: " 
                    //    + System.Security.Principal.WindowsIdentity.GetCurrent().Name);

                    ds.AddAccessRule(new FileSystemAccessRule(string.Format(@"LI\{0}", empId),
                        FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                        PropagationFlags.None, AccessControlType.Allow));
                    ds.AddAccessRule(new FileSystemAccessRule(System.Security.Principal.WindowsIdentity.GetCurrent().Name,
                        FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                        PropagationFlags.None, AccessControlType.Allow));

                    var tempFolder = ConfigurationManager.AppSettings["TestFilesTempFolder"];
                    string downloadFolder = Path.Combine(tempFolder,
                        string.Format("{0}\\{1}", empId.ToUpper(), DateTime.Now.ToString("yyyyMMddHHmm")));

                    if (Directory.Exists(downloadFolder) == false)
                    {
                        Directory.CreateDirectory(downloadFolder);
                    }
                    string sql = string.Format(@"SELECT H.TESTHEADERID, D.ARCHIVEFILECLEANUPSTATUS,
                                            H.ARCHIVELOCATION ARCHIVEFOLDER, ATTACH.FILENAME FROM
                                            (SELECT ARCHIVEFILENAME, FILENAME, TESTHEADERID FROM TESTHEADER_V
                                                UNION
                                            SELECT ARCHIVEFILENAME, FILENAME, TESTHEADERID FROM IMAGEDATA_V
                                                UNION
                                            SELECT ARCHIVEFILENAME, FILENAME, TESTHEADERID FROM ATTACHMENTDATA_V
                                                UNION
                                            SELECT ARCHIVEFILENAME, FILENAME, TESTHEADERID FROM TABLEDATA_V) ATTACH
                                            INNER JOIN TESTHEADER_V H ON ATTACH.TESTHEADERID = H.TESTHEADERID
                                            LEFT JOIN DATARETENTIONLOG D ON H.TESTHEADERID = D.TESTHEADERID
                                            WHERE ATTACH.TESTHEADERID in ({0}) ", string.Join(", ", testHeaderIdList));

                    //LogHelper.WriteLine("Query data: " + sql);
                    List<TestFileDownloadClass> processFileList = new List<TestFileDownloadClass>();
                    List<Stream> streamList = new List<Stream>();
                    using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                    {
                        processFileList = sqlConn.Query<TestFileDownloadClass>(sql).ToList();
                    }

                    Stopwatch timer = new Stopwatch();
                    timer.Reset();
                    timer.Start();
                    if (processFileList.Count() > 0)
                    {
                        AWSS3Helper awsHelper = new AWSS3Helper();
                        var bucketName = LookupHelper.GetConfigValueByName("Archive_BucketName"); //lum-tds
                        var awsFolderName = LookupHelper.GetConfigValueByName("AWSFolderPath"); //Dev

                        //var testHeaderFolderList = processFileList.Select(r => r.ARCHIVEFOLDER).Distinct().ToList();
                        var testHeaderFolderList = processFileList.
                                                   Select(r => new { r.TESTHEADERID, r.ARCHIVEFILECLEANUPSTATUS, r.ARCHIVEFOLDER }).Distinct().ToList();

                        testHeaderFolderList.AsParallel().ForAll(item =>
                        {
                            List<string> archiveChars = item.ARCHIVEFOLDER.Split('\\').ToList();
                            int startIdx = archiveChars.IndexOf("Archive");
                            string[] s3FolderChars = archiveChars.Where(x => archiveChars.IndexOf(x) > startIdx).ToArray();
                            //From S3
                            if (item.ARCHIVEFILECLEANUPSTATUS || forceFromS3 == true)
                            {
                                //LogHelper.WriteLine("Get From S3: " + item.TESTHEADERID);
                                var awsFilePath = awsFolderName + "/" + string.Join("/", s3FolderChars);
                                awsHelper.DownloadDirectory_from_s3(bucketName, awsFilePath, downloadFolder);
                            }
                            else
                            {
                                //LogHelper.WriteLine("Get From Local: " + item.TESTHEADERID);
                                //From Local
                                var subFolder = Path.Combine(downloadFolder, item.TESTHEADERID);
                                if (Directory.Exists(subFolder) == false)
                                    Directory.CreateDirectory(subFolder);
                                processFileList.Where(r => r.TESTHEADERID == item.TESTHEADERID).AsParallel().ForAll(file =>
                                {
                                    var fromFile = Path.Combine(file.ARCHIVEFOLDER, file.FILENAME);
                                    if (File.Exists(fromFile))
                                    {
                                        File.Copy(fromFile, Path.Combine(subFolder, file.FILENAME));
                                    }
                                    else
                                    {
                                        LogHelper.WriteLine(string.Format(@"Missing [ARCHIVELOCATION] info: {0}, TestHeaderId: {1}", fromFile, item.TESTHEADERID));
                                    }
                                });                                
                            }
                        });
                    }

                    timer.Stop();
                    var exeSec = timer.Elapsed.TotalSeconds.ToString();
                    var fileSize = ExtensionHelper.DirSize(new DirectoryInfo(downloadFolder));
                    //LogHelper.WriteLine("------------1. Is From S3: " + isFromS3);
                    //LogHelper.WriteLine("------------1. Download Time: " + exeSec + "s");
                    //LogHelper.WriteLine("------------2. Folder size = {0} bytes: " + ExtensionHelper.DirSize(new DirectoryInfo(downloadFolder)));

                    if (Directory.GetFiles(downloadFolder, "*.*", SearchOption.AllDirectories).Count() > 0)
                    {
                        //ZipFile.CreateFromDirectory(downloadFolder, downloadZipPath);
                        Directory.SetAccessControl(downloadFolder, ds);
                        var res = new
                        {
                            FilePath = downloadFolder,
                            FileCount = Directory.GetFiles(downloadFolder, "*.*", SearchOption.AllDirectories).Count(),
                            ExeSec = exeSec,
                            FileSizeBytes = fileSize
                        };
                        resp = ExtensionHelper.LogAndResponse(new ObjectContent<object>(res, new JsonMediaTypeFormatter()));
                        //new DirectoryInfo(downloadFolder).Delete(true);
                        var requestorMails = new List<string>() { userMail };
                        var downloadSubject = ConfigurationManager.AppSettings["mailTitle"].Trim() 
                            + " - Test File Download Request";
                        var isSend = MailHelper.SendMail(string.Empty, requestorMails, downloadSubject,
                            MailHelper.BuildTestFileDownloadMail(downloadFolder, true), true);
                    }
                    else
                    {
                        resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotFound, "No data found", null);
                    }
                }
                else
                {
                    resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotFound,
                        "User account not existing in system. User Email: " + userMail, null);
                }                
            }
            catch (Exception ex)
            {
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.InternalServerError, ExtensionHelper.GetAllFootprints(ex), ex);
            }
            //finally
            //{
            //    Directory.Delete(downloadFolder);
            //    File.Delete(downloadZipPath);
            //}

            return resp;
        }
    }
}
