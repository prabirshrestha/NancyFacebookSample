
namespace NancyFacebookSample.Models
{
    using System;
    using System.Collections.Generic;
    using Nancy.Security;

    public class User : IUserIdentity, IFacebookUser
    {
        public Guid Identifier { get; set; }

        public string UserName { get; set; }
        public IEnumerable<string> Claims { get; set; }

        public long FacebookId { get; set; }
        public string FacebookAccessToken { get; set; }
        public string FacebookName { get; set; }
    }
}