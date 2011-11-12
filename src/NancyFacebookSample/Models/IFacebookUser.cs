namespace NancyFacebookSample.Models
{
    public interface IFacebookUser
    {
        // append 'Facebook' in order to avoid clahes with property names.
        // for example there might already be a property named 'Id' in the class that inherits from IUserIdentity.

        /// <summary>
        /// Gets the Facebook user id.
        /// </summary>
        long FacebookId { get; set; }

        /// <summary>
        /// Gets the Facebook Access Token.
        /// </summary>
        string FacebookAccessToken { get; set; }

        /// <summary>
        /// Gets the Facebook user name.
        /// </summary>
        /// <remarks>
        /// name is actually not useful for authentication nor is it required, but helpful for debuging purposes.
        /// </remarks>
        string FacebookName { get; set; }
    }
}