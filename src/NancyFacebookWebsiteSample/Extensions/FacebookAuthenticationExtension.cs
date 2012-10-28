using Nancy;
using Nancy.Extensions;
using Nancy.Helpers;
using Nancy.Responses;

namespace NancyFacebookWebsiteSample.Extensions
{
    public static class FacebookAuthenticationExtension
    {
        public static void RequiresFacebookAuthentication(this NancyModule module)
        {
            module.Before.AddItemToStartOfPipeline(RedirectToLoginIfFacebookNotAuthorized);
        }

        private static Response RedirectToLoginIfFacebookNotAuthorized(NancyContext ctx)
        {
            if (ctx.CurrentUser == null)
            {
                return new RedirectResponse(ctx.ToFullPath("~/login/facebook?next=" + HttpUtility.UrlEncode(ctx.Request.Url.ToString())));
            }

            return null;
        }
    }
}