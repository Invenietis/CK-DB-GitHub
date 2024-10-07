using CK.Core;
using System;

namespace CK.DB.User.UserGitHub.AuthScope;

/// <summary>
/// Extends <see cref="UserGitHub.IUserGitHubInfo"/> with ScopeSet identifier.
/// </summary>
public interface IUserGitHubInfo : UserGitHub.IUserGitHubInfo
{
    /// <summary>
    /// Gets or sets the scope set identifier.
    /// Note that the ScopeSetId is intrinsic: a new ScopeSetId is acquired 
    /// and set only when a new UserGitHub is created (by copy from 
    /// the default one - the ScopeSet of the UserGitHub 0).
    /// </summary>
    int ScopeSetId { get; set; }
}
