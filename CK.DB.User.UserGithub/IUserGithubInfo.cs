using CK.Core;
using System;

namespace CK.DB.User.UserGithub
{
    /// <summary>
    /// Holds information stored for a Github user.
    /// </summary>
    public interface IUserGithubInfo : IPoco
    {
        /// <summary>
        /// Gets or sets the Github account identifier.
        /// </summary>
        string GithubAccountId { get; set; }
    }

}
