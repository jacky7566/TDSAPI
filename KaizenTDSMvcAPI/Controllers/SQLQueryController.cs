using KaizenTDSMvcAPI.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;

namespace KaizenTDSMvcAPI.Controllers
{
    /// <summary>
    /// SQLQuery Get/Post API
    /// </summary>
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("api/SQLQuery")]
    public class SQLQueryController : ApiController
    {
        /// <summary>
        /// Send SQL input to get the result by server
        /// </summary>
        /// <returns></returns>
        [Route("{APIConnectionName}/{version}/{apilookupname}")]
        public HttpResponseMessage Get(string APIConnectionName, string version, string apiLookupName)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            ConnectionHelper conHelper = new ConnectionHelper(APIConnectionName);

            try
            {
                var apiLkuList = LookupHelper.GetAPILookupList(apiLookupName, "GET", version, "SQL");
                if (apiLkuList != null && apiLkuList.Count() > 0)
                {
                    var queryKeyValues = Request.GetQueryNameValuePairs().ToList();
                    //Check if all key exisit
                    var apiLookUpItem = apiLkuList.FirstOrDefault();
                    var apiInputCnt = apiLookUpItem.COMMANDVALUE.Count(r => r.Equals("<%"));
                    //Check count between input and apilookup settings
                    if (apiInputCnt == queryKeyValues.Count())
                    {
                        foreach (var item in queryKeyValues)
                        {

                        }
                    }
                    else
                    {
                        resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotAcceptable,
                            string.Format("Wrong input xxxxxxxx - {0}", apiLookupName));
                    }
                    //string[] queryKeys = HttpUtility.ParseQueryString(query).AllKeys;
                }
                else
                {
                    resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.NotAcceptable,
                        string.Format("Wrong API Lookup Name - {0}", apiLookupName));
                }
            }
            catch (Exception ex)
            {
                resp = ExtensionHelper.LogAndResponse(null, HttpStatusCode.Conflict, ExtensionHelper.GetAllFootprints(ex));
                //ExtensionHelper.LogExpSPMessageToDB(ex, ExtensionHelper.GetAllFootprints(ex), HttpStatusCode.Conflict, "SQLQuery" + "_" + "GET", 2, JsonConvert.SerializeObject(Request.GetQueryNameValuePairs));
            }
            

            return resp;
        }
    }
}
