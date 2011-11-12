
namespace NancyFacebookSample.Modules
{
    using Nancy;
    using Nancy.Security;

    public class FacebookModule : NancyModule
    {
        public FacebookModule()
            : base("/facebook")
        {
            this.RequiresAuthentication();

            Get["/"] = _ => "fb";
        }
    }
}