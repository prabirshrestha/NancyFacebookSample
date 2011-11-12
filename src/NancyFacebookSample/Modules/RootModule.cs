
namespace NancyFacebookSample.Modules
{
    using Nancy;
    using Nancy.Authentication.Forms;
    using NancyFacebookSample.Models;
    using Repositories;

    public class RootModule : NancyModule
    {
        public RootModule(IAppUserMapper userMapper)
        {
            Get["/"] = _ => View["index"];

            Get["/logout"] = _ =>
                                 {
                                     var user = Context.CurrentUser as User;
                                     if (user != null)
                                         userMapper.Remove(user.Identifier);

                                     return this.LogoutAndRedirect("~/");
                                 };
        }
    }
}