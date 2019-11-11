using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;

namespace CognitoDemo.Api.Controllers
{
    public class DateTimeController : ApiController
    {
        [Authorize]
        public ResponseMessageResult Get()
        {
            return ResponseMessage(new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = Request,
                Content = new StringContent(DateTime.Now.ToString())
            });
        }
    }
}
