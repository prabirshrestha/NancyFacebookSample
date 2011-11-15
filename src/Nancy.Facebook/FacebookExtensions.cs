
namespace Nancy.Facebook
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Nancy;
    using SimpleJson;

    public static class FacebookExtensions
    {
        #region Canvas Application

        public static string FacebookCanvasPageUrl(this NancyContext context, string canvasPage,
            bool? https = null, bool? beta = null)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (context.Request == null)
                throw new ArgumentException("context.Request is null");
            if (string.IsNullOrWhiteSpace(canvasPage))
                throw new ArgumentNullException("canvasPage");

            var sb = new StringBuilder();
            var request = context.Request;

            if (https.HasValue)
                sb.Append(https.Value ? "https" : "http");
            else
                sb.Append(request.Url.Scheme);
            sb.Append("://");

            bool useBeta = false;
            if (beta.HasValue)
            {
                useBeta = beta.Value;
            }
            else
            {
                if (request.Headers != null && !string.IsNullOrWhiteSpace(request.Headers.Referrer))
                {
                    var referrer = new Uri(request.Headers.Referrer);
                    useBeta = referrer.Host == "apps.beta.facebook.com";
                }
            }

            sb.Append(useBeta ? "apps.beta.facebook.com" : "apps.facebook.com");
            canvasPage = RemoveTrailingSlash(canvasPage);
            sb.Append(canvasPage.Contains("/") ? new Uri(canvasPage).PathAndQuery : "/" + canvasPage);

            return sb.ToString();
        }

        /// <summary>
        /// Removes the trailing slash.
        /// </summary>
        /// <param name="input">
        /// The input string to remove the trailing slash from.
        /// </param>
        /// <returns>
        /// The string with trailing slash removed.
        /// </returns>
        private static string RemoveTrailingSlash(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return input.EndsWith("/") ? input.Substring(0, input.Length - 1) : input;
        }

        #endregion

        #region SignedRequest

        private const string InvalidSignedRequest = "invalid signed_request";
        private const string SignedRequestKey = "facebook_signed_request_nancy";

        public static object ParseFacebookSignedRequest(string appSecret, string signedRequestValue)
        {
            return TryParseFacebookSignedRequestInternal(appSecret, signedRequestValue, true);
        }

        public static bool TryParseFacebookSignedRequest(string appSecret, string signedRequestValue, out object signedRequest)
        {
            signedRequest = TryParseFacebookSignedRequestInternal(appSecret, signedRequestValue, false);
            return signedRequest != null;
        }

        public static object ParseFacebookSignedRequest(this NancyContext context, string appId, string appSecret)
        {
            return TryParseFacebookSignedRequestInternal(context, appId, appSecret, true);
        }

        public static bool TryParseFacebookSignedRequest(this NancyContext context, string appId, string appSecret, out object signedRequest)
        {
            signedRequest = TryParseFacebookSignedRequestInternal(context, appId, appSecret, false);
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

                var envelope = (IDictionary<string, object>)SimpleJson.DeserializeObject(Encoding.UTF8.GetString(Base64UrlDecode(encodedEnvelope)));
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

        private static object TryParseFacebookSignedRequestInternal(NancyContext context, string appId, string appSecret, bool throws)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (string.IsNullOrWhiteSpace(appSecret))
                throw new ArgumentNullException("appSecret");

            var request = context.Request;
            var items = context.Items;
            object signedRequest = null;

            if (items.ContainsKey(SignedRequestKey))
            {
                // return the cached value if present
                signedRequest = items[SignedRequestKey] as IDictionary<string, object>;
            }
            else
            {
                // check signed_request value from Form for canvas/tabs/page applications
                if (request.Form.signed_request.HasValue && !string.IsNullOrWhiteSpace(request.Form.signed_request))
                    signedRequest = TryParseFacebookSignedRequestInternal(appSecret, request.Form.signed_request, throws);

                if (signedRequest == null)
                {
                    // if signed_request is null, check from the fb cookie set by FB JS SDK
                    if (string.IsNullOrWhiteSpace(appId))
                        throw new ArgumentNullException("appId");
                    string signedRequestCookieValue;
                    if (request.Cookies.TryGetValue("fbsr_" + appId, out signedRequestCookieValue))
                    {
                        if (!string.IsNullOrWhiteSpace(signedRequestCookieValue))
                            signedRequest = TryParseFacebookSignedRequestInternal(appSecret, signedRequestCookieValue, throws);
                    }
                }
                items[SignedRequestKey] = signedRequest;
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

            base64UrlSafeString =
                base64UrlSafeString.PadRight(base64UrlSafeString.Length + (4 - base64UrlSafeString.Length % 4) % 4, '=');
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
            using (var crypto = new HMACSHA256(key))
            {
                return crypto.ComputeHash(data);
            }
        }

        #endregion

    }
}