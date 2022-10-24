using Dapper;
using KaizenTDSMvcAPI.Models;
using KaizenTDSMvcAPI.Models.Classes;
using KaizenTDSMvcAPI.Models.KaizenTDSClasses;
using KaizenTDSMvcAPI.Providers;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data.Odbc;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using SystemLibrary.Utility;
using ICSharpCode.SharpZipLib.Zip;

namespace KaizenTDSMvcAPI.Utils
{
    public class FileHelper
    {
        public static string FileUploadPath = ConfigurationManager.AppSettings["FileUploadPath"];
        public static string UploadFileName;
        public static async Task<bool> FileUploadAsync(ApiController apiCtrl, string folderName = null)
        {
            UploadFileName = string.Empty;
            string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempUploadFolder");
            try
            {
                if (!apiCtrl.Request.Content.IsMimeMultipartContent())
                {
                    ExtensionHelper.LogAndResponse(new StringContent("File not found!"), System.Net.HttpStatusCode.UnsupportedMediaType);
                    return false;
                }

                var uiUpdPath = LookupHelper.GetConfigValueByName("UI_UploadPath");
                if (string.IsNullOrEmpty(uiUpdPath) == false)
                {
                    root = uiUpdPath;
                }

                if (string.IsNullOrEmpty(folderName) == false)
                {
                    root = Path.Combine(uiUpdPath, folderName);
                }

                if (Directory.Exists(root) == false)
                {
                    Directory.CreateDirectory(root);
                }

                IEnumerable<HttpContent> files = null;
                Task.Factory
                    .StartNew(() => files = apiCtrl.Request.Content.ReadAsMultipartAsync().Result.Contents,
                        CancellationToken.None,
                        TaskCreationOptions.LongRunning, // guarantees separate thread
                        TaskScheduler.Default)
                    .Wait();

                List<UploadResponse> uploadResponseList = new List<UploadResponse>();
                UploadResponse ur = null;
                FileInfo fi;
                string newFilePath = string.Empty;
                string newFileName = string.Empty;
                foreach (var content in files)
                {
                    ur = new UploadResponse();
                    var fileName = content.Headers.ContentDisposition.FileName.Trim('\"');
                    var fileBytes = await content.ReadAsByteArrayAsync();

                    var outputPath = Path.Combine(root, fileName);
                    if (File.Exists(outputPath))
                    {
                        File.Delete(outputPath);
                    }
                    using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    {
                        output.Write(fileBytes, 0, fileBytes.Length);
                    }
                    LogHelper.WriteLine(fileName);
                    //Create tempFile
                    if (File.Exists(outputPath))
                    {
                        fi = new FileInfo(outputPath);
                        var ext = fi.Extension;
                        UploadFileName = fi.FullName;
                    }
                }

                return true;
            }
            catch (Exception ex )
            {
                throw ex;
            }
        }

