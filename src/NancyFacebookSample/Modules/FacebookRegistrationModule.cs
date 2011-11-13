
namespace NancyFacebookSample.Modules
{
    using System;
    using System.Dynamic;
    using Facebook;
    using Nancy;
    using Nancy.Authentication.Forms;
    using Nancy.Extensions;
    using Nancy.Helpers;
    using NancyFacebookSample.Models;
    using Repositories;

    public class FacebookRegistrationModule : NancyModule
    {
        public FacebookRegistrationModule(IFacebookApplication facebookApplication, IAppUserMapper userMapper)
        {
            Get["/register"] =
                _ =>
                {
                    // note: for more options to https://developers.facebook.com/docs/plugins/registration/

                    var fields = new object[]
                                         {
                                             new {name = "name"},
                                             new {name = "email"},
                                             new {name = "location"},
                                             new {name = "gender"},
                                             new {name = "birthday"},
                                             new {name = "password", view = "not_prefilled"},
                                             new
                                                 {
                                                     name = "like",
                                                     description = "Do you like this plugin?",
                                                     type = "checkbox",
                                                     @default = "checked"
                                                 },
                                             new
                                                 {
                                                     name = "phone",
                                                     description = "Phone Number",
                                                     type = "text"
                                                 },
                                             new {name = "captcha"}
                                         };

                    dynamic model = new ExpandoObject();
                    model.FacebookRegistrationUrl = string.Format(
                        "http://www.facebook.com/plugins/registration.php?client_id={0}&redirect_uri={1}&fields={2}&fb_only=true",
                        facebookApplication.AppId,
                        HttpUtility.UrlEncode("http://localhost:45254" + Context.ToFullPath("~/register/facebookcallback")),
                        HttpUtility.UrlEncode(JsonSerializer.Current.SerializeObject(fields)));

                    return View["register", model];
                };

            Post["/register/facebookcallback"] =
                _ =>
                {
                    dynamic signedRequest = Request.ParseFacebookSignedRequest(facebookApplication.AppId, facebookApplication.AppSecret);
                    DateTime expiresOn = signedRequest.expires == 0 ? DateTime.MaxValue : DateTime.UtcNow.AddSeconds(Convert.ToDouble(signedRequest.expires));
                    DateTime issuedAt = DateTimeConvertor.FromUnixTime(signedRequest.issued_at);
                    var accessToken = signedRequest.oauth_token;

                    var name = signedRequest.registration.name;
                    var userId = Convert.ToInt64(signedRequest.user_id);

                    var user = new User
                                   {
                                       FacebookAccessToken = accessToken,
                                       FacebookId = userId,
                                       FacebookName = name,
                                       UserName = name
                                   };

                    userMapper.AddOrUpdate(user);

                    return this.LoginAndRedirect(user.Identifier, expiresOn, "~/facebook");
                };
        }
    }
}