using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Filters;

namespace KaizenTDSMvcAPI.Filters
{
    public class KaizenTDSExceptionFilter: FilterAttribute
    {
        public void OnException(ExceptionContext filterContext)
        {
            throw new Exception(filterContext.Exception.ToString());
        }
    }
}