        public static string DownloadUserPhoto(string employeeId, string fileType)
        {
            string trgPath = ConfigurationManager.AppSettings["UserPhotoPath"];
            //string photoType = ConfigurationManager.AppSettings["PhotoType"];
            string saveFileName = Path.Combine(trgPath, string.Format("{0}.{1}", employeeId, fileType));
            ACCESSCONTROLEntities acEntity = new ACCESSCONTROLEntities();

            try
            {
                var empNum = 0;
                int.TryParse(employeeId.Substring(3, 5), out empNum);
                if (empNum > 0)
                {
                    var empMainList = from emp in acEntity.EMPs
                                      join udfEmp in acEntity.UDFEMPs on emp.ID equals udfEmp.ID
                                      where udfEmp.EMPLOYEENUM == empNum
                                      select emp.ID;

                    if (empMainList.Count() > 0)
                    {
                        var empObj = from obj in acEntity.MMOBJS
                                     where obj.EMPID == empMainList.FirstOrDefault() && obj.TYPE == 2
                                     select obj;
                        if (empObj.Count() > 0)
                        {
                            var res = byteArrayToImage(empObj.FirstOrDefault().LNL_BLOB);
                            res.Save(saveFileName);
                            return saveFileName;
                        }
                        else
                        {
                            LogHelper.WriteLine(string.Format("EmployeeId: {0}/EmployeeNum: {1} not found in MMOBJS table!",
                                employeeId, empNum));
                        }
                    }
                    else
                    {
                        LogHelper.WriteLine(string.Format("EmployeeId: {0}/EmployeeNum: {1} not found in EMPs or UDFEMPs table!",
                            employeeId, empNum));
                    }
                }
                else
                {
                    LogHelper.WriteLine(string.Format("EmployeeId: {0}/EmployeeNum: {1} error!",
                        employeeId, empNum));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return string.Empty;
        }

        private static Image byteArrayToImage(byte[] byteArrayIn)
        {
            MemoryStream ms = new MemoryStream(byteArrayIn);
            ms.Position = 0;
            Image returnImage = Image.FromStream(ms);
            
            return (Image)returnImage.Clone();
        }

        private static byte[] ScaleImageByPercent(byte[] imageBuffer, int Percent)
        {

            using (Stream imageStream = new MemoryStream(imageBuffer))
            {
                using (Image scaleImage = Image.FromStream(imageStream))
                {
                    float scalePercent = ((float)Percent / 100);

                    int originalWidth = scaleImage.Width;
                    int originalHeight = scaleImage.Height;
                    int originalXPoint = 0;
                    int originalYPoint = 0;

                    int scaleXPoint = 0;
                    int scaleYPoint = 0;
                    int scaleWidth = (int)(originalWidth * scalePercent);
                    int scaleHeight = (int)(originalHeight * scalePercent);

                    using (Bitmap scaleBitmapImage = new Bitmap(scaleWidth, scaleHeight, PixelFormat.Format24bppRgb))
                    {
                        scaleBitmapImage.SetResolution(scaleImage.HorizontalResolution, scaleImage.VerticalResolution);
                        Graphics graphicImage = Graphics.FromImage(scaleBitmapImage);
                        graphicImage.CompositingMode = CompositingMode.SourceCopy;
                        graphicImage.InterpolationMode = InterpolationMode.NearestNeighbor;
                        graphicImage.DrawImage(scaleImage,
                            new Rectangle(scaleXPoint, scaleYPoint, scaleWidth, scaleHeight),
                            new Rectangle(originalXPoint, originalYPoint, originalWidth, originalHeight),
                            GraphicsUnit.Pixel);
                        graphicImage.Dispose();

                        ImageConverter converter = new ImageConverter();
                        return (byte[])converter.ConvertTo(scaleBitmapImage, typeof(byte[]));
                    }
                }
            }
        }

        public static FileNameClass GetFileNameByTestHeaderId(string sql, bool isCheckAthena)
        {
            string fullFilePath = string.Empty;

            try
            {
                List<FileNameClass> list = new List<FileNameClass>();
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                {
                    if (isCheckAthena)
                    {
                        //20210601 Jacky Add Athena Query function
                        var athenaSchema = LookupHelper.GetConfigValueByName("KaizenTDSAthenaSchema").ToUpper();
                        if (string.IsNullOrEmpty(athenaSchema) == false)
                        {
                            sql = ConnectionHelper.AthenaSQLSchemaModification(sql, athenaSchema);
                            var athenaConnStr = LookupHelper.GetConfigValueByName("KaizenTDSAthenaConn");
                            using (var atConn = new OdbcConnection(athenaConnStr))
                            {
                                list = atConn.Query<FileNameClass>(sql).ToList();
                            }
                        }
                    }
                    else
                    {
                        list = sqlConn.Query<FileNameClass>(sql).ToList();
                    }
                }

                var fileNameObj = list.FirstOrDefault();
                return fileNameObj;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public static List<TestFileDownloadClass> GetTestFilesByTestHeaderIds(string sql, bool isCheckAthena)
        {
            string fullFilePath = string.Empty;

            try
            {
                List<TestFileDownloadClass> list = new List<TestFileDownloadClass>();
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                {
                    if (isCheckAthena)
                    {
                        //20210601 Jacky Add Athena Query function
                        var athenaSchema = LookupHelper.GetConfigValueByName("KaizenTDSAthenaSchema").ToUpper();
                        if (string.IsNullOrEmpty(athenaSchema) == false)
                        {
                            sql = ConnectionHelper.AthenaSQLSchemaModification(sql, athenaSchema);
                            var athenaConnStr = LookupHelper.GetConfigValueByName("KaizenTDSAthenaConn");
                            using (var atConn = new OdbcConnection(athenaConnStr))
                            {
                                list = atConn.Query<TestFileDownloadClass>(sql).ToList();
                            }
                        }
                    }
                    else
                    {
                        list = sqlConn.Query<TestFileDownloadClass>(sql).ToList();
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public static void FileExistChecker(List<dynamic> list, Dictionary<string, string> s3FileList, bool isCheckAthena)
        {
            try
            {
                if (list.Count() == 0) 
                    return;
                var dicList = list.Select(x => x as IDictionary<string, object>).ToList();
                string archiveFileNameKey = isCheckAthena ? "archivefilename" : "ARCHIVEFILENAME"; //Because Athena is using lower case column
                string fileNameKey = isCheckAthena ? "filename" : "FILENAME"; //Because Athena is using lower case column
                //To avoid empty archive folder   
                var archiveLoc = dicList.FirstOrDefault()[archiveFileNameKey].ToString().Replace(dicList.FirstOrDefault()[fileNameKey].ToString(), "");
                var archiveFolderFiles = Directory.Exists(archiveLoc) ? Directory.GetFiles(archiveLoc).ToList() : new List<string>();
                foreach (var item in dicList)
                {
                    var archiveFN = item[archiveFileNameKey].ToString();//archiveFNItem.Where(r => r.Key == "ARCHIVEFILENAME").FirstOrDefault();
                    var fileName = item[fileNameKey].ToString();//archiveFNItem.Where(r => r.Key == "FILENAME").FirstOrDefault();
                    if (archiveFolderFiles.Contains(archiveFN) == false)
                    {
                        var s3File = s3FileList.Where(r => r.Key.Contains(fileName)).FirstOrDefault().Value;
                        if (string.IsNullOrEmpty(s3File) == false)
                            item[archiveFileNameKey] = s3File;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLine(ex.ToString());
                throw ex;
            }
        }
        public static byte[] ReadStream(Stream responseStream)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        public class MultipartContent
        {
            public FileData[] files;
        }
        public class FileData
        {
            public byte[] Content;
            public string Name;
        }

        /// <summary>
        /// compress mutiple files and return a compressed stream
        /// </summary>
        /// <param name="streams">
        /// key is file name
        /// value is compressed stream
        /// </param>
        /// <returns>return a stream compressed</returns>
        public static Stream PackageManyZip(Dictionary<string, Stream> streams)
        {
            byte[] buffer = new byte[6500];
            MemoryStream returnStream = new MemoryStream();
            var zipMs = new MemoryStream();

            using (ZipOutputStream zipStream = new ZipOutputStream(zipMs))
            {
                zipStream.SetLevel(9);
                foreach (var kv in streams)
                {
                    string fileName = kv.Key;
                    using (var streamInput = kv.Value)
                    {
                        zipStream.PutNextEntry(new ZipEntry(fileName));
                        while (true)
                        {
                            var readCount = streamInput.Read(buffer, 0, buffer.Length);
                            if (readCount > 0)
                            {
                                zipStream.Write(buffer, 0, readCount);
                            }
                            else
                            {
                                break;
                            }
                        }
                        zipStream.Flush();
                    }
                }

                zipStream.Finish();
                zipMs.Position = 0;
                zipMs.CopyTo(returnStream, 5600);
            }

            returnStream.Position = 0;
            return returnStream;
        }
    }
}