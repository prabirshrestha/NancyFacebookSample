
namespace NancyFacebookSample
{
    using Nancy;
    using Nancy.Authentication.Forms;

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
    }
}