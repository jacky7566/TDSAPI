using Amazon.Runtime.Internal.Util;
using Dapper;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using iTextSharp.text;
using iTextSharp.text.pdf;
using KaizenTDSMvcAPI.Models.TestReportClasses;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using System.Xml.XPath;
using SystemLibrary.Utility;

namespace KaizenTDSMvcAPI.Utils
{
    public class TestReportHelper
    {
        //可以是任何 Stream 類型 (MemoryStream、FileStream、OutputStream)
        //private MemoryStream stream = new MemoryStream();
        private BaseFont bfTimes = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false);
        private Document document;
        /// <summary>
        /// Return TestHeader
        /// </summary>
        public dynamic _TestHeader;
        private string _testHeaderId;
        private string _deviceId;
        private bool _isAddImage;

        public string GetNewFilePath(string fileExt)
        {
            var uiUpdPath = @"C:\Users\lic67888\Documents\00 Doc\Temp\PDF_Test";
            //var uiUpdPath = LookupHelper.GetConfigValueByName("UI_UploadPath");
            uiUpdPath = Path.Combine(uiUpdPath, "PDF");
            if (Directory.Exists(uiUpdPath) == false)
                Directory.CreateDirectory(uiUpdPath);

            var filePath = string.Format("{0}\\{1}{2}",
                uiUpdPath, Guid.NewGuid().ToString(), fileExt);
            return filePath;
        }

