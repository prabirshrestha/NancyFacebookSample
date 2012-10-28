using System;
using System.Collections.Generic;
using System.Linq;
using Nancy.Security;

namespace NancyFacebookWebsiteSample
{
    [Serializable]
    public class User : IUserIdentity
    {
        public User()
        {
            Claims = Enumerable.Empty<string>();
        }

        public string UserName { get; set; }
        public IEnumerable<string> Claims { get; set; }

        public string FacebookUserId { get; set; }
        public string FacebookAccessToken { get; set; }
        public DateTime FacebookAccessTokenExpiration { get; set; }
    }
}