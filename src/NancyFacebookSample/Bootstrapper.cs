
namespace NancyFacebookSample
{
    using System;
    using Nancy;
    using Nancy.Authentication.Forms;
    using Nancy.Extensions;
    using Nancy.Facebook;
    using NancyFacebookSample.Models;

    public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(TinyIoC.TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

            var formsAuthConfig = new FormsAuthenticationConfiguration
                                      {
                                          RedirectUrl = "~/facebook/login",
                                          UserMapper = container.Resolve<IUserMapper>()
                                      };
            FormsAuthentication.Enable(pipelines, formsAuthConfig);
        }

        protected override void ConfigureApplicationContainer(TinyIoC.TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            container.Register<Repositories.IAppUserMapper, Repositories.InMemoryAppUserMapper>().AsSingleton();
            container.Register<IUserMapper>(container.Resolve<Repositories.IAppUserMapper>());
        }

        protected override void ConfigureRequestContainer(TinyIoC.TinyIoCContainer container, NancyContext context)
        {
            base.ConfigureRequestContainer(container, context);

            RegisterFacebookClientPerRequest(container, context);
            RegisterIFacebookApplicationPerRequest(container, context);
            RegisterFacebookOAuthClientPerRequest(container, context);
        }

        private void RegisterFacebookClientPerRequest(TinyIoC.TinyIoCContainer container, NancyContext context)
        {
            var facebookClient = new Facebook.FacebookClient();

            if (context.Request != null && context.Request.Url != null)
                facebookClient.IsSecureConnection = context.Request.Url.Scheme == "https";

            container.Register(facebookClient);
        }

        protected override void RequestStartup(TinyIoC.TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines, NancyContext context)
        {
            base.RequestStartup(container, pipelines, context);

            pipelines.BeforeRequest.AddItemToStartOfPipeline(
                ctx =>
                {
                    var facebookClient = container.Resolve<Facebook.FacebookClient>();
                    var fbUser = context.CurrentUser as IFacebookUser;
                    if (fbUser != null)
                        facebookClient.AccessToken = fbUser.FacebookAccessToken;

                    #region SignedRequest

                    if (context.Request != null)
                    {
                        dynamic signedRequest;
                        var fbApp = container.Resolve<Facebook.IFacebookApplication>();
                        if (context.Request.TryParseFacebookSignedRequest(fbApp.AppId, fbApp.AppSecret, out signedRequest))
                        {
                            if (signedRequest.ContainsKey("oauth_token"))
                                facebookClient.AccessToken = signedRequest.oauth_token;
                        }
                    }

                    #endregion

                    return null;
                });
        }

        private void RegisterIFacebookApplicationPerRequest(TinyIoC.TinyIoCContainer container, NancyContext context)
        {
            Facebook.IFacebookApplication facebookApplication = null;
            if (context != null && context.Request != null && context.Request.Url != null)
            {
                var url = context.Request.Url;
                if (url.HostName == "localhost")
                {
                    facebookApplication = new Facebook.DefaultFacebookApplication
                                              {
                                                  AppId = ",
                                                  AppSecret = ",
                                                  SiteUrl = "http://localhost:45254/",
                                                  CanvasUrl = "http://localhost:45254/canvas/",
                                                  SecureCanvasUrl = "https://localhost:44302/canvas/",
                                                  CanvasPage = "http://apps.facebook.com/appname/
                                              };
                }
                else
                {
                    //facebookApplication = new Facebook.DefaultFacebookApplication { AppId = "", AppSecret = "" };
                }
            }
            container.Register(facebookApplication);
        }

        private void RegisterFacebookOAuthClientPerRequest(TinyIoC.TinyIoCContainer container, NancyContext context)
        {
            var facebookOAuthClient =
                new Facebook.FacebookOAuthClient(container.Resolve<Facebook.IFacebookApplication>())
                    {
                        RedirectUri = new Uri("http://localhost:45254" + context.ToFullPath("~/facebook/login/callback"))
                    };

            container.Register(facebookOAuthClient);
        }
    }
}