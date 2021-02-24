using KaizenTDSMvcAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Net.Http.Headers;
using System.Net.Http.Formatting;
using KaizenTDSMvcAPI.Models.Classes;
using System.Data;
using System.Configuration;
using Oracle.ManagedDataAccess.Client;
using System.Text;
using System.Web.Http.Cors;
using System.IO;




namespace KaizenTDSMvcAPI.Controllers
{

    // [Authorize]

    // [EnableCores()]
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("API/ISN")]

   
    public class ISNController : ApiController
    {
        //O_Details[] po = new PO_Details[];
        //public IEnumerable< PO_Details> GetAllPO()
        //  {
        //      return po;
        //  }
        //  [Route 'GetPO']
         
        public string Error_Message;
        public Cls_SNSpec ObjSNSpec;
        public Cls_Issued_SN obj_issuedSN;

        /// <summary>
        /// Check Login
        /// </summary>
        /// <param name="userinfo">input user and password</param>
        /// <returns></returns>
        [Route("CheckLogin")]
       // [HttpPost]
        public HttpResponseMessage CheckLogin(Cls_User userinfo)
        {

            string conn = System.Configuration.ConfigurationManager.AppSettings["PATDBTDSTST01_WEBSTART"];
            Ora_DB ora = new Ora_DB(conn);
            DataSet DBSet = new DataSet();
            string sp_name = "ISN.ISN_CHECK_PASSWORD_new";
            //  OracleParameter[] Para_List = new OracleParameter;
            List<OracleParameter> Para_List = new List<OracleParameter>();


            OracleParameter Param1 = new OracleParameter("pi_who", OracleDbType.Varchar2, 10);
            Param1.Value = "Vendor";
            Param1.Direction = ParameterDirection.Input;
            Para_List.Add(Param1);

            OracleParameter Param2 = new OracleParameter("pi_user_id", OracleDbType.Varchar2, 100);
            Param2.Value = userinfo.Username;
            Param2.Direction = ParameterDirection.Input;
            Para_List.Add(Param2);

            OracleParameter Param3 = new OracleParameter("pi_password", OracleDbType.Varchar2, 100);
            Param3.Value = userinfo.Password;
            Param3.Direction = ParameterDirection.Input;
            Para_List.Add(Param3);

            OracleParameter Param4 = new OracleParameter("po_cur", OracleDbType.RefCursor);
            //Param1.Value = "Vendor";
            Param4.Direction = ParameterDirection.Output;
            Para_List.Add(Param4);
            Cls_Return Obj_Return = new Cls_Return();
            if (ora.SP_2_DBSet(sp_name, Para_List, ref DBSet,ref Error_Message))
            {

                if (DBSet.Tables[0].Rows[0]["strPassword"].ToString() =="1")
                {
                    Obj_Return.Success = true;
                    Obj_Return.Data = DBSet.Tables[0].Rows[0]["INTERNALID"].ToString();
                    Obj_Return.Error_Message = "";

                }
                else
                {
                    Obj_Return.Success = false;
                    Obj_Return.Data ="";
                    Obj_Return.Error_Message = "Incorrect userid and password";
                }


                HttpResponseMessage HRM = Request.CreateResponse(HttpStatusCode.OK, Obj_Return);

                return HRM;
            }
            else

            {

                Obj_Return.Success = false;
                Obj_Return.Data = "";
                Obj_Return.Error_Message = "Error_Message";

                HttpResponseMessage HRM = Request.CreateResponse(HttpStatusCode.OK, Obj_Return);
                return HRM;
            }

        }

        [Route("ListAllPO")]
        public HttpResponseMessage ListAllPO(Cls_Get_PO PO)
        {
            string conn = System.Configuration.ConfigurationManager.AppSettings["PATDBTDSTST01_WEBSTART"];
            

            List<OracleParameter> Para_List = new List<OracleParameter>();            

            OracleParameter Param1 = new OracleParameter("pi_vendor_id", OracleDbType.Varchar2, 100);
            Param1.Value = PO.Vendor_id;
            Param1.Direction = ParameterDirection.Input;
            Para_List.Add(Param1);

            //OracleParameter Param2 = new OracleParameter("pi_page_index", OracleDbType.Varchar2, 100);
            //Param2.Value = PO.Page_idx;
            //Param2.Direction = ParameterDirection.Input;
            //Para_List.Add(Param2);

            //OracleParameter Param3 = new OracleParameter("pi_page_size", OracleDbType.Varchar2, 100);
            //Param3.Value = PO.Page_size;
            //Param3.Direction = ParameterDirection.Input;
            //Para_List.Add(Param3);

            OracleParameter Param4 = new OracleParameter("cur1", OracleDbType.RefCursor);
            Param4.Direction = ParameterDirection.Output;
            Para_List.Add(Param4);
            Ora_DB ora = new Ora_DB(conn);

            // string sqlstr = "select * from isn.isn_po_details where PD_VENDOR_ID='" + InternalID + "' and insert_date>sysdate -10 order by pd_purchase_order ";

            string sp_name = "ISN.isn_get_po";
            DataSet DBSet = new DataSet();
            Boolean B_Status = ora.SP_2_DBSet(sp_name, Para_List, ref DBSet, ref Error_Message);

            Cls_Return Obj_Return = new Cls_Return();
            if (B_Status)
            {
                if (DBSet.Tables[0].Rows.Count > 0)
                {

                    Obj_Return.Success = true;
                    Obj_Return.Data = DBSet.Tables[0];
                    Obj_Return.Error_Message = "";
                }
                else
                {
                    Obj_Return.Success = false;
                    Obj_Return.Data = "";
                    Obj_Return.Error_Message = "No data";
                }

            }
            else
            {
                Obj_Return.Success = false;
                Obj_Return.Data = "";
                Obj_Return.Error_Message = Error_Message;
            }
            HttpResponseMessage HRM = Request.CreateResponse(HttpStatusCode.OK, Obj_Return);
            return HRM;


        }

        [Route("CheckPO")]        
        public HttpResponseMessage CheckPO(Cls_CheckPO PO)
        {

            string conn = System.Configuration.ConfigurationManager.AppSettings["PATDBTDSTST01_WEBSTART"];
            Ora_DB ora = new Ora_DB(conn);
            DataSet DBSet = new DataSet();
            string sp_name = "ISN.ISN_CHECKPO_NEW";
            //  OracleParameter[] Para_List = new OracleParameter;
            List<OracleParameter> Para_List = new List<OracleParameter>();

            OracleParameter Param1 = new OracleParameter("pi_ponum", OracleDbType.Varchar2, 100);
            Param1.Value = PO.Po_Number;
            Param1.Direction = ParameterDirection.Input;
            Para_List.Add(Param1);

            OracleParameter Param2 = new OracleParameter("pi_vendor_id", OracleDbType.Varchar2, 100);
            Param2.Value = PO.Vendor_id;
            Param2.Direction = ParameterDirection.Input;
            Para_List.Add(Param2);


            OracleParameter Param3 = new OracleParameter("pi_start_row", OracleDbType.Int32);
            Param3.Value = PO.start;
            Param3.Direction = ParameterDirection.Input;
            Para_List.Add(Param3);

            OracleParameter Param4 = new OracleParameter("pi_end_row", OracleDbType.Int32);
            Param4.Value = PO.end;
            Param4.Direction = ParameterDirection.Input;
            Para_List.Add(Param4);

            OracleParameter Param5 = new OracleParameter("po_count", OracleDbType.Int32);             
            Param5.Direction = ParameterDirection.Output;
            Para_List.Add(Param5);

            OracleParameter Param6 = new OracleParameter("cur1", OracleDbType.RefCursor);
            Param6.Direction = ParameterDirection.Output;
            Para_List.Add(Param6);

            

            Boolean B_Status = ora.SP_2_DBSet(sp_name, Para_List, ref DBSet, ref Error_Message);

           

            Cls_Return Obj_Return = new Cls_Return();
            if (B_Status)
            {
                if (DBSet.Tables[0].Rows.Count > 0 || PO.Po_Number == String.Empty)
                {
                    Obj_Return.datacount = Convert.ToInt16(Param5.Value.ToString());
                    Obj_Return.Success = true;
                    Obj_Return.Data = DBSet.Tables[0];
                    Obj_Return.Error_Message = "";
                    

                }
                else
                {
                    
                    {
                        Obj_Return.Success = false;
                        Obj_Return.Data = "";
                        Obj_Return.Error_Message = "Incorrect PO";
                    }
                }
            }
            else
            {
                Obj_Return.Success = false;
                Obj_Return.Data = "";
                Obj_Return.Error_Message = Error_Message;
            }

            HttpResponseMessage HRM = Request.CreateResponse(HttpStatusCode.OK, Obj_Return);
            return HRM;
        }

        [Route("IssuedQty")]
        public HttpResponseMessage GetIssuedQty(string po_num)
        {
            string sqlstr = "select nvl(sum(ID_Batch_Quantity), 0) IssuedQuantity from ISN_Issue_Details and  where ID_Purchase_Order = '" + po_num + "'";
            string conn = System.Configuration.ConfigurationManager.AppSettings["PATDBTDSTST01_WEBSTART"];
            Ora_DB ora = new Ora_DB(conn);
            DataSet DBSet = new DataSet();
            // DBSet = ora.GetDBSet(sqlstr);
            Boolean B_Status = ora.GetDBSet(sqlstr, ref DBSet, ref Error_Message);
            
            if (DBSet.Tables[0].Rows.Count > 0)
            {

                HttpResponseMessage HRM = Request.CreateResponse(HttpStatusCode.OK, DBSet.Tables[0]);

                return HRM;
            }
            else
            {
                HttpResponseMessage HRM = Request.CreateResponse(HttpStatusCode.OK, "NO data");
                return HRM;
            }

        }

        [Route("IssueHistory_PO")]
        public HttpResponseMessage GetissuedHistory_BYPO(string po_num)
        {
            string sqlstr = "select nvl(sum(ID_Batch_Quantity), 0) IssuedQuantity from ISN_Issue_Details and  where ID_Purchase_Order = '" + po_num + "'";
            string conn = System.Configuration.ConfigurationManager.AppSettings["PATDBTDSTST01_WEBSTART"];
            Ora_DB ora = new Ora_DB(conn);
            DataSet DBSet = new DataSet();
            //DBSet = ora.GetDBSet(sqlstr);
            Boolean B_Status = ora.GetDBSet(sqlstr, ref DBSet, ref Error_Message);
            if (DBSet.Tables[0].Rows.Count > 0)
            {

                HttpResponseMessage HRM = Request.CreateResponse(HttpStatusCode.OK, DBSet.Tables[0]);

                return HRM;
            }
            else
            {
                HttpResponseMessage HRM = Request.CreateResponse(HttpStatusCode.OK, "NO data");
                return HRM;
            }


        }


        [Route("IssueSN")]
        public HttpResponseMessage IssueSN(Cls_IssueSN IssueSN)
        {

          Boolean IssueResult=  SubIssueSN(IssueSN);
            //DBSet = ora.GetDBSet(sqlstr);
            Cls_Return Obj_Return = new Cls_Return();
            if (IssueResult)
            {
                Obj_Return.Success = true;
                Obj_Return.Error_Message ="";
                Obj_Return.Data = obj_issuedSN ;
                HttpResponseMessage HRM = Request.CreateResponse(HttpStatusCode.OK, Obj_Return);

               return HRM;
            }
            else
            {

                // Error_Message = "Can not get product family";
                Obj_Return.Success = false;
                Obj_Return.Error_Message = Error_Message;
                Obj_Return.Data = "";
                  HttpResponseMessage HRM = Request.CreateResponse(HttpStatusCode.OK, Obj_Return);
                return HRM;
            }


        }



        private Boolean  SubIssueSN(Cls_IssueSN obj_newissueSN)
        {
            //string sqlstr="select "

            string strSQL = "select distinct FM_Family_Type from isn.ISN_SN_Format where FM_Part_Number = '" + obj_newissueSN.part_number + "' ";
            string conn = System.Configuration.ConfigurationManager.AppSettings["PATDBTDSTST01_WEBSTART"];
             string strMfgIdReq="",StrAppendChar,StrSNFormat=""; // It may not using
            string strprdfamily = "";
            Ora_DB ora = new Ora_DB(conn);
            DataSet DBSet = new DataSet();
           // DBSet = ora.GetDBSet(strSQL);
            Boolean B_Status = ora.GetDBSet(strSQL, ref DBSet, ref Error_Message);
            
            if (DBSet.Tables[0].Rows.Count == 0)
            {

                Error_Message = " Cannot get the part number inforamtion, Please report the problem to Purchasing Department.";
                return false;
            }
            strprdfamily = DBSet.Tables[0].Rows[0]["FM_Family_Type"].ToString();

            if ((DBSet.Tables[0].Rows[0]["FM_Family_Type"] == DBNull.Value) || (DBSet.Tables[0].Rows[0]["FM_Family_Type"] == ""))
            {
                strSQL = "select max(ID_SerialNo_To) maxSerNo ";

                strSQL = strSQL + " from  ISN.ISN_Issue_Details where  ";

                strSQL = strSQL + " ID_Product_Family Is Null OR ID_Product_Family = ''";

            }
            else
            {
                if (DBSet.Tables[0].Rows[0]["FM_Family_Type"].ToString().Equals(10))
                {
                    strSQL = "select max(ID_SerialNo_To) maxSerNo ";

                    strSQL = strSQL + " from  ISN.ISN_Issue_Details where  ";

                    strSQL = strSQL + " ID_Product_Family = ( select distinct FM_Family_Type from ISN.ISN_SN_Format where ";

                    strSQL = strSQL + " FM_Part_Number = '" + obj_newissueSN.part_number + "') AND (ID_SerialNo_To NOT LIKE 'O%') ";
                }
                else
                {
                    strSQL = "select max(ID_SerialNo_To) maxSerNo ";

                    strSQL = strSQL + " from ISN.ISN_Issue_Details where  ";

                    strSQL = strSQL + " ID_Product_Family = ( select distinct FM_Family_Type from ISN.ISN_SN_Format where ";

                    strSQL = strSQL + " FM_Part_Number = '" + obj_newissueSN.part_number + "') ";
                }

            }
            DBSet.Clear();

               B_Status = ora.GetDBSet(strSQL, ref DBSet, ref Error_Message);
            if (B_Status)
            {
                if (DBSet == null || DBSet.Tables[0].Rows.Count == 0)
                {
                    Error_Message = "Cannot get the issue hisotry data";
                    return false;
                }
            }
            else
            {
                return false;
            }

            string Str_MaxSerialNo = DBSet.Tables[0].Rows[0]["MAXSERNO"].ToString();

            int intActReqQty = obj_newissueSN.Bth_Qty;
            ObjSNSpec = new Cls_SNSpec();
            obj_issuedSN = new Cls_Issued_SN();
            obj_issuedSN.po_number = obj_newissueSN.po_number;
            obj_issuedSN.ordertype = obj_newissueSN.order_type;
            obj_issuedSN.po_line = obj_newissueSN.po_line_id;
            obj_issuedSN.part_number = obj_newissueSN.part_number;
            obj_issuedSN.prdfamily = strprdfamily;
            obj_issuedSN.ActQty = obj_newissueSN.ACT_QTY;
            obj_issuedSN.BchQty = obj_newissueSN.Bth_Qty;
            obj_issuedSN.Venuid = obj_newissueSN.vendor_id;
            obj_issuedSN.Ventransferdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (Str_MaxSerialNo.Trim() != string.Empty)
            {

                //return;
               
                if (GetSNSpec(obj_newissueSN.po_number, obj_newissueSN.order_type, obj_newissueSN.po_line_id, obj_newissueSN.part_number, obj_newissueSN.vendor_id))
                     {

                    if (ObjSNSpec.StrMFGID_Req.ToUpper() == "Y")
                    {
                        ObjSNSpec.StrMFGID_Req = Str_MaxSerialNo.Substring(ObjSNSpec.InsSns , 1);

                    }

                    Str_MaxSerialNo = Str_MaxSerialNo.Substring(0, ObjSNSpec.InsSns);

                }

            }
            else

            {

                if (GetSNSpec(obj_newissueSN.po_number, obj_newissueSN.order_type, obj_newissueSN.po_line_id, obj_newissueSN.part_number, obj_newissueSN.vendor_id))
                {
                    if (ObjSNSpec.StrSNOri != "")
                    {
                        int i=0;
                        string strAlphaNumeric,StrSize;
                        int intsize;
                        while (i < ObjSNSpec.InsSns)
                        {
                            i = i + 3;
                            strAlphaNumeric = ObjSNSpec.StrSNOri.Substring(i - 1, 1);
                            StrSize = ( ObjSNSpec.StrSNOri.Substring(i - 2, 1));

                            if (StrSize == "")
                            {
                                ObjSNSpec.InsSns = 0;
                                intsize = 0;
                            }
                            else
                            {
                                intsize = Convert.ToInt16(StrSize);
                            }

                            if (strAlphaNumeric.ToUpper() == "A")
                            {
                                StrAppendChar = "A";
                            }
                            else
                            {
                                StrAppendChar = "0";
                            }
                            for (int j = 0; j <= ObjSNSpec.InsSns; j++)
                            {
                                StrSNFormat = StrSNFormat + StrAppendChar;

                            }
                            Str_MaxSerialNo = StrSNFormat;
                        }

                    }
 
                }


            }

            if (Str_MaxSerialNo != "")
            {
                if (getFromToSerialNos(obj_newissueSN.part_number, Str_MaxSerialNo, obj_newissueSN.po_number, Str_MaxSerialNo, obj_newissueSN.Bth_Qty, ObjSNSpec.StrNotRelAlpht, 0, ""))
                {
                    if (obj_issuedSN.strCalcFromSN == "" || obj_issuedSN.strCalcTOSN == "")
                    {
                        Error_Message = "There was a problem with Serial Number Range. Please report the problem to Purchasing Department.";
                        return false;

                    }

                }
                else
                {
                    return false;
                }
            }

            if (obj_issuedSN.strMFGId is null)
            {
                obj_issuedSN.strMFGId = "";
            }

            if (InsertSN())
            {
                if (GenerateSNFile())
                {

                    return true;
                }
                else
                {
                    Error_Message = "Save SN file fail";
                    return false;
                }
            }
            else
            {

                return false;
            }





           


        }

        private bool GetSNSpec( string po_number, string order_type, string po_line_id,string part_number, string vendor_id)
            {


            try
            {

                List<OracleParameter> Para_List = new List<OracleParameter>();

                OracleParameter Param1 = new OracleParameter("pi_ponum", OracleDbType.Varchar2, 50);
                Param1.Value = po_number;
                Param1.Direction = ParameterDirection.Input;
                Para_List.Add(Param1);

                OracleParameter Param2 = new OracleParameter("pi_ordtyp", OracleDbType.Varchar2, 50);
                Param2.Value = order_type;
                Param2.Direction = ParameterDirection.Input;
                Para_List.Add(Param2);

                OracleParameter Param3 = new OracleParameter("pi_lid", OracleDbType.Varchar2, 50);
                Param3.Value = po_line_id;
                Param3.Direction = ParameterDirection.Input;
                Para_List.Add(Param3);

                OracleParameter Param4 = new OracleParameter("pi_partnum", OracleDbType.Varchar2, 50);
                Param4.Value = part_number;
                Param4.Direction = ParameterDirection.Input;
                Para_List.Add(Param4);

                OracleParameter Param5 = new OracleParameter("pi_vendor_id", OracleDbType.Varchar2, 50);
                Param5.Value = vendor_id;
                Param5.Direction = ParameterDirection.Input;
                Para_List.Add(Param5);

                OracleParameter Param6 = new OracleParameter("cur1", OracleDbType.RefCursor);
                Param6.Direction = ParameterDirection.Output;
                Para_List.Add(Param6);
                string conn = System.Configuration.ConfigurationManager.AppSettings["PATDBTDSTST01_WEBSTART"];
                string error_message;
                Ora_DB ora = new Ora_DB(conn);
                DataSet DBSet = new DataSet();
                if (ora.SP_2_DBSet("ISN.isn_snspec", Para_List, ref DBSet,ref Error_Message))
                {
                    if (DBSet.Tables.Count > 0)
                    {
                        ObjSNSpec.InsSns = Convert.ToInt16(DBSet.Tables[0].Rows[0]["FM_SN_Size"].ToString());

                        ObjSNSpec.StrSNOri = DBSet.Tables[0].Rows[0]["FM_SN_Orientation"].ToString();

                        ObjSNSpec.StrNotRelAlpht = DBSet.Tables[0].Rows[0]["FM_Restrict_Chars"].ToString();

                        ObjSNSpec.StrProductFamily = DBSet.Tables[0].Rows[0]["FM_Product_Family"].ToString();

                        ObjSNSpec.StrFamilyType = DBSet.Tables[0].Rows[0]["FM_Family_Type"].ToString();

                        ObjSNSpec.StrMFGID_Req = DBSet.Tables[0].Rows[0]["FM_Mfg_Id_Reqd"].ToString();
                        return true;
                    }
                    else
                    {
                        Error_Message = "Request could not be completed as no Format Specs were found for the Part Number: '" + part_number + "'.Please report the problem to Purchasing Department";
                            return false;
                    }


                }
                else
                {
                    Error_Message = "Cannot Get SN Spec, part number" + part_number;
                    return false;

                }
            }
            catch (Exception ex)
            {
                Error_Message = System.Reflection.MethodBase.GetCurrentMethod().Name + ":" + ex.Message;
                return false;

            }
         

        }


        private bool getFromToSerialNos(string part_number,string strInputVal,string PO_NO, string pstrMaxSerNo, int pReqQuantity, string pNotReqdAlpha, int oSNCount, string pstrToSerialNo)
        {
            int intIncrementalVal = pReqQuantity;
            string InputVal = pstrMaxSerNo;
            string StrNotIn = pNotReqdAlpha;
            string[] StrNotInArray = StrNotIn.Split(',');
            string StrCalcFromSN, StrCalcTOSN;
            string strMaxAlpha = "Z";
            string FinalStr = "";
            Boolean BReachedMax;
            string StrCurrentVal="";


            if (StrNotIn.IndexOf(strMaxAlpha) >= 0)
            {
                strMaxAlpha = "Y";
            }

            try
            {

                for (int i = 0; i < StrNotInArray.Length; i++)
                {
                    if (InputVal.IndexOf(StrNotInArray[i]) >= 0)

                    {
                        Error_Message = "The Last Max SN "+ InputVal +" includes the restrict char "+ StrNotInArray[i];
                        return false;
                    }

                }
                
                //List<string> strval=new List();
                int IntLen = InputVal.Length ;
                string[] Strval=new string[IntLen];
                string[] strMaxAlphaNumericVal = new string[IntLen];
                //strMaxAlphaNumericVal
                string StrMaxVal = "";
                //object int_out;
                for (int i = IntLen-1; i >= 0; i--)
                {
                    Strval[i] = InputVal.Substring(i , 1);

                    short int_out;
                    // int.TryParse(Strval[i],  );
                    if (Int16.TryParse(Strval[i], out int_out))//adjust if it is number
                    {
                        StrMaxVal =  "9"+ StrMaxVal ;
                        strMaxAlphaNumericVal[i] = "9";
                    }
                    else
                    {
                        StrMaxVal = strMaxAlpha + StrMaxVal;

                        strMaxAlphaNumericVal[i] = strMaxAlpha;
                    }


                }


                if (pstrToSerialNo != "")
                {

                    StrMaxVal = pstrToSerialNo;
                }

                switch (part_number)
                {
                    case "21090261":
                        StrMaxVal = "YYY399";
                        break;

                    case "21094082":
                        StrMaxVal = "XYY999";
                        break;
                }

                int intCurrentDecrement = 1;
                if (strInputVal ==  StrMaxVal)                {

                    Error_Message = "Reached maximum Serial Number range for the selected PO# "+ PO_NO+ " Part# "+part_number+  "There were still " + (intIncrementalVal - Convert.ToInt16(oSNCount)).ToString() + " serial numbers remaining to be generated.<br>" + " serial numbers remaining to be generated ";
                    return false;
                }


                ///////////////Increment///////////////////////////
                Boolean bfirst = true;
                for (int I = 1; I <= intIncrementalVal; I++)
                {
                    if (IsNumber(Strval[IntLen-1]))
                    {
                        if (Convert.ToInt16(Strval[IntLen-1]) < 9)
                        {
                            Strval[IntLen-1] = (Convert.ToInt16(Strval[IntLen-1]) + 1).ToString();

                        }
                        else
                        {
                            Strval[IntLen-1] = "0";
                            for (int j = IntLen - 2; j > 0; j--)
                            {
                                if (Strval[j] == strMaxAlphaNumericVal[j])
                                {
                                    if (IsNumber(Strval[j]))
                                    {
                                        Strval[j] = "0";
                                    }
                                    else
                                    {
                                        Strval[j] = "A";
                                    }
                                }
                                else
                                {
                                    if (IsNumber(Strval[j]))
                                    {
                                        Strval[j] = (Convert.ToInt16(Strval[j]) + 1).ToString();
                                        
                                    }
                                    else
                                    {
                                        Strval[j] = ASCII_2_STR((String_2_ASCII(Strval[j])) + 1).ToString();
                                        if (StrNotIn.IndexOf(Strval[j]) >= 0)
                                        {
                                            Strval[j] = ASCII_2_STR(String_2_ASCII(Strval[j]) + 1).ToString();

                                        }

                                    }

                                    break;
                                }


                            }

                        }
                    }
                    else
                    {

                        if (String_2_ASCII(Strval[IntLen-1]) < String_2_ASCII(strMaxAlpha))
                        {

                            Strval[IntLen-1] = ASCII_2_STR(String_2_ASCII(Strval[IntLen-1]) + 1).ToString();
                            if (StrNotIn.IndexOf(Strval[IntLen-1]) >= 0)
                            {
                                Strval[IntLen - 1] = ASCII_2_STR(String_2_ASCII(Strval[IntLen - 1]) + 1).ToString();

                            }
                        }
                        else
                        {

                            Strval[IntLen-1] = "A";
                            for (int j = IntLen - 2; j > 0; j--)
                            {
                                if (Strval[j] == strMaxAlphaNumericVal[j])
                                {
                                    if (IsNumber(Strval[j]))
                                    {
                                        Strval[j] = "0";
                                    }
                                    else
                                    {
                                        Strval[j] = "A";
                                    }
                                }
                                else
                                {
                                    if (IsNumber(Strval[j]))
                                    {
                                        Strval[j] = (Convert.ToInt16(Strval[j]) + 1).ToString();
                                       // Strval[j] = ASCII_2_STR(String_2_ASCII(Strval[j]) + 1).ToString();
                                    }
                                    else
                                    {
                                        Strval[j] = ASCII_2_STR(String_2_ASCII(Strval[j]) + 1).ToString();
                                        if (StrNotIn.IndexOf(Strval[j]) >= 0)
                                        {
                                            Strval[j] = ASCII_2_STR(String_2_ASCII(Strval[j]) + 1).ToString();

                                        }

                                    }
                                    break;
                                }
                               

                            }

                        }

                    }


                    StrCurrentVal = String.Join("", Strval); //Strval.ToString();

                    if (StrCurrentVal.Substring(StrCurrentVal.Length- 3, 3) == "000")
                    {
                        I = I - 1;
                    }
                    else
                    {
                        if (bfirst)
                        {
                            obj_issuedSN.strCalcFromSN= StrCurrentVal;
                            bfirst = false;
                        }
                        obj_issuedSN.ListSN.Add(StrCurrentVal);
                        if (StrCurrentVal == StrMaxVal)
                        {
                            obj_issuedSN.strCalcTOSN = StrCurrentVal;
                            BReachedMax = true;
                            Error_Message = "Reached maximum Serial Number range for the selected PO #:" + obj_issuedSN.po_number + " Line Item: " + obj_issuedSN.po_line + " Part#: " + obj_issuedSN.part_number;
                            Error_Message = Error_Message+ "There were still " + (intIncrementalVal - oSNCount).ToString() + " serial numbers remaining to be generated.This request could not be completed";
                             return  false;
                        }

                    }
                    oSNCount = oSNCount + 1;
                }

                obj_issuedSN.strCalcTOSN = StrCurrentVal;

            }
            catch (Exception ex)
            {
                Error_Message = System.Reflection.MethodBase.GetCurrentMethod().Name + ":" + ex.Message;
                return false;
            }



            return true;
        }



        private bool InsertSN()
        {

            List<OracleParameter> Para_List = new List<OracleParameter>();

            OracleParameter Param1 = new OracleParameter("pi_ponum", OracleDbType.Varchar2, 50);
            Param1.Value = obj_issuedSN.po_number;
            Param1.Direction = ParameterDirection.Input;
            Para_List.Add(Param1);

            OracleParameter Param2 = new OracleParameter("pi_ordtyp", OracleDbType.Varchar2, 50);
            Param2.Value = obj_issuedSN.ordertype;
            Param2.Direction = ParameterDirection.Input;
            Para_List.Add(Param2);

            OracleParameter Param3 = new OracleParameter("pi_lid", OracleDbType.Varchar2, 50);
            Param3.Value = obj_issuedSN.po_line;
            Param3.Direction = ParameterDirection.Input;
            Para_List.Add(Param3);

            OracleParameter Param4 = new OracleParameter("pi_prdfamily", OracleDbType.Varchar2, 50);
            Param4.Value = obj_issuedSN.prdfamily;
            Param4.Direction = ParameterDirection.Input;
            Para_List.Add(Param4);

            OracleParameter Param5 = new OracleParameter("pi_partnum", OracleDbType.Varchar2, 50);
            Param5.Value = obj_issuedSN.part_number;
            Param5.Direction = ParameterDirection.Input;
            Para_List.Add(Param5);
            
            OracleParameter Param6 = new OracleParameter("pi_actqty", OracleDbType.Int16);
            Param6.Value = obj_issuedSN.ActQty;
            Param6.Direction = ParameterDirection.Input;
            Para_List.Add(Param6);

            OracleParameter Param7 = new OracleParameter("pi_bchqty", OracleDbType.Int16);
            Param7.Value = obj_issuedSN.BchQty;
            Param7.Direction = ParameterDirection.Input;
            Para_List.Add(Param7);

            OracleParameter Param8 = new OracleParameter("pi_fromsernum", OracleDbType.Varchar2, 50);
            Param8.Value = obj_issuedSN.strCalcFromSN;
            Param8.Direction = ParameterDirection.Input;
            Para_List.Add(Param8);
          
            OracleParameter Param9 = new OracleParameter("pi_tosernum", OracleDbType.Varchar2, 50);
            Param9.Value = obj_issuedSN.strCalcTOSN;
            Param9.Direction = ParameterDirection.Input;
            Para_List.Add(Param9);

            OracleParameter Param10 = new OracleParameter("pi_venuid", OracleDbType.Varchar2, 50);
            Param10.Value = obj_issuedSN.Venuid;
            Param10.Direction = ParameterDirection.Input;
            Para_List.Add(Param10);

            OracleParameter Param11 = new OracleParameter("pi_ventrandate", OracleDbType.Varchar2, 50);
            Param11.Value = obj_issuedSN.Ventransferdate;
            Param11.Direction = ParameterDirection.Input;
            Para_List.Add(Param11);

            OracleParameter Param12 = new OracleParameter("pi_snlist", OracleDbType.Clob);
            Param12.Value = string.Join(",", obj_issuedSN.ListSN); //obj_issuedSN.ListSN;
            Param12.Direction = ParameterDirection.Input;
            Para_List.Add(Param12);

            OracleParameter Param13 = new OracleParameter("cur1", OracleDbType.RefCursor);
            Param13.Direction = ParameterDirection.Output;
            Para_List.Add(Param13);
            string conn = System.Configuration.ConfigurationManager.AppSettings["PATDBTDSTST01_WEBSTART"];
            string error_message;
            Ora_DB ora = new Ora_DB(conn);
            DataSet DBSet = new DataSet();

            if (ora.SP_2_DBSet("ISN.ISN_INSERTSN", Para_List, ref DBSet, ref Error_Message))
            {

                if (DBSet.Tables[0].Rows[0][0].ToString().ToUpper() == "INSERTED")
                {

                    return true;
                }
                else
                {
                    Error_Message = DBSet.Tables[0].Rows[0][0].ToString();
                    return false;
                }
             }
            else
            {
                Error_Message = "Save issue SN error";
                return false;
            }

             

        }
        

        private bool IsNumber(string str_val)
        {

            int o_short;
            if (Int32.TryParse(str_val, out o_short))
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public HttpResponseMessage GetIsssedFile(string po_number, string start_sn)
        {
            return null; 
        }
        [Route("IssueHistory")]
        [HttpPost]
        public HttpResponseMessage GetIssueHistory(Cls_IssuedHistory issuedHistory)
        {
            string conn = System.Configuration.ConfigurationManager.AppSettings["PATDBTDSTST01_WEBSTART"];


            List<OracleParameter> Para_List = new List<OracleParameter>();

            OracleParameter Param1 = new OracleParameter("pi_vendor_name", OracleDbType.Varchar2, 100);
            Param1.Value = issuedHistory.Vendor_Name;
            Param1.Direction = ParameterDirection.Input;
            Para_List.Add(Param1);

            OracleParameter Param2 = new OracleParameter("pi_part_number", OracleDbType.Varchar2, 100);
            Param2.Value = issuedHistory.Part_Number;
            Param2.Direction = ParameterDirection.Input;
            Para_List.Add(Param2);

            OracleParameter Param3 = new OracleParameter("pi_po_number", OracleDbType.Varchar2, 100);
            Param3.Value = issuedHistory.Po_Number;
            Param3.Direction = ParameterDirection.Input;
            Para_List.Add(Param3);

            OracleParameter Param4 = new OracleParameter("Pi_SN", OracleDbType.Varchar2, 5000);

            // Param4.Value = issuedHistory.SerialNo;
            //Param4.Direction = ParameterDirection.Input;
            Param4.Direction = ParameterDirection.Output;
            Para_List.Add(Param4);           

            OracleParameter Param5 = new OracleParameter("pi_BYGroup", OracleDbType.Varchar2, 100);
            if (issuedHistory.Group)
            {
                Param5.Value =1  ;
            }
            else
            {
                Param5.Value = 0;
            }
            
            Param5.Direction = ParameterDirection.Input;
            Para_List.Add(Param5);
           

            OracleParameter Param6 = new OracleParameter("PI_START_ROW", OracleDbType.Int32);
            Param6.Value = issuedHistory.start;
            Param6.Direction = ParameterDirection.Input;
            Para_List.Add(Param6);

            OracleParameter Param7 = new OracleParameter("PI_END_ROW", OracleDbType.Int32);
            Param7.Value = issuedHistory.end;
            Param7.Direction = ParameterDirection.Input;
            Para_List.Add(Param7);

            OracleParameter Param8 = new OracleParameter("PO_COUNT", OracleDbType.Int32);
           // Param8.Value = issuedHistory.SerialNo;
            Param8.Direction = ParameterDirection.Output;
            Para_List.Add(Param8);

            OracleParameter Param9 = new OracleParameter("cur1", OracleDbType.RefCursor);
            Param9.Direction = ParameterDirection.Output;
            Para_List.Add(Param9);
            Ora_DB ora = new Ora_DB(conn);            

            string sp_name = "ISN.isn_get_issue_details";
            DataSet DBSet = new DataSet();
            Boolean B_Status = ora.SP_2_DBSet(sp_name, Para_List, ref DBSet, ref Error_Message);

            Cls_Return Obj_Return = new Cls_Return();
            if (B_Status)
            {
                if (DBSet.Tables[0].Rows.Count > 0)
                {
                    Obj_Return.datacount = Convert.ToInt32(Param8.Value.ToString());
                    Obj_Return.Success = true;
                    Obj_Return.Data = DBSet.Tables[0];
                    Obj_Return.Error_Message = "";
                }
                else
                {
                    Obj_Return.Success = false;
                    Obj_Return.Data = "";
                    Obj_Return.Error_Message = "No data";
                }

            }
            else
            {
                Obj_Return.Success = false;
                Obj_Return.Data = "";
                Obj_Return.Error_Message = Error_Message;
            }
            HttpResponseMessage HRM = Request.CreateResponse(HttpStatusCode.OK, Obj_Return);
            return HRM;
        }

        [Route("DownloadFile")]
        [HttpGet]
        public HttpResponseMessage DownloaFile(string file_name)
        {
            Cls_Return Obj_Return = new Cls_Return();
            string strPath = System.Configuration.ConfigurationManager.AppSettings["FilePath"];
            string filePath = Path.Combine(strPath, file_name);

            if (!File.Exists(filePath))
            {
                file_name = file_name.Replace(".LITE", ".jdsu");
                filePath= Path.Combine(strPath, file_name);
            }


            if (File.Exists(filePath))
            {



                FileStream stream = new FileStream(filePath, FileMode.Open);
                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);                          
                response.Content = new StreamContent(stream);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = System.Web.HttpUtility.UrlEncode(file_name)
                };
                response.Headers.Add("Access-Control-Expose-Headers", "FileName");
                response.Headers.Add("FileName", System.Web.HttpUtility.UrlEncode(file_name));
                return response;

            }
            else
            {
                Obj_Return.Success = false;
                Obj_Return.Error_Message = "File is not exists";
                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK, Obj_Return);
                return response;
            }
            }

        private Boolean GenerateSNFile()
        {
            string StrFilename = obj_issuedSN.Venuid + "__" + obj_issuedSN.strCalcFromSN + "__" + obj_issuedSN.strCalcTOSN + ".LITE";
            string Str_Path = System.Configuration.ConfigurationManager.AppSettings["FilePath"];
            string StrLongFileName = Path.Combine(Str_Path, StrFilename);
            obj_issuedSN.Filename = StrFilename;
            if (write_file(StrLongFileName,string.Join("\r\n",obj_issuedSN.ListSN)))
            {
                return true;
            }
            else
            {
                return false;
            }






            
        }


        int String_2_ASCII( string str)
        {

            char[] cc= str.ToCharArray();

            return (int)(cc[0]);

         

        }

        string ASCII_2_STR(int ASCII_CODE)
        {
            byte[] array = new byte[1];
            array[0] = (byte)(Convert.ToInt16(ASCII_CODE));
            return Convert.ToString(System.Text.Encoding.ASCII.GetString(array));
        }


        private Boolean write_file(string filpath, string writestr)
        { 
         string  myfiledir = filpath.Substring(0, filpath.LastIndexOf("\\"));
        if (  !Directory.Exists(myfiledir) )
        {
                Directory.CreateDirectory(myfiledir);
        }

            try
            {
                StreamWriter mystream = new StreamWriter(filpath, true);
                mystream.WriteLine(writestr);
                mystream.Close();
                return true;
            }
        catch(Exception ex)
            {
                return false;

            }
             
  }

    }
}
