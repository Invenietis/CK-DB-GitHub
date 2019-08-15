using CK.Core;

namespace CK.DB.User.UserGitHub
{
    /// <summary>
    /// Package that adds GitHub authentication support for users. 
    /// </summary>
    [SqlPackage( Schema = "CK", ResourcePath = "Res" )]
    [Versions("1.0.0")]
    [SqlObjectItem( "transform:vUserAuthProvider" )]
    public class Package : SqlPackage
    {
        void StObjConstruct( Actor.Package actorPackage, Auth.Package authPackage )
        {
        }

        /// <summary>
        /// Gets the user GitHub table.
        /// </summary>
        [InjectObject]
        public UserGitHubTable UserGitHubTable { get; protected set; }

    }
}
