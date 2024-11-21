using CK.Core;

namespace CK.DB.User.UserGitHub.RefreshToken;

/// <summary>
/// Package that adds RefreshToken support to GitHub authentication.
/// </summary>
[SqlPackage( ResourcePath = "Res" )]
[Versions( "1.0.0" )]
[SqlObjectItem( "transform:CK.sUserGitHubUCL" )]
public abstract class Package : SqlPackage
{
    void StObjConstruct( UserGitHub.UserGitHubTable table )
    {
    }
}
