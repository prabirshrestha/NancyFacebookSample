namespace NancyFacebookCanvasSample
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Facebook;
    using Nancy;
    using Nancy.Facebook;

    public partial class Bootstrapper
    {
        private void ConfigureFacebookRequestContainer(TinyIoC.TinyIoCContainer container, NancyContext context)
        {
            var fb = new FacebookClient();
            if (context.Request != null)
            {
                fb.IsSecureConnection = context.Request.Url.Scheme == "https";
                if (!string.IsNullOrWhiteSpace(context.Request.Headers.Referrer))
                    fb.UseFacebookBeta = new Uri(context.Request.Headers.Referrer).Host == "apps.beta.facebook.com";
            }
            container.Register(fb);

            container.Register<IFacebookApplication>(
                (c, o) => new DefaultFacebookApplication
                              {
                                  AppId = ",
                                  AppSecret = ",
                                  //SiteUrl = "http://localhost:10537/",
                                  CanvasUrl = "http://localhost:10537/",
                                  SecureCanvasUrl = "https://localhost:44300/",
                                  CanvasPage = "http://apps.facebook.com/
                              });
        }

        private Response FacebookRequestStartup(TinyIoC.TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines, NancyContext context)
        {
            if (context.Request != null)
            {
                string[] perms = null;
                dynamic signedRequest;
                var fbApp = container.Resolve<IFacebookApplication>();
                if (context.TryParseFacebookSignedRequest(fbApp.AppId, fbApp.AppSecret, out signedRequest))
                {
                    if (((System.Collections.Generic.IDictionary<string, object>)signedRequest).ContainsKey("oauth_token"))
                    {
                        var fb = container.Resolve<FacebookClient>();
                        fb.AccessToken = signedRequest.oauth_token;
                        try
                        {
                            var result = (IDictionary<string, object>)fb.Get("me/permissions");
                            perms = ((IDictionary<string, object>)((IList<object>)result["data"])[0]).Keys.ToArray();
                        }
                        catch (FacebookOAuthException)
                        {
                            // access token is invalid so perms=none
                            // but don't throw exception.
                            fb.AccessToken = null;  
                        }
                    }
                }

                context.Items[CustomFacebookExtensions.FacebookPermsKey] = perms ?? new string[0];
            }

            return null;
        }
    }
}