using CK.Core;
using System;

namespace CK.DB.User.UserGithub.AuthScope
{
    /// <summary>
    /// Extends <see cref="UserGithub.IUserGithubInfo"/> with ScopeSet identifier.
    /// </summary>
    public interface IUserGithubInfo : UserGithub.IUserGithubInfo
    {
        /// <summary>
        /// Gets the scope set identifier.
        /// Note that the ScopeSetId is intrinsic: a new ScopeSetId is acquired 
        /// and set only when a new UserGithub is created (by copy from 
        /// the default one - the ScopeSet of the UserGithub 0).
        /// </summary>
        int ScopeSetId { get; }
    }
}
