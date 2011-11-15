namespace NancyFacebookCanvasSample
{
    using System;
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
                                  SiteUrl = "http://localhost:10537/",
                                  CanvasUrl = "http://localhost:10537/",
                                  SecureCanvasUrl = "https://localhost:44303/",
                                  CanvasPage = "http://apps.facebook.com/appname
                              });
        }

        private Response FacebookRequestStartup(TinyIoC.TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines, NancyContext context)
        {
            if (context.Request != null)
            {
                dynamic signedRequest;
                var fbApp = container.Resolve<IFacebookApplication>();
                if (context.TryParseFacebookSignedRequest(fbApp.AppId, fbApp.AppSecret, out signedRequest))
                {
                    var fb = container.Resolve<FacebookClient>();
                    if (((System.Collections.Generic.IDictionary<string, object>)signedRequest).ContainsKey("oauth_token"))
                        fb.AccessToken = signedRequest.oauth_token;
                }
            }
            return null;
        }
    }
}