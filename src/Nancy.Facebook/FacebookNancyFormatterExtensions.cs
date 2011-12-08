
namespace Nancy.Facebook
{
    using Nancy.Facebook.Responses;
    using System.Collections.Generic;
    using global::Facebook.Web;

    public static class FacebookNancyFormatterExtensions
    {
        public static Response AsFacebookAppRedirect(this IResponseFormatter formatter, string url)
        {
            return new FacebookAppRedirectResponse(url);
        }

        public static Response AsFacebookLogin(this IResponseFormatter formatter, string appId, string redirectUri = null, string scope = null, string state = null, IDictionary<string, object> parameters = null, string queryStringsToDrop = FacebookWebHelper.QueryStringsToDrop)
        {
            var request = formatter.Context == null ? null : formatter.Context.Request;
            var referrer = request == null ? null : request.Headers.Referrer;
            var url = FacebookWebHelper.FacebookLoginUrl(appId, redirectUri, scope, state, parameters, referrer, queryStringsToDrop);
            return formatter.AsFacebookAppRedirect(url);
        }
    }
}
