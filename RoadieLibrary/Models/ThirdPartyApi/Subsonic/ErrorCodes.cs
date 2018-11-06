using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Roadie.Models.ThirdPartyApi.Subsonic
{

    [DataContract]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class ErrorXmlResponse : BaseResponse
    {
        [XmlElement(ElementName = "error")]
        public error error { get; set; }
    }


    public class ErrorJsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public ErrorJsonResponse subsonicresponse { get; set; }
    }

    public class ErrorJsonResponse : BaseResponse
    {
        [DataMember(Name = "error")]
        public error error { get; set; }
    }


    public static class ErrorCodeHelper
    {
        public static error WrongUserNameOrPassword()
        {
            return ErrorCodeHelper.ErrorCodes().First(x => x.code == 40);
        }

        public static error RequestedDataNotFound()
        {
            return ErrorCodeHelper.ErrorCodes().First(x => x.code == 70);
        }

        public static error UserNotAuthorized()
        {
            return ErrorCodeHelper.ErrorCodes().First(x => x.code == 50);
        }

        public static error GenericError()
        {
            return ErrorCodeHelper.ErrorCodes().First(x => x.code == 0);
        }

        public static error RequiredParameterMissing()
        {
            return ErrorCodeHelper.ErrorCodes().First(x => x.code == 10);
        }

        public static List<error> ErrorCodes()
        {
            return new List<error>
            {
                new error { code = 0, message = "A generic error." },
                new error { code = 10, message = "Required parameter is missing." },
                new error { code = 20, message = "Incompatible Subsonic REST protocol version. Client must upgrade." },
                new error { code = 30, message = "Incompatible Subsonic REST protocol version. Server must upgrade." },
                new error { code = 40, message = "Wrong username or password." },
                new error { code = 41, message = "Token authentication not supported for LDAP users." },
                new error { code = 50, message = "User is not authorized for the given operation." },
                new error { code = 60, message = "The trial period for the Subsonic server is over. Please upgrade to Subsonic Premium. Visit subsonic.org for details." },
                new error { code = 70, message = "The requested data was not found." }
            };
        }
    }


    public class error
    {
        public int code { get; set; }
        public string message { get; set; }
    }


}
