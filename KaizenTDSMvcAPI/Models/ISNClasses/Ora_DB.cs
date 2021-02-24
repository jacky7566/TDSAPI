using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using Oracle.ManagedDataAccess.Client;


namespace KaizenTDSMvcAPI.Models.Classes
{

    public class Ora_DB
    {
        string _connstring;

        public Ora_DB(string connstring)
            {
            _connstring=connstring;
            _connstring = _connstring.Replace("[Userid]", "webstart");
            _connstring = _connstring.Replace("[Password]", "webstart");
            }

        public OracleConnection GetConn()
        {
            // Dim Str_conn As String = _Str_DB_Conn

            string str_conn = _connstring;
            OracleConnection conn = new OracleConnection();
            conn.ConnectionString = str_conn;
            try
            {
                conn.Open();
                conn.Close();
                return conn;
            }
            catch (Exception EX)
            {
                return conn;
            }        
                

        }

        public Boolean   GetDBSet(string sqlstr,ref DataSet dbset, ref string error_message)
        {
            OracleDataAdapter OraDBAdapter = new OracleDataAdapter(sqlstr, GetConn());
           // DataSet dbset = new DataSet();
                try
            {

                OraDBAdapter.SelectCommand.Connection.Open();
                OraDBAdapter.Fill(dbset);
                OraDBAdapter.SelectCommand.Connection.Close();
                error_message = "";
                return true;
            }
            catch (Exception ex)
            {
                error_message = ex.Message;
                return false;
            }
        }

        public Boolean SP_2_DBSet(string sp_name, List<OracleParameter> Para_List, ref DataSet O_DBSet,ref string error_message)
        {
            OracleCommand OraCMD = new OracleCommand( );
            OraCMD.CommandText = sp_name;
            OraCMD.Connection = GetConn();
            OraCMD.CommandType = CommandType.StoredProcedure;

           // DataSet dbset = new DataSet();

            foreach  (OracleParameter Param in Para_List)
                {
                OraCMD.Parameters.Add(Param);
            }
            OracleDataAdapter OraDBAdapter = new OracleDataAdapter(OraCMD);
            try
            {
                OraCMD.Connection.Open();
                OraDBAdapter.Fill(O_DBSet);
                OraCMD.Connection.Close();
                return true;
            }
            catch (Exception ex)
                {
                error_message = ex.Message;
                return false;
            }

        }

        public Boolean SP_2_String(string sp_name, List<OracleParameter> Para_List, ref string O_Str )
        {
            OracleCommand OraCMD = new OracleCommand();
            OraCMD.CommandText = sp_name;
            OraCMD.CommandType = CommandType.StoredProcedure;
            // DataSet dbset = new DataSet();

            foreach (OracleParameter Param in Para_List)
            {
                OraCMD.Parameters.Add(Param);
            }
          
            try
            {
                OraCMD.Connection.Open();
                //  OraDBAdapter.Fill(O_DBSet);
                OraCMD.ExecuteNonQuery();
                OraCMD.Connection.Close();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }



    }
}