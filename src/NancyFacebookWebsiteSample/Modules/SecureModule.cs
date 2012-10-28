using Nancy;
using NancyFacebookWebsiteSample.Extensions;

namespace NancyFacebookWebsiteSample.Modules
{
    public class SecureModule : NancyModule
    {
        public SecureModule()
        {
            this.RequiresFacebookAuthentication();

            Get["/form"] = _ => View["form"];
        }
    }
}