        public FileStream CreatePDF2(string filePath)
        {
            //MemoryStream stream = new MemoryStream();
            var stream = new FileStream(filePath, FileMode.Create);
            try
            {
                using (this.document = new Document())
                {
                    using (var writer = PdfWriter.GetInstance(this.document, stream))
                    {
                        this.document.Open();
                        iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(@"C:\Users\lic67888\Documents\50 Source\KaizenTDSMvcAPI\KaizenTDSMvcAPI\Images\Logo.jpg");
                        Paragraph para = new Paragraph();
                        para.Leading = 15;
                        //調整圖片大小
                        image.ScalePercent(50f);
                        image.Alignment = Element.ALIGN_RIGHT;//Image在paragraph中的話，可以設定left, center, right
                        para.Add(image);
                        this.document.Add(para);
                        this.document.AddTitle("Test");
                        //操作 PDF ...
                        this.document.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            //建立文件
            
            return stream;
        }

        public MemoryStream CreateTestReport(string testHeaderId, bool isAddImage = true)
        {
            this._testHeaderId = testHeaderId;
            this._isAddImage = isAddImage;
            _TestHeader = new TestHeader();
            var stream = new MemoryStream();
            Font titleFT = new Font(this.bfTimes, 10, 1);
            IDictionary<string, object> tempItem;
            try
            {
                using (this.document = new Document())
                {
                    this.document.SetPageSize(iTextSharp.text.PageSize.A4.Rotate());
                    using (var writer = PdfWriter.GetInstance(this.document, stream))
                    {
                        this.document.Open();
                        this.SetPDFTitle();
                        this.document.Add(new Paragraph("\n"));
                        this.document.Add(new Paragraph("\n"));
                        //TEST HEADER
                        this.document.Add(new Paragraph("TEST HEADER", titleFT));
                        this.SetColumnwiseTable(this.GetTestHeader());
                        if (this._TestHeader != null)
                        {
                            //DEVICE RELATIONS
                            this.document.Add(new Paragraph("DEVICE RELATIONS", titleFT));
                            this.SetPureTable(this.GetDeviceReleations());
                            //TEST HEADER MISC
                            this.document.Add(new Paragraph("TEST HEADER MISC", titleFT));
                            this.SetPureTable(this.GetTestHeaderMisc());
                            //this.SetTestHeaderParameters(); No use
                            //TEST STEP DATA
                            this.SetTestHeaderSteps();
                            this.document.AddTitle("Test");
                        }
                        //操作 PDF ...
                        this.document.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            //建立文件
            
            return stream;
        }

        #region SetContent
        private void SetPDFTitle()
        {
            try
            {
                this.document.Add(new Paragraph("Test Report", new Font(this.bfTimes, 12, 1)));
                string imagePath = System.AppDomain.CurrentDomain.BaseDirectory + "\\Images\\Logo.jpg";
                iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(imagePath);
                Paragraph para = new Paragraph();
                para.Leading = 15;
                //調整圖片大小
                image.ScalePercent(30f);
                image.Alignment = Element.ALIGN_RIGHT;//Image在paragraph中的話，可以設定left, center, right
                para.Add(image);
                this.document.Add(para);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        private void SetColumnwiseTable(dynamic dynamicInput, List<string> colHeaderList = null)
        {
            if (dynamicInput == null) return;
            Font bondFt = new Font(this.bfTimes, 6, 1);
            Font contentFt = new Font(this.bfTimes, 6);
            Font passBlueFT = new Font(this.bfTimes, 6, BaseColor.BLUE.RGB);
            Font passGreenFT = new Font(this.bfTimes, 6, BaseColor.GREEN.RGB);            
            try
            {
                var inputDic = dynamicInput as IDictionary<string, object>;
                PdfPTable headerTable = new PdfPTable(colHeaderList == null ? 2 : colHeaderList.Count());
                headerTable.WidthPercentage = 100f;
                headerTable.HorizontalAlignment = Element.ALIGN_LEFT;
                //Set Column Name (Header)
                if (colHeaderList != null && colHeaderList.Count() > 0)
                {                    
                    foreach (var colName in colHeaderList)
                    {
                        headerTable.AddCell(new PdfPCell(new Phrase(colName, bondFt)));
                    }
                }
                //Content
                foreach (var kvp in inputDic)
                {
                    if (kvp.Value == null || kvp.Value.ToString() == "0" || kvp.Value.ToString() == "")
                        continue;

                    if (kvp.Key == "DEVICEID") //Get Device Id for Device Releations
                        this._deviceId = kvp.Value.ToString();
                    
                    headerTable.AddCell(new PdfPCell(new Phrase(kvp.Key.ToString(), contentFt)));

                    //Content
                    headerTable.AddCell(new PdfPCell(new Phrase(kvp.Value.ToString(), contentFt)));
                }
                this.document.Add(new Paragraph("\n"));
                this.document.Add(headerTable);
                this.document.Add(new Paragraph("\n"));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void SetPureTable(List<dynamic> inputList, bool hasBoldHeader = false)
        {
            if (inputList.Any() == false) return;
            Font bondFt = new Font(this.bfTimes, 6, 1);
            Font contentFt = new Font(this.bfTimes, 6);
            Font passBlueFT = FontFactory.GetFont("Arial", 6, new iTextSharp.text.BaseColor(0, 128, 255));
            Font passGreenFT = FontFactory.GetFont("Arial", 6, new iTextSharp.text.BaseColor(0, 153, 0));
            Font failRedFT = FontFactory.GetFont("Arial", 6, iTextSharp.text.BaseColor.RED);
            PdfPCell pdfPCell;
            try
            {
                var list = inputList.Select(x => x as IDictionary<string, object>).ToList();
                PdfPTable headerTable = new PdfPTable(list.FirstOrDefault().Keys.Count());
                headerTable.WidthPercentage = 100f;
                headerTable.HorizontalAlignment = Element.ALIGN_LEFT;
                //Header
                foreach (var kvp in list.FirstOrDefault())
                {
                    pdfPCell = new PdfPCell(new Phrase(kvp.Key, bondFt));
                    if (hasBoldHeader)
                    {
                        pdfPCell.BackgroundColor = new BaseColor(System.Drawing.Color.Gray);
                        pdfPCell.BorderColor = new BaseColor(System.Drawing.Color.Black);
                    }
                    headerTable.AddCell(pdfPCell);
                }
                //Content
                //var temp = new IDictionary<string, object>;
                string tempVal;
                foreach (var item in list) //All records
                {
                    var dic = item as IDictionary<string, object>;
                    foreach (var kvp in dic) //Key Value Row data
                    {
                        tempVal = kvp.Value != null ? kvp.Value.ToString() : string.Empty;
                        if (kvp.Key == "STATUS" && tempVal == "Passed")
                            headerTable.AddCell(new PdfPCell(new Phrase(tempVal, passBlueFT)));                        
                        else if (kvp.Key == "RESULT" && tempVal == "Passed")
                            headerTable.AddCell(new PdfPCell(new Phrase(tempVal, passGreenFT)));
                        else if (tempVal == "Failed")
                            headerTable.AddCell(new PdfPCell(new Phrase(tempVal, failRedFT)));
                        else
                            headerTable.AddCell(new PdfPCell(new Phrase(tempVal, contentFt)));
                    }
                }
                this.document.Add(new Paragraph("\n"));
                this.document.Add(headerTable);
                this.document.Add(new Paragraph("\n"));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void SetPureTableByDicList(List<IDictionary<string, object>> list, bool hasBoldHeader = false, bool hideHeaderID = true)
        {
            if (list.Any() == false) return;
            Font bondFt = new Font(this.bfTimes, 6, 1);
            Font contentFt = new Font(this.bfTimes, 6);
            Font passBlueFT = FontFactory.GetFont("Arial", 6, new iTextSharp.text.BaseColor(0, 128, 255));
            Font passGreenFT = FontFactory.GetFont("Arial", 6, new iTextSharp.text.BaseColor(0, 153, 0));
            Font failRedFT = FontFactory.GetFont("Arial", 6, iTextSharp.text.BaseColor.RED);
            PdfPCell pdfPCell;
            try
            {
                //var list = inputList.Select(x => x as IDictionary<string, object>).ToList();
                PdfPTable headerTable = new PdfPTable(list.FirstOrDefault().Keys.Count());
                headerTable.WidthPercentage = 100f;
                headerTable.HorizontalAlignment = Element.ALIGN_LEFT;
                //Header
                foreach (var kvp in list.FirstOrDefault())
                {
                    pdfPCell = new PdfPCell(new Phrase(kvp.Key, bondFt));
                    if (hasBoldHeader)
                    {
                        pdfPCell.BackgroundColor = new BaseColor(System.Drawing.Color.Gray);
                        pdfPCell.BorderColor = new BaseColor(System.Drawing.Color.Black);
                    }
                    headerTable.AddCell(pdfPCell);
                }
                //Content
                //var temp = new IDictionary<string, object>;
                string tempVal;
                foreach (var item in list) //All records
                {
                    var dic = item as IDictionary<string, object>;
                    foreach (var kvp in dic) //Key Value Row data
                    {
                        tempVal = kvp.Value != null ? kvp.Value.ToString() : string.Empty;
                        if (kvp.Key == "STATUS" && tempVal == "Passed")
                            headerTable.AddCell(new PdfPCell(new Phrase(tempVal, passBlueFT)));
                        else if (kvp.Key == "RESULT" && tempVal == "Passed")
                            headerTable.AddCell(new PdfPCell(new Phrase(tempVal, passGreenFT)));
                        else if (tempVal == "Failed")
                            headerTable.AddCell(new PdfPCell(new Phrase(tempVal, failRedFT)));
                        else
                            headerTable.AddCell(new PdfPCell(new Phrase(tempVal, contentFt)));
                    }
                }
                this.document.Add(new Paragraph("\n"));
                this.document.Add(headerTable);
                this.document.Add(new Paragraph("\n"));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void SetTestHeaderSteps()
        {
            Font titleFT = new Font(this.bfTimes, 10, 1);            
            string testHeaderStepId = string.Empty;
            try
            {
                var testHeaderStepList = this.GetTestHeaderSteps().Select(x => x as IDictionary<string, object>).ToList();
                var mList = this.GetMesurements();
                var smList = this.GetStringMesurements();
                var testFileList = this.GetAllTestFiles();
                var arrayDataList = this.GetArrayDatas();
                var tempList = new List<IDictionary<string, object>>();
                foreach (var item in testHeaderStepList)
                {
                    if (item == null) continue;                    
                    testHeaderStepId = item["TESTHEADERSTEPID"].ToString();
                    LogHelper.WriteLine("TestHeaderStepId: " + testHeaderStepId);
                    this.document.Add(new Paragraph("TEST STEP DATA: " + testHeaderStepId, titleFT));
                    this.SetPureTable(new List<dynamic>() { item }, true);
                    //--> PARAMETRIC DATA
                    if (mList.Any())
                    {
                        tempList = mList.Select(x => x as IDictionary<string, object>).ToList()
                            .Where(r => r["TESTHEADERSTEPID"].ToString() == testHeaderStepId).Take(50).ToList();
                        if (tempList.Any())
                        {
                            this.document.Add(new Paragraph("PARAMETRIC DATA", titleFT));
                            this.SetPureTableByDicList(tempList);
                        }
                    }

                    //--> STRING DATA
                    if (smList.Any())
                    {
                        tempList = smList.Select(x => x as IDictionary<string, object>).ToList()
                            .Where(r => r["TESTHEADERSTEPID"].ToString() == testHeaderStepId).ToList();
                        if (tempList.Any())
                        {
                            this.document.Add(new Paragraph("STRING DATA", titleFT));
                            this.SetPureTableByDicList(tempList);
                        }
                    }
 
                    //--> PLOT DATA CHART no used
                    //GetAllTestFiles
                    if (testFileList.Any())
                    {
                        tempList = testFileList.Select(x => x as IDictionary<string, object>).ToList()
                            .Where(r => r["TESTHEADERSTEPID"].ToString() == testHeaderStepId).ToList();
                        if (tempList.Any())
                        {
                            var allTestFile = tempList.Select(x => x as IDictionary<string, object>).ToList();
                            //--> IMAGE DATA -- Download Image data back to local
                            if (this._isAddImage)
                            {
                                var imageDataList = allTestFile.Where(r => r["TABLENAME"].ToString() == "IMAGEDATA_V").ToList();
                                if (imageDataList.Any())
                                {
                                    this.document.Add(new Paragraph("IMAGE DATA", titleFT));
                                    this.SetImageDataList(imageDataList);
                                }
                            }
                            //--> TABLE DATA                        
                            var tableDataList = allTestFile.Where(r => r["TABLENAME"].ToString() == "TABLEDATA_V").ToList();
                            if (tableDataList.Any())
                            {
                                this.document.Add(new Paragraph("TABLE DATA", titleFT));
                                this.SetTableDataList(tableDataList);
                            }
                            //--> ATTACHMENT DATA                        
                            var attachmentDataList = allTestFile.Where(r => r["TABLENAME"].ToString() == "ATTACHMENTDATA_V").ToList();
                            if (attachmentDataList.Any())
                            {
                                this.document.Add(new Paragraph("ATTACHMENT DATA", titleFT));
                                this.SetAttachmentDataList(attachmentDataList);
                            }
                        }
                    }
                    
                    //--> ARRAY DATA
                    if (arrayDataList.Any())
                    {
                        tempList = arrayDataList.Select(x => x as IDictionary<string, object>).ToList()
                            .Where(r => r["TESTHEADERSTEPID"].ToString() == testHeaderStepId).ToList();
                        if (tempList.Any())
                        {
                            this.document.Add(new Paragraph("ARRAY DATA", titleFT));
                            this.SetPureTableByDicList(tempList);
                        }
                    }
                    
                    //--> PLOT DATA
                    //this.document.Add(new Paragraph("PLOT DATA", titleFT));
                    //this.SetColumnwiseTable(tempList);

                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        private void SetImageDataList(List<IDictionary<string, object>> list)
        {
            try
            {
                PdfPTable imageTable = new PdfPTable(1);
                PdfPCell imageCell;
                iTextSharp.text.Image img;
                Paragraph nameParagraph;
                //Paragraph conditionParagraph;
                foreach (var item in list)
                {
                    LogHelper.WriteLine("Image: " + item["ARCHIVEFILENAME"].ToString());
                    try
                    {
                        img = iTextSharp.text.Image.GetInstance(item["ARCHIVEFILENAME"].ToString());
                        img.WidthPercentage = 70;
                        img.SetAbsolutePosition(0, 0);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLine(ex.ToString());
                        LogHelper.WriteLine(ex.Message);
                        continue;
                    }
                    
                    if (img != null)
                    {
                        nameParagraph = new Paragraph("NAME: " + item["NAME"].ToString());
                        //conditionParagraph = new Paragraph("CONDITIONS: " + (item["CONDITIONS"] != null 
                        //    ? item["CONDITIONS"].ToString() : string.Empty));                                                
                        //this.document.Add(new Paragraph("\n"));
                        imageCell = new PdfPCell { PaddingLeft = 5, PaddingTop = 5, PaddingBottom = 5, PaddingRight = 5 };
                        imageCell.HorizontalAlignment = Element.ALIGN_CENTER;
                        imageCell.AddElement(img);
                        imageCell.Border = 0;
                        imageCell.AddElement(nameParagraph);
                        //imageCell.AddElement(conditionParagraph);
                        imageTable.AddCell(imageCell);
                        //this.document.Add(new Paragraph("\n"));
                    }
                }
                this.document.Add(new Paragraph("\n"));
                this.document.Add(imageTable);
                this.document.Add(new Paragraph("\n"));
            }
            catch (Exception ex)
            {
                LogHelper.WriteLine(ex.Message);
                throw ex;
            }
        }
        private void SetTableDataList(List<IDictionary<string, object>> list)
        {
            Font bondFt = new Font(this.bfTimes, 6, 1);
            Font contentFt = new Font(this.bfTimes, 6);
            try
            {
                PdfPTable imageTable = new PdfPTable(4);
                imageTable.AddCell(new Paragraph("TABLEID", bondFt));
                imageTable.AddCell(new Paragraph("TABLENAME", bondFt));
                imageTable.AddCell(new Paragraph("FILENAME", bondFt));
                imageTable.AddCell(new Paragraph("ARCHIVEFILENAME", bondFt));
                foreach (var item in list)
                {
                    imageTable.AddCell(new PdfPCell(new Phrase(item["SRC_ID"].ToString(), contentFt))); //TABLEID
                    imageTable.AddCell(new PdfPCell(new Phrase(item["NAME"].ToString(), contentFt))); //TABLENAME
                    imageTable.AddCell(new PdfPCell(new Phrase(item["FILENAME"].ToString(), contentFt))); //FILENAME
                    imageTable.AddCell(new PdfPCell(new Phrase(item["ARCHIVEFILENAME"].ToString(), contentFt))); //ARCHIVEFILENAME
                }
                this.document.Add(new Paragraph("\n"));
                this.document.Add(imageTable);
                this.document.Add(new Paragraph("\n"));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void SetAttachmentDataList(List<IDictionary<string, object>> list)
        {
            Font bondFt = new Font(this.bfTimes, 6, 1);
            Font contentFt = new Font(this.bfTimes, 6);
            try
            {
                PdfPTable imageTable = new PdfPTable(4);
                imageTable.AddCell(new Paragraph("ATTACHMENTNAME", bondFt));
                imageTable.AddCell(new Paragraph("FILENAME", bondFt));
                imageTable.AddCell(new Paragraph("ARCHIVEFILENAME", bondFt));
                imageTable.AddCell(new Paragraph("CONDITIONS", bondFt));
                foreach (var item in list)
                {
                    imageTable.AddCell(new PdfPCell(new Phrase(item["NAME"].ToString(), contentFt))); //ATTACHMENTNAME
                    imageTable.AddCell(new PdfPCell(new Phrase(item["FILENAME"].ToString(), contentFt))); //FILENAME
                    imageTable.AddCell(new PdfPCell(new Phrase(item["ARCHIVEFILENAME"].ToString(), contentFt))); //ARCHIVEFILENAME
                    imageTable.AddCell(new PdfPCell(new Phrase(item["CONDITIONS"] != null 
                        ? item["CONDITIONS"].ToString() : string.Empty, contentFt))); //CONDITIONS
                }
                this.document.Add(new Paragraph("\n"));
                this.document.Add(imageTable);
                this.document.Add(new Paragraph("\n"));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void SetTestHeaderParameters()
        {
            Font bondFt = new Font(this.bfTimes, 6, 1);
            Font contentFt = new Font(this.bfTimes, 6);
            try
            {
                var paraList = this.GetTestMesurements();
                if (paraList.Count() > 0)
                {
                    var opStepList = paraList.Select(r => r.OPERATIONSTEPNAME).Distinct().ToList();
                    //PdfPTable measurementsTable = new PdfPTable(paraList.FirstOrDefault().GetType().GetProperties().Count() + 1);

                    foreach (var opStep in opStepList)
                    {
                        PdfPTable measurementsTable = new PdfPTable(paraList.FirstOrDefault().GetType().GetProperties().Count() - 1);
                        measurementsTable.WidthPercentage = 100f;
                        //Add OperationStepName Header into each MeasurementTable
                        PdfPCell opStepCell = new PdfPCell(new Phrase(opStep, bondFt));
                        opStepCell.Colspan = paraList.FirstOrDefault().GetType().GetProperties().Count();
                        measurementsTable.AddCell(opStepCell);

                        var opStepMeasurements = paraList.Where(r => r.OPERATIONSTEPNAME == opStep).ToList();
                        //Add Table Header
                        foreach (var pi in opStepMeasurements.FirstOrDefault().GetType().GetProperties())
                        {
                            if (pi.Name == "OPERATIONSTEPNAME") continue;
                            measurementsTable.AddCell(new PdfPCell(new Phrase(pi.Name, contentFt)));
                        }
                        //Add Table Content
                        int i = 1;
                        foreach (var osMeasurement in opStepMeasurements)
                        {
                            foreach (var pi in osMeasurement.GetType().GetProperties())
                            {
                                if (pi.Name == "OPERATIONSTEPNAME") continue;
                                if (pi.Name == "No")
                                {
                                    measurementsTable.AddCell(new PdfPCell(new Phrase(i.ToString(), contentFt)));
                                    i = i + 1;
                                }
                                else
                                {
                                    var value = pi.GetValue(osMeasurement);
                                    measurementsTable.AddCell(new PdfPCell(new Phrase(value == null ? "" : value.ToString(), contentFt)));
                                }
                            }
                        }

                        this.document.Add(measurementsTable);
                        this.document.Add(new Paragraph("\n"));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region GetTestReportRawData
        private dynamic GetTestHeader()
        {
            string sql = string.Format(@"SELECT * FROM TESTHEADER_V WHERE TESTHEADERID = {0}", this._testHeaderId);
            List<dynamic> list = new List<dynamic>();
            try
            {
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                {
                    list = sqlConn.Query(sql).ToList();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            if (list.Any())
                this._TestHeader = list.FirstOrDefault();           
            return this._TestHeader;
        }
        private List<dynamic> GetDeviceReleations()
        {
            string sql = string.Format(@"SELECT DEVICEID,DEVICERELATIONID,SERIALNUMBER,PARTNUMBER,PRODUCTID,RELATETOSERIALNUMBER,RELATETOPARTNUMBER,RELATETOPARTTYPE,RELATETOREV,ACTIVE	 FROM DEVICERELATION WHERE DEVICEID = {0}",
                this._deviceId);
            List<dynamic> list = new List<dynamic>();
            try
            {
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                {
                    list = sqlConn.Query(sql).ToList();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return list;
        }
        private List<dynamic> GetTestHeaderMisc()
        {
            string sql = string.Format(@"SELECT miscdesc, miscvalue FROM testheadermisc WHERE testheaderid = {0}", this._testHeaderId);
            List<dynamic> list = new List<dynamic>();
            try
            {
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                {
                    list = sqlConn.Query(sql).ToList();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return list;
        }
        private List<dynamic> GetTestHeaderSteps()
        {
            string sql = string.Format(@"SELECT distinct TESTHEADERSTEPID, OPERATIONSTEPNAME, CALLERNAME, TESTTYPE,
                                TEMPERATURE, SAMPLERATE, MISCINFO, RESULT
                                FROM testheaderstep_v WHERE testheaderid = {0} order by testheaderstepid", this._testHeaderId);
            List<dynamic> list = new List<dynamic>();
            try
            {
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                {
                    list = sqlConn.Query(sql).ToList();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return list;
        }
        private List<dynamic> GetMesurements()
        {
            string sql = string.Format(@"SELECT TESTHEADERSTEPID, PARAMETERNAME, POSITION, UNITS, VALUE, SPECMIN, SPECMAX,
                                        COMPOPERATOR, STATUS, COMMENTS
                                        FROM MEASUREMENT_V WHERE TESTHEADERID = {0} order by PARAMETERNAME,POSITION", this._testHeaderId);
            var list = new List<dynamic>();
            try
            {
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                {
                    list = sqlConn.Query(sql).ToList();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return list;
        }
        private List<dynamic> GetStringMesurements()
        {
            string sql = string.Format(@"SELECT TESTHEADERSTEPID, PARAMETERNAME, UNITS, VALUESTRING ""VALUE"", SPECSTRING,
                                        COMPOPERATOR, STATUS
                                        FROM STRINGMEASUREMENT_V WHERE TESTHEADERID = {0}", this._testHeaderId);
            var list = new List<dynamic>();
            try
            {
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                {
                    list = sqlConn.Query(sql).ToList();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return list;
        }
        private List<dynamic> GetAllTestFiles()
        {
            var list = new List<dynamic>();
            try
            {
                Dictionary<string, string> s3List = new Dictionary<string, string>();
                string sql = string.Format("Select V.* From TESTHEADERATTACHMENT_V V " +
                    "where V.TestHeaderId = {0} ", this._testHeaderId);
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                {
                    list = sqlConn.Query(sql).ToList();
                }

                if (list != null && list.Count() > 0)
                {
                    string archiveFolder = string.Empty;
                    var testAttachItem = (list.FirstOrDefault() as IDictionary<string, object>);
                    if (testAttachItem != null && testAttachItem.ContainsKey("ARCHIVEFOLDER"))
                        archiveFolder = testAttachItem["ARCHIVEFOLDER"].ToString();
                        
                    if (string.IsNullOrEmpty(archiveFolder) == false)
                    {
                        AWSS3Helper awsHelper = new AWSS3Helper();
                        var bucketName = LookupHelper.GetConfigValueByName("Archive_BucketName"); //lum-tds
                        var awsFolderName = LookupHelper.GetConfigValueByName("AWSFolderPath"); //Dev

                        List<string> archiveChars = archiveFolder.Split('\\').ToList();
                        int startIdx = archiveChars.IndexOf("Archive");
                        string[] s3Chars = archiveChars.Where(x => archiveChars.IndexOf(x) > startIdx).ToArray();
                        var awsFilePath = awsFolderName + "/" + string.Join("/", s3Chars);

                        s3List = awsHelper.GenerateAllURL_from_s3(bucketName, awsFilePath);
                    }
                }

                Utils.FileHelper.FileExistChecker(list, s3List, false);
            }
            catch (Exception)
            {
                throw;
            }
            return list;
        }
        private List<dynamic> GetArrayDatas()
        {
            string sql = string.Format(@"SELECT TESTHEADERSTEPID, PARAMETERNAME, ARRAYVALUE FROM ARRAYDATA_V 
                                        WHERE TESTHEADERID = {0}", this._testHeaderId);
            List<dynamic> list = new List<dynamic>();
            try
            {
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                {
                    list = sqlConn.Query(sql).ToList();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return list;
        }
        private List<TestHeaderParameter> GetTestMesurements()
        {
            string sql = string.Format(@"SELECT OPERATIONSTEPNAME, PARAMETERNAME, UNITS, VALUE, SPECMIN, SPECMAX, STATUS  
                                        FROM MEASUREMENT_V WHERE TESTHEADERID = {0} 
                                        ORDER BY TESTHEADERSTEPID ASC, PARAMETERID ASC", this._testHeaderId);
            List<TestHeaderParameter> list = new List<TestHeaderParameter>();
            try
            {
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                {
                    list = sqlConn.Query<TestHeaderParameter>(sql).ToList();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return list;
        }
        #endregion
    }
}
