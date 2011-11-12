namespace NancyFacebookSample.Repositories
{
    using Models;
    using Nancy.Authentication.Forms;

    public interface IAppUserMapper : IUserMapper
    {
        void AddOrUpdate(User user);
        void Remove(User user);
    }
}