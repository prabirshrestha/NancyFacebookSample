
namespace NancyFacebookCanvasSample
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using Facebook;
    using Nancy;
    using Nancy.Facebook;

    public class CanvasModule : NancyModule
    {
        public CanvasModule(IFacebookApplication fbApp, FacebookClient fb)
        {
            this.HandleFacebookOAuthDialogError(fbApp.AppId, scope: "user_about_me,read_stream");
            this.DropFacebookQueryStrings();

            Post["/"] = _ =>
                            {
                                var canvasPageUrl = Context.FacebookCanvasPageUrl(fbApp.CanvasPage);
                                return View["index", canvasPageUrl];
                            };

            Post["/feed"] = _ =>
            {
                var perms = Context.FacebooPermissions();

                if (!perms.Intersect(new[] { "user_about_me", "read_stream" }).Any())
                    return Response.AsFacebookLogin(fbApp.AppId, scope: "user_about_me,read_stream");

                dynamic model = new JsonObject();
                model.canvasPageUrl = Context.FacebookCanvasPageUrl(fbApp.CanvasPage);
                model.facebookLoginUrl = Context.FacebookLoginUrl(fbApp.AppId, scope: "user_about_me,read_stream");

                if (perms.Contains("user_about_me"))
                {
                    dynamic result = fb.Get("me?fields=picture,name");
                    model.name = result.name;
                    model.picture = result.picture;
                }

                if (perms.Contains("read_stream"))
                {
                    dynamic result = fb.Get("me/feed");
                    model.feeds = result;
                }

                return View["Feed", model];
            };

            Post["/feed/batch"] = _ =>
                                {
                                    var perms = Context.FacebooPermissions();

                                    if (!perms.Intersect(new[] { "user_about_me", "read_stream" }).Any())
                                        return Response.AsFacebookLogin(fbApp.AppId, scope: "user_about_me,read_stream");

                                    dynamic model = new JsonObject();
                                    model.canvasPageUrl = Context.FacebookCanvasPageUrl(fbApp.CanvasPage);
                                    model.facebookLoginUrl = Context.FacebookLoginUrl(fbApp.AppId, scope: "user_about_me,read_stream");

                                    var bp = new Dictionary<string, Tuple<int, FacebookBatchParameter>>();
                                    int bpi = 0;
                                    if (perms.Contains("user_about_me"))
                                        bp.Add("me", new Tuple<int, FacebookBatchParameter>(bpi++, new FacebookBatchParameter("me?fields=picture,name")));

                                    if (perms.Contains("read_stream"))
                                        bp.Add("feeds", new Tuple<int, FacebookBatchParameter>(bpi++, new FacebookBatchParameter("me/feed")));

                                    dynamic result = fb.Batch(bp.Values.Select(t => t.Item2).ToArray());

                                    if (bp.ContainsKey("me"))
                                    {
                                        dynamic me = result[bp["me"].Item1];
                                        if (!(me is Exception))
                                        {
                                            model.name = me.name;
                                            model.picture = me.picture;
                                        }
                                    }

                                    if (bp.ContainsKey("feeds"))
                                    {
                                        dynamic feeds = result[bp["feeds"].Item1];
                                        if (!(feeds is Exception))
                                            model.feeds = feeds;
                                    }

                                    return View["Feed", model];
                                };
        }
    }
}