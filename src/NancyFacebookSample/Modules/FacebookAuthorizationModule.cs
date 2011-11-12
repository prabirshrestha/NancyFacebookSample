
namespace NancyFacebookSample.Modules
{
    using System;
    using System.Dynamic;
    using System.Text;
    using Facebook;
    using Nancy;
    using NancyFacebookSample.Models;
    using NancyFacebookSample.Repositories;
    using Nancy.Authentication.Forms;

    public class FacebookAuthorizationModule : NancyModule
    {
        private const string ExtendedPermissions = "user_about_me,publish_stream,offline_access";

        private readonly FacebookClient _fb;
        private readonly IAppUserMapper _userMapper;

        public FacebookAuthorizationModule(FacebookClient fb, FacebookOAuthClient fbOAuthClient, IAppUserMapper userMapper)
            : base("/facebook")
        {
            _fb = fb;
            _userMapper = userMapper;

            Get["/login"] = _ =>
                                {
                                    string returnUrl = Request.Query.returnUrl;

                                    dynamic parameters = new ExpandoObject();
                                    parameters.scope = ExtendedPermissions;
                                    parameters.state = Base64UrlEncode(Encoding.UTF8.GetBytes(
                                        JsonSerializer.Current.SerializeObject(new { return_url = returnUrl })));

                                    string loginUri = fbOAuthClient.GetLoginUrl(parameters).AbsoluteUri;
                                    return Response.AsRedirect(loginUri);
                                };

            Get["/login/callback"] = _ =>
                                         {
                                             FacebookOAuthResult oAuthResult;
                                             var requestUrl = Request.Url.Scheme + "://" + Request.Url.HostName + Request.Url.BasePath + Request.Url.Path + Request.Url.Query;
                                             if (fbOAuthClient.TryParseResult(requestUrl, out oAuthResult))
                                             {
                                                 if (oAuthResult.IsSuccess)
                                                 {
                                                     if (!string.IsNullOrWhiteSpace(oAuthResult.Code))
                                                     {
                                                         string returnUrl = null;
                                                         try
                                                         {
                                                             if (!string.IsNullOrWhiteSpace(oAuthResult.State))
                                                             {
                                                                 dynamic state = JsonSerializer.Current.DeserializeObject(Encoding.UTF8.GetString(Base64UrlDecode(oAuthResult.State)));
                                                                 if (state.ContainsKey("return_url") && !string.IsNullOrWhiteSpace(state.return_url))
                                                                     returnUrl = state.return_url;
                                                             }
                                                         }
                                                         catch (Exception ex)
                                                         {
                                                             // catch exception incase user puts custom state
                                                             // which contains invalid json or invalid base64 url encoding
                                                             return Response.AsRedirect("~/");
                                                         }

                                                         try
                                                         {
                                                             dynamic result = fbOAuthClient.ExchangeCodeForAccessToken(oAuthResult.Code);

                                                             DateTime expiresOn;
                                                             User user = ProcessSuccessfulFacebookCallback(result, out expiresOn);
                                                             if (user == null)
                                                                 return Response.AsRedirect("~/");

                                                             // todo: prevent open redirection attacks. make sure the returnUrl is trusted before redirecting to it

                                                             return this.LoginAndRedirect(user.Identifier, expiresOn, returnUrl);
                                                         }
                                                         catch (Exception ex)
                                                         {
                                                             // catch incase the user entered dummy code or the code expires
                                                             // or no internet access or any other errors
                                                         }
                                                     }
                                                     return Response.AsRedirect("~/");
                                                 }
                                                 return View["FacebookLoginCallbackError", oAuthResult];
                                             }
                                             return Response.AsRedirect("~/");
                                         };
        }

        public virtual User ProcessSuccessfulFacebookCallback(dynamic result, out DateTime expiresOn)
        {
            var user = new User { FacebookAccessToken = result.access_token };

            // incase the expires on is not present, it means we have offline_access permission
            // for this example, we don't save the expires
            expiresOn = result.ContainsKey("expires") ? DateTime.UtcNow.AddSeconds(Convert.ToDouble(result["expires"])) : DateTime.MaxValue;

            try
            {
                dynamic me = _fb.Get("me?fields=id,name&access_token=" + user.FacebookAccessToken);
                user.FacebookId = Convert.ToInt64(me.id);
                user.FacebookName = me.name;
                user.UserName = me.name;

                _userMapper.AddOrUpdate(user);
                return user;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        #region Base64 Url Decoding/Encoding

        /// <summary>
        /// Base64 Url decode.
        /// </summary>
        /// <param name="base64UrlSafeString">
        /// The base 64 url safe string.
        /// </param>
        /// <returns>
        /// The base 64 url decoded string.
        /// </returns>
        private static byte[] Base64UrlDecode(string base64UrlSafeString)
        {
            if (string.IsNullOrEmpty(base64UrlSafeString))
                throw new ArgumentNullException("base64UrlSafeString");

            base64UrlSafeString =
                base64UrlSafeString.PadRight(base64UrlSafeString.Length + (4 - base64UrlSafeString.Length % 4) % 4, '=');
            base64UrlSafeString = base64UrlSafeString.Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(base64UrlSafeString);
        }

        /// <summary>
        /// Base64 url encode.
        /// </summary>
        /// <param name="input">
        /// The input.
        /// </param>
        /// <returns>
        /// Base64 url encoded string.
        /// </returns>
        private static string Base64UrlEncode(byte[] input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            return Convert.ToBase64String(input).Replace("=", String.Empty).Replace('+', '-').Replace('/', '_');
        }

        #endregion
    }
}