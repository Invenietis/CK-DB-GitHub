
using System;

namespace CK.DB.User.UserGitHub.RefreshToken
{
    /// <summary>
    /// Extends <see cref="UserGitHub.IUserGitHubInfo"/> with email related information.
    /// </summary>
    public interface IUserGitHubInfo : UserGitHub.IUserGitHubInfo
    {
        /// <summary>
        /// Gets or sets the last time the refresh token has been updated.
        /// This is meaningful only when reading from the database and is ignored when writing.
        /// </summary>
        DateTime LastRefreshTokenTime { get; set; }

        /// <summary>
        /// Gets or sets the refresh token.
        /// </summary>
        string RefreshToken { get; set; }
    }
}
