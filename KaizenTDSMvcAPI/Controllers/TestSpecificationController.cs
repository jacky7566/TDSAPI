using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Cors;

namespace APortalAPI.Controllers
{
    //Build by Balaji 20201112
    /// <summary>
    /// TestSpecification
    /// </summary>
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("api/TestSpecification")]
    public class TestSpecificationController : ApiController
    {
        /// <summary>
        /// TestSpec
        /// </summary>
        /// <param name="mylist"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("TestSpec")]        
        public HttpResponseMessage TestSpec([FromBody] TestSpecification[] mylist)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            foreach (var element in mylist)
            {
                if(element.TESTSPECIFICATIONID > 0)
                {
                    // Update - Call Generic/1.0/TESTSPECIFICATION/UPDATE
                }
                else
                {
                    // Insert - Call Generic/1.0/TESTSPECIFICATION/INSERT
                }
            }
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return response;
        }
    }
    public class TestSpecification
    {
        public Int32 TESTSPECIFICATIONID { get; set; }
        public int TESTSPECIFICATIONSREVID { get; set; }
        public string TESTSPECIFICATIONDESC { get; set; }
        public int PARAMETERID { get; set; }
        public int COMPOPERATORID { get; set; }
        public decimal SPECMIN { get; set; }
        public decimal SPECMAX { get; set; }
        public string SPECSTRING { get; set; }
        public decimal TARGETMIN { get; set; }
        public decimal TARGETMAX { get; set; }
        public decimal CALIBRATIONMIN { get; set; }
        public decimal CALIBRATIONMAX { get; set; }
        public int PRIORITY { get; set; }
        public int PARETOCODEID { get; set; }
        public string FORMAT { get; set; }
        public int REGRADE { get; set; }
        public string COMMENTS { get; set; }
    }
}
