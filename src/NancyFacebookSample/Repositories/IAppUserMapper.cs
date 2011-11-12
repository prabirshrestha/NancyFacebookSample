namespace NancyFacebookSample.Repositories
{
    using System;
    using Models;
    using Nancy.Authentication.Forms;

    public interface IAppUserMapper : IUserMapper
    {
        void AddOrUpdate(User user);
        void Remove(Guid identifier);
    }
}