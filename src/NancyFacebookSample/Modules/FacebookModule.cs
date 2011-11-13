
namespace NancyFacebookSample.Modules
{
    using System;
    using Facebook;
    using Nancy;
    using Nancy.Security;
    using Nancy.Extensions;

    public class FacebookModule : NancyModule
    {
        public FacebookModule(FacebookClient fb)
            : base("/facebook")
        {
            this.RequiresAuthentication();

            Get["/"] = _ =>
                           {
                               try
                               {
                                   dynamic me = fb.Get("me");
                                   return View["facebook/index", me];
                               }
                               catch (FacebookOAuthException)
                               {
                                   // fb access token is no longer valid.
                                   return Response.AsRedirect("~/facebook/login?returnUrl=" + Context.ToFullPath(ModulePath + "/"));
                               }
                           };
        }
    }
}