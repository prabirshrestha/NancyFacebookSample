
namespace NancyFacebookSample.Modules
{
    using Nancy;

    public class RootModule : NancyModule
    {
        public RootModule()
        {
            Get["/"] = parameters => View["index"];
        }
    }
}