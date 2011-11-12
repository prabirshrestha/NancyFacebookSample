namespace NancyFacebookSample.Repositories
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using Models;
    using Nancy.Security;

    public class InMemoryAppUserMapper : IAppUserMapper
    {
        /// <remarks>
        /// Key (long): Facebook user id
        /// Value (User): The actual uer object.
        /// </remarks>
        private readonly ConcurrentDictionary<long, User> _users = new ConcurrentDictionary<long, User>();

        public IUserIdentity GetUserFromIdentifier(Guid identifier)
        {
            return _users.Where(u => u.Value.Identifier == identifier).Select(u => u.Value).SingleOrDefault();
        }

        public void AddOrUpdate(User user)
        {
            if (user == null)
                throw new ArgumentNullException("user");
            if (user.Identifier == Guid.Empty)
                user.Identifier = Guid.NewGuid();

            _users.AddOrUpdate(user.FacebookId, user, (key, oldValue) => user);
        }

        public void Remove(Guid identifier)
        {
            var user = (User)GetUserFromIdentifier(identifier);
            if (user != null)
                _users.TryRemove(user.FacebookId, out user);
        }
    }
}