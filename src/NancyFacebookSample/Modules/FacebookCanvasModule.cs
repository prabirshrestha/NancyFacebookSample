
namespace NancyFacebookSample.Modules
{
    using Facebook;
    using Nancy;

    public class FacebookCanvasModule : NancyModule
    {
        public FacebookCanvasModule(FacebookClient fb, IFacebookApplication facebookApplication)
            : base("/canvas")
        {
            Post["/"] = parameters =>
                            {
                                return Request.ParseFacebookSignedRequest(facebookApplication.AppId, facebookApplication.AppSecret).ToString();
                            };
        }
    }
}