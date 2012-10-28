using System;
using Nancy;
using Nancy.Session;
using WorldDomination.Web.Authentication;
using WorldDomination.Web.Authentication.Facebook;

namespace NancyFacebookWebsiteSample
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(TinyIoC.TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

            RegisterAuthenticationProviders(container);
        }

        private void RegisterAuthenticationProviders(TinyIoC.TinyIoCContainer container)
        {
            const string rootUrl = "http://localhost:1777/"; // must end with /

            const string facebookAppId = ""
            const string facebookAppSecret = ""

            var facebookProvider = new FacebookProvider(facebookAppId, facebookAppSecret, new Uri(rootUrl + "login/facebook/callback"));

            var authenticationService = new AuthenticationService();
            authenticationService.AddProvider(facebookProvider);

            container.Register<IAuthenticationService>(authenticationService);
        }

        protected override void RequestStartup(TinyIoC.TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines, NancyContext context)
        {
            base.RequestStartup(container, pipelines, context);

            CookieBasedSessions.Enable(pipelines);

            pipelines.BeforeRequest.AddItemToEndOfPipeline(SetFacebookUser);
            pipelines.BeforeRequest.AddItemToEndOfPipeline(ctx => SetFacebookAccessTokenInFacebookClient(container, ctx));
        }

        private Response SetFacebookUser(NancyContext context)
        {
            context.CurrentUser = (User)context.Request.Session["User"];
            return null;
        }

        private Response SetFacebookAccessTokenInFacebookClient(TinyIoC.TinyIoCContainer container, NancyContext context)
        {
            var user = context.CurrentUser as User;
            if (user != null)
            {
                var fb = new Facebook.FacebookClient { AccessToken = user.FacebookAccessToken };
                container.Register(fb);
            }

            return null;
        }
    }
}