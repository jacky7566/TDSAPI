using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KaizenTDSDLL
{
    public class SqlQuery
    {
        //public IDictionary<string, object> ResultList;
        public string ErrorMessage;
        public string ResultStr;        
        private string _APIRoute = "api/Generic";
        private string _OptFormat;

        public SqlQuery(string systemName = "", string version = "1.0")
        {
            Configuration config = null;
            string exeConfigPath = this.GetType().Assembly.Location;
            try
            {
                config = ConfigurationManager.OpenExeConfiguration(exeConfigPath);
                if (config != null)
                {
                    Utilities._APIUrl = Utilities.GetAppSetting(config, "APIUrl");
                    this._OptFormat = Utilities.GetAppSetting(config, "OutputFormat");
                }
                if (string.IsNullOrEmpty(systemName) == false)
                {
                    this._APIRoute = string.Format("{0}/{1}", this._APIRoute, systemName);
                }
                this._APIRoute = string.Format("{0}/{1}/SQLQUERY", this._APIRoute, version);
            }
            catch (Exception ex)
            {
                ErrorMessage = Utilities.GetAllFootprints(ex);
            }
        }

        public bool Process(string sqlCommand)
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    if (string.IsNullOrEmpty(sqlCommand) == false)
                    {
                        wc.QueryString.Add("format", string.IsNullOrEmpty(this._OptFormat) ? "json" : this._OptFormat);
                        wc.QueryString.Add("sqlcommand", sqlCommand);
                        ResultStr = wc.DownloadString(Utilities._APIUrl + this._APIRoute);
                        //ResultList = (IDictionary<string, object>)Utilities.DeserializeJson<object>(ResultStr);
                        return true;
                    }
                    else
                    {
                        ResultStr = "{\"Message\":\"Missing Sql Command\"}";
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = Utilities.GetAllFootprints(ex);
                ResultStr = "{\"Message\":\"Exception! Please check error message field!\"}";
            }

            return false;
        }
    }
}
