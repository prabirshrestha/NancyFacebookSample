
namespace Nancy.Facebook
{
    using System;
    using System.Collections.Generic;
    using global::Facebook.Web;
    using Nancy;

    public static class FacebookNancyExtensions
    {
        #region SignedRequest

        private const string SignedRequestKey = "facebook_signed_request_nancy";

        public static object ParseFacebookSignedRequest(string appSecret, string signedRequestValue)
        {
            return FacebookWebHelper.TryParseFacebookSignedRequest(appSecret, signedRequestValue, DeserializeObject, true);
        }

        public static bool TryParseFacebookSignedRequest(string appSecret, string signedRequestValue, out object signedRequest)
        {
            signedRequest = FacebookWebHelper.TryParseFacebookSignedRequest(appSecret, signedRequestValue, DeserializeObject, false);
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

        private static object TryParseFacebookSignedRequestInternal(NancyContext context, string appId, string appSecret, bool throws)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            var request = context.Request;
            var items = context.Items;

            return FacebookWebHelper.TryParseFacebookSignedRequest(
                appId, appSecret,
                () => items.ContainsKey(SignedRequestKey),
                () => items[SignedRequestKey] as IDictionary<string, object>,
                () => request.Form.signed_request.HasValue ? request.Form.signed_request : string.Empty,
                cookieName => request.Cookies.ContainsKey(cookieName) ? request.Cookies[cookieName] : string.Empty,
                signedRequest => items[SignedRequestKey] = signedRequest,
                DeserializeObject,
                throws);
        }

        #endregion

        #region Canvas Application

        public static string FacebookCanvasPageUrl(this NancyContext context, string canvasPageOrAppName,
            bool? https = null, bool? beta = null)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (context.Request == null)
                throw new ArgumentException("context.Request is null");

            var request = context.Request;

            return FacebookWebHelper.FacebookCanvasPageUrl(canvasPageOrAppName,
                () => request.Url.Scheme,
                () => request.Headers != null && !string.IsNullOrEmpty(request.Headers.Referrer) && new Uri(request.Headers.Referrer).Host == "apps.beta.facebook.com",
                https,
                beta);
        }

        #endregion

        #region Facebook Login Url

        public static string FacebookLoginUrl(this NancyContext context, string appId, string redirectUri = null, string scope = null, string state = null, IDictionary<string, object> parameters = null, string queryStringsToDrop = FacebookWebHelper.QueryStringsToDrop)
        {
            var request = context == null ? null : context.Request;
            var referrer = request == null ? null : request.Headers.Referrer;
            var url = FacebookWebHelper.FacebookLoginUrl(appId, redirectUri, scope, state, parameters, referrer, queryStringsToDrop);
            return url;
        }

        #endregion

        public static bool DropQueryStrings(string url, out string newUrl, string queryStringsToDrop = FacebookWebHelper.QueryStringsToDrop)
        {
            return FacebookWebHelper.DropQueryStrings(url, out newUrl, queryStringsToDrop);
        }

        private static object DeserializeObject(string json)
        {
            return SimpleJson.SimpleJson.DeserializeObject(json);
        }
    }
}