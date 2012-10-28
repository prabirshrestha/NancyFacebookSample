using Facebook;
using Nancy;
using NancyFacebookWebsiteSample.Extensions;

namespace NancyFacebookWebsiteSample.Modules
{
    public class SecureModule : NancyModule
    {
        public SecureModule(FacebookClient fb)
        {
            this.RequiresFacebookAuthentication();

            // note: fb.AccessToken is set in Bootstapper automatically during RequestStartup

            Get["/form"] =
                _ =>
                {
                    dynamic me = fb.Get("me");
                    ViewBag.name = me.name;

                    return View["form"];
                };
        }
    }
}