
namespace NancyFacebookSample
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Nancy;

    public static class FacebookExtensions
    {
        private const string InvalidSignedRequest = "invalid signed_request";

        public static object ParseFacebookSignedRequest(string appSecret, string signedRequestValue)
        {
            return TryParseFacebookSignedRequestInternal(appSecret, signedRequestValue, true);
        }

        public static bool TryParseFacebookSignedRequest(string appSecret, string signedRequestValue, out object signedRequest)
        {
            signedRequest = TryParseFacebookSignedRequestInternal(appSecret, signedRequestValue, false);
            return signedRequest != null;
        }

        public static object ParseFacebookSignedRequest(this Request request, string appId, string appSecret)
        {
            return TryParseFacebookSignedRequestInternal(request, appId, appSecret, true);
        }

        public static bool TryParseFacebookSignedRequest(this Request request, string appId, string appSecret, out object signedRequest)
        {
            signedRequest = TryParseFacebookSignedRequestInternal(request, appId, appSecret, false);
            return signedRequest != null;
        }

        private static object TryParseFacebookSignedRequestInternal(string appSecret, string signedRequestValue, bool throws)
        {
            if (string.IsNullOrWhiteSpace(appSecret))
                throw new ArgumentNullException("appSecret");
            if (string.IsNullOrWhiteSpace(signedRequestValue))
                throw new ArgumentNullException("signedRequestValue");

            try
            {
                string[] split = signedRequestValue.Split('.');
                if (split.Length != 2)
                {
                    // need to have exactly 2 parts
                    throw new InvalidOperationException(InvalidSignedRequest);
                }

                string encodedignature = split[0];
                string encodedEnvelope = split[1];

                if (string.IsNullOrWhiteSpace(encodedignature) || string.IsNullOrWhiteSpace(encodedEnvelope))
                    throw new InvalidOperationException(InvalidSignedRequest);

                var envelope = (IDictionary<string, object>)Facebook.JsonSerializer.Current.DeserializeObject(Encoding.UTF8.GetString(Base64UrlDecode(encodedEnvelope)));
                var algorithm = (string)envelope["algorithm"];
                if (!algorithm.Equals("HMAC-SHA256"))
                    throw new InvalidOperationException("Unknown algorithm. Expected HMAC-SHA256");

                byte[] key = Encoding.UTF8.GetBytes(appSecret);
                byte[] digest = ComputeHmacSha256Hash(Encoding.UTF8.GetBytes(encodedEnvelope), key);

                if (!digest.SequenceEqual(Base64UrlDecode(encodedignature)))
                    throw new InvalidOperationException(InvalidSignedRequest);
                return envelope;
            }
            catch
            {
                if (throws)
                    throw;
                return null;
            }
        }

        private static object TryParseFacebookSignedRequestInternal(Request request, string appId, string appSecret, bool throws)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            if (string.IsNullOrWhiteSpace(appSecret))
                throw new ArgumentNullException("appSecret");

            object signedRequest = null;
            if (request.Form.signed_request.HasValue && !string.IsNullOrWhiteSpace(request.Form.signed_request))
            {
                signedRequest = TryParseFacebookSignedRequestInternal(appSecret, request.Form.signed_request, throws);
            }

            if (signedRequest == null)
            {
                if(string.IsNullOrWhiteSpace(appId))
                    throw new ArgumentNullException("appId");
                string signedRequestCookieValue;
                if (request.Cookies.TryGetValue("fbsr_" + appId, out signedRequestCookieValue))
                {
                    if (!string.IsNullOrWhiteSpace(signedRequestCookieValue))
                        signedRequest = TryParseFacebookSignedRequestInternal(appSecret, signedRequestCookieValue, throws);
                }
            }

            return signedRequest;
        }

        /// <summary>
        /// Base64 Url decode.
        /// </summary>
        /// <param name="base64UrlSafeString">
        /// The base 64 url safe string.
        /// </param>
        /// <returns>
        /// The base 64 url decoded string.
        /// </returns>
        private static byte[] Base64UrlDecode(string base64UrlSafeString)
        {
            if (string.IsNullOrEmpty(base64UrlSafeString))
                throw new ArgumentNullException("base64UrlSafeString");

            base64UrlSafeString = base64UrlSafeString.PadRight(base64UrlSafeString.Length + (4 - base64UrlSafeString.Length % 4) % 4, '=');
            base64UrlSafeString = base64UrlSafeString.Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(base64UrlSafeString);
        }

        /// <summary>
        /// Computes the Hmac Sha 256 Hash.
        /// </summary>
        /// <param name="data">
        /// The data to hash.
        /// </param>
        /// <param name="key">
        /// The hash key.
        /// </param>
        /// <returns>
        /// The Hmac Sha 256 hash.
        /// </returns>
        private static byte[] ComputeHmacSha256Hash(byte[] data, byte[] key)
        {
            using (var crypto = new System.Security.Cryptography.HMACSHA256(key))
            {
                return crypto.ComputeHash(data);
            }
        }
    }
}