using Microsoft.Net.Http.Headers;
using Roadie.Library.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roadie.Library.Utility
{
    public static class EtagHelper
    {
        public static EntityTagHeaderValue GenerateETag(IHttpEncoder encoder, byte[] bytes)
        {
            if(encoder == null || bytes == null || !bytes.Any())
            {
                return null;
            }
            return new EntityTagHeaderValue($"\"{ encoder.UrlEncodeBase64(HashHelper.CreateMD5(bytes)) }\"");
        }

        public static bool CompareETag(IHttpEncoder encoder, EntityTagHeaderValue eTagLeft, EntityTagHeaderValue eTagRight)
        {
            if(eTagLeft == null && eTagRight == null)
            {
                return true;
            }
            if(eTagLeft == null && eTagRight != null)
            {
                return false;
            }
            if (eTagRight == null && eTagLeft != null)
            {
                return false;
            }
            return eTagLeft == eTagRight;
        }

        public static bool CompareETag(IHttpEncoder encoder, EntityTagHeaderValue eTag, byte[] bytes)
        {
            if(eTag == null && (bytes == null || !bytes.Any()))
            {
                return true;
            }
            if(eTag == null && (bytes != null) || bytes.Any())
            {
                return false;
            }
            if(eTag != null && (bytes == null || !bytes.Any()))
            {
                return false;
            }
            var etag = EtagHelper.GenerateETag(encoder, bytes);
            return eTag == etag;
        }
    }
}
