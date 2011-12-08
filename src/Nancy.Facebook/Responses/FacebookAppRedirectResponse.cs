
namespace Nancy.Facebook.Responses
{
    using System.IO;
    using global::Facebook.Web;

    public class FacebookAppRedirectResponse : Response
    {
        public FacebookAppRedirectResponse(string url, HttpStatusCode statusCode = HttpStatusCode.TemporaryRedirect)
        {
            this.ContentType = "text/html";
            this.StatusCode = statusCode;
            this.Contents = s =>
            {
                var writer = new StreamWriter(s);
                writer.Write(FacebookWebHelper.FacebookAppRedirectHtml(url));
                writer.Flush();
            };
        }
    }
}
