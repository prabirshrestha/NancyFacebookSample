using System;
using System.Collections.Specialized;
using Nancy;
using WorldDomination.Web.Authentication;

namespace NancyFacebookWebsiteSample.Modules
{
    public class FacebookAuthenticationModule : NancyModule
    {
        private const string AuthenticationServiceProviderKey = "Facebook";
        private const string FbCsrfKey = "FBCSRF";
        private const string FbNextUrl = "FBnext";

        public FacebookAuthenticationModule(IAuthenticationService authenticationService)
            : base("/login/facebook")
        {
            Get["/"] =
                _ =>
                {
                    var state = Guid.NewGuid().ToString("N");
                    Session[FbCsrfKey] = state;
                    Session[FbNextUrl] = (string)Request.Query.next;

                    var redirectUri = authenticationService.RedirectToAuthenticationProvider(AuthenticationServiceProviderKey, state);

                    return Response.AsRedirect(redirectUri.AbsoluteUri);
                };

            Get["/callback"] =
                _ =>
                {
                    var existingState = (string)Session[FbCsrfKey] ?? Guid.Empty.ToString();
                    var queryNameValueCollection = GetQueryAsNameValueCollection();

                    try
                    {
                        var result = authenticationService.CheckCallback(AuthenticationServiceProviderKey, queryNameValueCollection, existingState);

                        AuthenticateFacebookUser(result);
                        Session.Delete(FbCsrfKey);
                        Session.Delete(FbNextUrl);

                        var next = (string)Session[FbNextUrl];
                        if (!string.IsNullOrWhiteSpace(next))
                        {
                            return Response.AsRedirect(next);
                        }

                        return Response.AsRedirect("~/");
                    }
                    catch (Exception ex)
                    {
                        return Response.AsRedirect("~/");
                    }
                };
        }

        private NameValueCollection GetQueryAsNameValueCollection()
        {
            var queryParams = new NameValueCollection();
            foreach (var queryParam in Request.Query)
            {
                queryParams.Add(queryParam, Request.Query[queryParam]);
            }

            return queryParams;
        }

        private void AuthenticateFacebookUser(IAuthenticatedClient result)
        {
            var user = new User
                           {
                               UserName = result.UserInformation.UserName,

                               FacebookUserId = result.UserInformation.Id,
                               FacebookAccessToken = result.AccessToken,
                               FacebookAccessTokenExpiration = result.AccessTokenExpiresOn,
                           };

            Session["User"] = user;
        }
    }
}