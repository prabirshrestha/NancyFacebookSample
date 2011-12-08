namespace Facebook.Web
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

    class FacebookWebHelper
    {
        #region Signed Request

        private const string InvalidSignedRequest = "Invalid signed_request";

        public static object TryParseFacebookSignedRequest(string appSecret, string signedRequestValue, Func<string, object> deserializeObject, bool throws)
        {
            if (string.IsNullOrEmpty(appSecret))
                throw new ArgumentNullException("appSecret");
            if (string.IsNullOrEmpty(signedRequestValue))
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

                var envelope = (IDictionary<string, object>)deserializeObject(Encoding.UTF8.GetString(Base64UrlDecode(encodedEnvelope)));
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

        public static object TryParseFacebookSignedRequest(string appId, string appSecret, Func<bool> isInCache, Func<object> getFromCache, Func<string> getSignedRequestFormValue, Func<string, string> getSignedRequestCookieValue, Action<object> cache, Func<string, object> deserializeObject, bool throws)
        {
            object signedRequest = null;

            if (isInCache())
            {
                // return the cached value if present
                signedRequest = getFromCache();
            }
            else
            {
                // check signed_request value from Form for canvas/tabs/page applications
                var signedRequestValue = getSignedRequestFormValue();
                if (!string.IsNullOrEmpty(signedRequestValue))
                    signedRequest = TryParseFacebookSignedRequest(appSecret, signedRequestValue, deserializeObject, throws);

                if (signedRequest == null)
                {
                    // if signed_request is null, check from the fb cookie set by FB JS SDK
                    if (string.IsNullOrEmpty(appSecret))
                        throw new ArgumentNullException("appId");
                    var signedRequestCookieValue = getSignedRequestCookieValue("fbsr_" + appId);
                    if (!string.IsNullOrEmpty(signedRequestCookieValue))
                        signedRequest = TryParseFacebookSignedRequest(appSecret, signedRequestCookieValue, deserializeObject, throws);
                }
                cache(signedRequest);
            }

            return signedRequest;
        }

        #endregion

        #region Canvas Application

        public static string FacebookCanvasPageUrl(string canvasPageOrAppName,
            Func<string> requestUrlScheme, Func<bool> isReferrerBeta,
            bool? https = null, bool? beta = null)
        {
            if (string.IsNullOrEmpty(canvasPageOrAppName))
                throw new ArgumentNullException("canvasPageOrAppName");

            var sb = new StringBuilder();

            if (https.HasValue)
                sb.Append(https.Value ? "https" : "http");
            else
                sb.Append(requestUrlScheme());
            sb.Append("://");

            bool useBeta = beta.HasValue ? beta.Value : isReferrerBeta();
            sb.Append(useBeta ? "apps.beta.facebook.com" : "apps.facebook.com");

            canvasPageOrAppName = RemoveTrailingSlash(canvasPageOrAppName);
            sb.Append(canvasPageOrAppName.Contains("/")
                          ? canvasPageOrAppName.Substring(canvasPageOrAppName.LastIndexOf('/'))
                          : "/" + canvasPageOrAppName);

            return sb.ToString();
        }

        #endregion

        #region Facebook Application Helper

        public static string FacebookAppRedirectScript(string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException(url);

            return string.Concat("<script type=\"text/javascript\">top.location.href = '", url, "';</script>");
        }

        #endregion

        #region OAuth Dialog Login Helper

        public const string QueryStringsToDrop = "code,error_reason,error,error_description";

        public static string FacebookLoginUrl(string appId, string redirectUri, string scope = null, string state = null, IDictionary<string, object> parameters = null, string referrer = null, string queryStringsToDrop = QueryStringsToDrop)
        {
            var defaultParameters = new Dictionary<string, object>();
            defaultParameters["client_id"] = appId;
            defaultParameters["redirect_uri"] = redirectUri;

            if (!string.IsNullOrEmpty(scope))
                defaultParameters["scope"] = scope;

            if (!string.IsNullOrEmpty(state))
                defaultParameters["state"] = state;

            var mergedParameters = Merge(defaultParameters, parameters);

            if (mergedParameters["client_id"] == null || string.IsNullOrEmpty(mergedParameters["client_id"].ToString()))
                throw new ArgumentException("client_id requried.");

            if (mergedParameters["redirect_uri"] == null || string.IsNullOrEmpty(mergedParameters["redirect_uri"].ToString()))
            {
                bool containsRedirectUri = false;
                if (!string.IsNullOrEmpty(referrer))
                {
                    var referralUri = new Uri(referrer);
                    if (referralUri.Host == "apps.facebook.com" || referralUri.Host == "apps.beta.facebook.com")
                    {
                        containsRedirectUri = true;
                        mergedParameters["redirect_uri"] = referralUri;
                    }
                }

                if (!containsRedirectUri)
                    throw new ArgumentException("redirect_uri requried.");
            }

            string newRedirectUri = null;
            DropQueryStrings(mergedParameters["redirect_uri"].ToString(), out newRedirectUri, queryStringsToDrop);
            mergedParameters["redirect_uri"] = newRedirectUri;

            var sb = new StringBuilder();

            sb.Append("https://www.facebook.com/dialog/oauth/?");

            foreach (var kvp in mergedParameters)
            {
                if (kvp.Value != null)
                    sb.AppendFormat("{0}={1}&", kvp.Key, Uri.EscapeDataString(kvp.Value.ToString()));
            }

            sb.Length--;

            return sb.ToString();
        }

        #endregion

        #region Helper Methods

        public static bool DropQueryStrings(string url, out string newUrl, string queryStringsToDrop = FacebookWebHelper.QueryStringsToDrop)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException("url");

            if (string.IsNullOrEmpty(queryStringsToDrop))
                newUrl = url;

            var returnValue = false;
            var qsToDrop = queryStringsToDrop.Split(',');
            var redirectUriSplit = url.ToString().Split('?');
            if (redirectUriSplit.Length == 2 && !string.IsNullOrEmpty(redirectUriSplit[1]))
            {
                var newRedirectUri = new StringBuilder();
                newRedirectUri.Append(redirectUriSplit[0]);
                newRedirectUri.Append('?');

                string fragment = null;
                // contains fragment
                if (redirectUriSplit[1].Contains('#'))
                {
                    var fragmentIndex = redirectUriSplit[1].LastIndexOf('#');
                    fragment = redirectUriSplit[1].Substring(fragmentIndex);
                    redirectUriSplit[1] = redirectUriSplit[1].Substring(0, fragmentIndex);
                }

                var queryStrings = redirectUriSplit[1].Split('&');
                foreach (var qs in queryStrings)
                {
                    var kvp = qs.Split('=');
                    if (kvp.Length == 2)
                    {
                        if (qs.Contains(kvp[0]))
                            returnValue = true;
                        else
                            newRedirectUri.AppendFormat("{0}={1}&", kvp[0], kvp[1]);
                    }
                }

                newRedirectUri.Length--;

                // don't add fragment added by Facebook
                if (returnValue && !string.IsNullOrEmpty(fragment) && fragment != "#_=_")
                    newRedirectUri.Append(fragment);

                newUrl = newRedirectUri.ToString();
            }
            else
            {
                newUrl = url;
            }

            return returnValue;
        }

        /// <summary>
        /// Base64 Url decode.
        /// </summary>
        /// <param name="base64UrlSafeString">
        /// The base 64 url safe string.
        /// </param>
        /// <returns>
        /// The base 64 url decoded string.
        /// </returns>c
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

        /// <summary>
        /// Merges two dictionaries.
        /// </summary>
        /// <param name="first">Default values, only used if second does not contain a value.</param>
        /// <param name="second">Every value of the merged object is used.</param>
        /// <returns>The merged dictionary</returns>
        private static IDictionary<TKey, TValue> Merge<TKey, TValue>(IDictionary<TKey, TValue> first, IDictionary<TKey, TValue> second)
        {
            first = first ?? new Dictionary<TKey, TValue>();
            second = second ?? new Dictionary<TKey, TValue>();
            var merged = new Dictionary<TKey, TValue>();

            foreach (var kvp in second)
                merged.Add(kvp.Key, kvp.Value);

            foreach (var kvp in first)
            {
                if (!merged.ContainsKey(kvp.Key))
                    merged.Add(kvp.Key, kvp.Value);
            }

            return merged;
        }

        #endregion
    }
}