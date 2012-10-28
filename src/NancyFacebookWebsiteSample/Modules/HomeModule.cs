using Nancy;

namespace NancyFacebookWebsiteSample.Modules
{
    public class HomeModule : NancyModule
    {
        public HomeModule()
        {
            Get["/"] = _ => View["index"];
        }
    }
}