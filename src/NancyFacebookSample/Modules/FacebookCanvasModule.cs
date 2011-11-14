
namespace NancyFacebookSample.Modules
{
    using Facebook;
    using Nancy;
    using Nancy.Facebook;

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