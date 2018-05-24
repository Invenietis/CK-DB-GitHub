using CK.Core;
using CK.DB.Auth;
using CK.Setup;
using CK.SqlServer;
using CK.SqlServer.Setup;
using CK.Text;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CK.DB.User.UserGithub
{
    /// <summary>
    /// Package that adds Github authentication support for users. 
    /// </summary>
    [SqlPackage( Schema = "CK", ResourcePath = "Res" )]
    [Versions("1.0.0")]
    [SqlObjectItem( "transform:vUserAuthProvider" )]
    public class Package : SqlPackage
    {
        /// <summary>
        /// Github api url (https://www.googleapis.com).
        /// </summary>
        public static readonly string ApiUrl = "https://www.googleapis.com";

        void StObjConstruct( Actor.Package actorPackage, Auth.Package authPackage )
        {
        }

        /// <summary>
        /// Gets the user Github table.
        /// </summary>
        [InjectContract]
        public UserGithubTable UserGithubTable { get; protected set; }

    }
}
