using ClosedXML.Excel;
using KaizenTDSMvcAPI.Models.PSMReport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Cors;

namespace KaizenTDSMvcAPI.Controllers
{
    /// <summary>
    /// PSMReport
    /// </summary>
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("api/PSMReport")]
    public class PSMReportController : ApiController
    {
        /// <summary>
        /// PSMExcelReport
        /// </summary>
        /// <param name="inputList">PSMReport[] inputList</param>
        /// <returns></returns>
        [HttpPost]
        [Route("PSMExcelReport")]
        public HttpResponseMessage PSMExcelReport([FromBody] PSMReport[] inputList)
        {
            using (var workbook = new XLWorkbook())
            {
                var uniqueMast_Name = inputList.Select(p => p.MAST_NAME).Distinct().ToList();
                var worksheet = workbook.Worksheets.Add("PSMReport");
                worksheet.Style.Font.FontSize = 6;

                var currentRow = 1;
                worksheet.Cell(currentRow, 1).Value = "";
                // worksheet.Cell(currentRow, i + 2).Style.Alignment.TextRotation = 90;
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 1).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(currentRow, 1).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(currentRow, 1).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(currentRow, 1).Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                for (int i = 0; i < uniqueMast_Name.Count; i++)
                {
                    worksheet.Cell(currentRow, i + 2).Value = uniqueMast_Name[i];
                    worksheet.Cell(currentRow, i + 2).Style.Alignment.TextRotation = 90;
                    worksheet.Cell(currentRow, i + 2).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, i + 2).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(currentRow, i + 2).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(currentRow, i + 2).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(currentRow, i + 2).Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                    worksheet.Cell(currentRow, i + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    if (i == uniqueMast_Name.Count - 1)
                    {
                        worksheet.Cell(currentRow, i + 2).Style.Border.RightBorder = XLBorderStyleValues.Thick;
                    }
                }
                var uniqueJDE_Product = inputList.Select(p => p.JDE_PRODUCT_ID).Distinct().ToList();
                var currentCol = 1;
                for (int j = 0; j < uniqueJDE_Product.Count; j++)
                {
                    currentRow++;
                    worksheet.Row(currentRow).Height = 16;
                    var jde_Name = uniqueJDE_Product[j].ToString();
                    worksheet.Cell(currentRow, currentCol).Value = jde_Name;
                    worksheet.Cell(currentRow, currentCol).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(currentRow, currentCol).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(currentRow, currentCol).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(currentRow, currentCol).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    for (int k = 0; k < uniqueMast_Name.Count; k++)
                    {
                        currentCol++;
                        var mast_Name = uniqueMast_Name[k];
                        var spec_Value = inputList.ToList().Where(w => w.JDE_PRODUCT_ID == jde_Name).Where(w => w.MAST_NAME == mast_Name).Select(y => y.SPEC_VALUE).FirstOrDefault();
                        worksheet.Cell(currentRow, currentCol).Value = spec_Value;
                        worksheet.Cell(currentRow, currentCol).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                        worksheet.Cell(currentRow, currentCol).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                        worksheet.Cell(currentRow, currentCol).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                        worksheet.Cell(currentRow, currentCol).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                        worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        if (k == uniqueMast_Name.Count - 1)
                        {
                            worksheet.Cell(currentRow, currentCol).Style.Border.RightBorder = XLBorderStyleValues.Thick;
                        }
                        if (j == uniqueJDE_Product.Count - 1)
                        {
                            worksheet.Cell(currentRow, currentCol).Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                        }
                    }
                    if (j == uniqueJDE_Product.Count - 1)
                    {
                        worksheet.Cell(currentRow, 1).Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                    }
                    currentCol = 1;
                }
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var p = stream.ToArray();

                    HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
                    httpResponseMessage = new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new ByteArrayContent(p)
                    };
                    httpResponseMessage.Content.Headers.Add("x-filename", "name.xlsx");
                    //.XLSX
                    httpResponseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                    httpResponseMessage.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                    httpResponseMessage.Content.Headers.ContentDisposition.FileName = "PSM.xlsx";
                    httpResponseMessage.Content.Headers.ContentLength = p.Length;
                    httpResponseMessage.StatusCode = HttpStatusCode.OK;
                    return httpResponseMessage;
                }
            }
        }
    }
}
