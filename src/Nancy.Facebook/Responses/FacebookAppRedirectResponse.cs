
namespace Nancy.Facebook.Responses
{
    using System.IO;
    using global::Facebook.Web;

    public class FacebookAppRedirectResponse : Response
    {
        public FacebookAppRedirectResponse(string url)
        {
            this.ContentType = "text/html";
            this.Contents = GetStringContents(FacebookWebHelper.FacebookAppRedirectScript(url));
        }
    }
}
