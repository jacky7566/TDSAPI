using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KaizenTDSDLL
{
    public class FileIngestion
    {
        #region Auto-implemented Properties
        public string ErrorMessage;
        //public IDictionary<string, object> ResultList;
        public string ResultStr;
        #endregion        
        private string _APIRoute = "api/Generic/FileIngestion";
        private string _FilePath;
        private string _OptFormat;
        
        public FileIngestion(string FilePath, string SystemName = "")
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

                this._FilePath = FilePath;
                if (string.IsNullOrEmpty(SystemName) == false)
                {
                    this._APIRoute = string.Format("{0}/{1}", this._APIRoute, SystemName);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = Utilities.GetAllFootprints(ex);
            }
        }

        public bool Process()
        {
            if (File.Exists(this._FilePath) == false)
            {
                ErrorMessage = string.Format("File: {0} not exist!", this._FilePath);
            }

            try
            {
                using (WebClient wc = new WebClient())
                {
                    //wc.QueryString.Add("format", string.IsNullOrEmpty(this._OptFormat) ? "json" : this._OptFormat);
                    wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                    wc.Headers.Add("Authorization", "Bearer eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJVc2VyTmFtZSI6IkxJQzY3ODg4IiwiR1VJRCI6IjM2MTVhMTQ2LTRiOWUtNGE1YS1hNGJlLTRlMmQ4OTRhN2U5NCJ9.oe1Te0KJBm5skbYDNek0a1mgUMfBnr37tjPTRyJVeE8Fy-u21aH0129TCl1-FMwzary_h20hAdLAX30wd6XkuQ");
                    var result = wc.UploadFile(Utilities._APIUrl + this._APIRoute, _FilePath);
                    ResultStr = Encoding.UTF8.GetString(result);
                    if (string.IsNullOrEmpty(ResultStr) == false)
                    {
                        //ResultList = (IDictionary<string, object>)Utilities.DeserializeJson<object>(ResultStr);
                        return true;
                        //    var resObj = (IDictionary<string, object>)Utilities.DeserializeJson<object>(ReturnJSonStr);
                        //    bool.TryParse(resObj.Where(r => r.Key == "IsSuccess").FirstOrDefault().Value.ToString(), out IsSuccess);                        
                        //    if (IsSuccess)
                        //    {
                        //        int.TryParse(resObj.Where(r => r.Key == "Id").FirstOrDefault().Value.ToString(), out TestHeaderId);
                        //        return true;
                        //    }
                        //    else
                        //    {
                        //        ErrorMessage = resObj.Where(r => r.Key == "ErrorMessage").FirstOrDefault().Value.ToString();
                        //        LastVisitedXml = resObj.Where(r => r.Key == "LastVisitedXml").FirstOrDefault().Value.ToString();
                        //    }
                        //}
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = Utilities.GetAllFootprints(ex);
            }

            return false;
        }

        //private void SetFailReturnObjs(string message, string lastVistedXml)
        //{
        //    IsSuccess = false;
        //    Error_Message = message;
        //    Last_Visited_XML = lastVistedXml;
        //    TestHeaderId = 0;
        //}

        //private void SetPassReturnObjs(string message, int testHeaderId)
        //{
        //    IsSuccess = true;
        //    Error_Message = message;            
        //    TestHeaderId = testHeaderId;
        //}        
    }
}
