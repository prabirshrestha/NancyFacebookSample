
namespace NancyFacebookCanvasSample
{
    using System.Collections.Generic;
    using System.Dynamic;
    using Nancy;
    using Nancy.Extensions;
    using Nancy.Facebook;
    using Nancy.Facebook.Responses;
    using System;

    public static class CustomFacebookExtensions
    {
        public const string FacebookPermsKey = "fb_perms";

        #region Facebook Permissions

        public static string[] FacebooPermissions(this NancyContext context)
        {
            return context.Items.ContainsKey(FacebookPermsKey) ? context.Items[FacebookPermsKey] as string[] : new string[0];
        }

        #endregion

        #region Handle OAuth Dialog Error

        public const string QueryStringsToDrop = "code,error_reason,error,error_description";

        public static void HandleFacebookOAuthDialogError(this NancyModule module, string appId, string redirectUri = null, string scope = null, string state = null, IDictionary<string, object> parameters = null, string queryStringsToDrop = QueryStringsToDrop)
        {
            module.Before.AddItemToEndOfPipeline(ctx => HandleFacebookOAuthDialogErrorInternal(module, appId, redirectUri, scope, state, parameters, queryStringsToDrop));
        }

        private static Response HandleFacebookOAuthDialogErrorInternal(NancyModule module, string appId, string redirectUri = null, string scope = null, string state = null, IDictionary<string, object> parameters = null, string queryStringsToDrop = QueryStringsToDrop)
        {
            var request = module.Context.Request;
            if (request.Query.error.HasValue && request.Query.error_description.HasValue)
            {
                dynamic model = new ExpandoObject();
                model.error = request.Query.error.Value;
                model.error_description = request.Query.error_description.Value;
                model.facebook_login_url = module.Context.FacebookLoginUrl(appId, redirectUri, scope, state, parameters, queryStringsToDrop);
                return module.View["FacebookLoginError", model];
            }

            return null;
        }

        #endregion

        #region Handle OAuth Dialog Code

        public static void DropFacebookQueryStrings(this NancyModule module, string queryStringsToDrop = QueryStringsToDrop)
        {
            module.Before.AddItemToEndOfPipeline(ctx => DropFacebookQueryStringsInternal(module, queryStringsToDrop));
        }

        private static Response DropFacebookQueryStringsInternal(NancyModule module, string queryStringsToDrop = QueryStringsToDrop)
        {
            var request = module.Context.Request;
            if (!string.IsNullOrEmpty(request.Headers.Referrer))
            {
                var referralUri = new Uri(request.Headers.Referrer);
                if (referralUri.Host == "apps.facebook.com" || referralUri.Host == "apps.beta.facebook.com")
                {
                    string newUrl;
                    if (FacebookNancyExtensions.DropQueryStrings(request.Headers.Referrer, out newUrl, queryStringsToDrop))
                        return new FacebookAppRedirectResponse(newUrl);
                }
            }

            return null;
        }

        #endregion
    }
}