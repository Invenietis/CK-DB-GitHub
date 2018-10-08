using CK.Core;
using System;

namespace CK.DB.User.UserGitHub
{
    /// <summary>
    /// Holds information stored for a GitHub user.
    /// </summary>
    public interface IUserGitHubInfo : IPoco
    {
        /// <summary>
        /// Gets or sets the GitHub account identifier.
        /// </summary>
        string GitHubAccountId { get; set; }
    }

}
