
namespace NancyFacebookCanvasSample
{
    using Facebook;
    using Nancy;
    using Nancy.Facebook;

    public class CanvasModule : NancyModule
    {
        public CanvasModule(IFacebookApplication fbApp)
        {
            Post["/"] = _ =>
                            {
                                var canvasPageUrl = Context.FacebookCanvasPageUrl(fbApp.CanvasPage);
                                return View["index", canvasPageUrl];
                            };

            Post["/feed"] = _ =>
                                {
                                    return "feed page";
                                };
        }
    }
}