using Dapper;
using iTextSharp.text;
using iTextSharp.text.pdf;
using KaizenTDSMvcAPI.Models.TestReportClasses;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.XPath;

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
        public TestHeader _TestHeader;
        private string testHeaderId;
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

        public MemoryStream CreateTestReport(string testHeaderId)
        {
            this.testHeaderId = testHeaderId;
            _TestHeader = new TestHeader();
            var stream = new MemoryStream();
            try
            {
                using (this.document = new Document())
                {
                    using (var writer = PdfWriter.GetInstance(this.document, stream))
                    {
                        this.document.Open();
                        this.SetPDFTitle();
                        this.document.Add(new Paragraph("\n"));
                        this.document.Add(new Paragraph("\n"));
                        this.SetTestHeader();
                        this.document.Add(new Paragraph("\n"));
                        this.SetTestHeaderParameters();
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

        private void SetTestHeader()
        {
            Font bondFt = new Font(this.bfTimes, 6, 1);
            Font contentFt = new Font(this.bfTimes, 6);
            try
            {
                var testHeader = this.GetTestHeader();

                PdfPTable headerTable = new PdfPTable(testHeader.GetType().GetProperties().Count());
                headerTable.WidthPercentage = 80f;
                headerTable.HorizontalAlignment = Element.ALIGN_LEFT;
                //Header
                foreach (var pi in testHeader.GetType().GetProperties())
                {
                    headerTable.AddCell(new PdfPCell(new Phrase(pi.Name, bondFt)));
                }
                //Content
                foreach (var pi in testHeader.GetType().GetProperties())
                {
                    headerTable.AddCell(new PdfPCell(new Phrase(pi.GetValue(testHeader, null).ToString(), contentFt)));
                }
                this.document.Add(headerTable);
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
        private TestHeader GetTestHeader()
        {
            string sql = string.Format(@"SELECT SERIALNUMBER, PRODUCTFAMILYNAME, PARTNUMBER,
                                        OPERATIONNAME, ENDTIME, RESULT   
                                        FROM TESTHEADER_V WHERE TESTHEADERID = {0}", this.testHeaderId);
            List<TestHeader> list = new List<TestHeader>();
            try
            {
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DATABASECONNECTIONSTRING))
                {
                    list = sqlConn.Query<TestHeader>(sql).ToList();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            this._TestHeader = list.FirstOrDefault();
            return this._TestHeader;
        }

        private List<TestHeaderParameter> GetTestMesurements()
        {
            string sql = string.Format(@"SELECT OPERATIONSTEPNAME, PARAMETERNAME, UNITS, VALUE, SPECMIN, SPECMAX, STATUS  
                                        FROM MEASUREMENT_V WHERE TESTHEADERID = {0} 
                                        ORDER BY TESTHEADERSTEPID ASC, PARAMETERID ASC", this.testHeaderId);
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