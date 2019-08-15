using CK.DB.Auth;
using CK.SqlServer;
using CK.Core;

namespace CK.DB.User.UserGitHub
{
    public abstract partial class UserGitHubTable
    {
        /// <summary>
        /// Creates or updates a user entry for this provider. 
        /// This is the "binding account" feature since it binds an external identity to 
        /// an already existing user that may already be registered into other authencation providers.
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="actorId">The acting actor identifier.</param>
        /// <param name="userId">The user identifier that must be registered.</param>
        /// <param name="info">Provider specific data: the <see cref="IUserGitHubInfo"/> poco.</param>
        /// <param name="mode">Optionnaly configures Create, Update only or WithLogin behavior.</param>
        /// <returns>The result.</returns>
        public UCLResult CreateOrUpdateGitHubUser( ISqlCallContext ctx, int actorId, int userId, IUserGitHubInfo info, UCLMode mode = UCLMode.CreateOrUpdate )
        {
            return UserGitHubUCL( ctx, actorId, userId, info, mode );
        }

        /// <summary>
        /// Challenges <see cref="IUserGitHubInfo"/> data to identify a user.
        /// Note that a successful challenge may have side effects such as updating claims, access tokens or other data
        /// related to the user and this provider.
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="info">The payload to challenge.</param>
        /// <param name="actualLogin">Set it to false to avoid login side-effect (such as updating the LastLoginTime) on success.</param>
        /// <returns>The login result.</returns>
        public LoginResult LoginUser( ISqlCallContext ctx, IUserGitHubInfo info, bool actualLogin = true )
        {
            var mode = actualLogin
                        ? UCLMode.UpdateOnly | UCLMode.WithActualLogin
                        : UCLMode.UpdateOnly | UCLMode.WithCheckLogin;
            var r = UserGitHubUCL( ctx, 1, 0, info, mode );
            return r.LoginResult;
        }

        /// <summary>
        /// Destroys a GitHubUser for a user.
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="actorId">The acting actor identifier.</param>
        /// <param name="userId">The user identifier for which GitHub account information must be destroyed.</param>
        /// <returns>The awaitable.</returns>
        [SqlProcedure( "sUserGitHubDestroy" )]
        public abstract void DestroyGitHubUser( ISqlCallContext ctx, int actorId, int userId );

        /// <summary>
        /// Finds a user by its GitHub account identifier.
        /// Returns null if no such user exists.
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="googleAccountId">The google account identifier.</param>
        /// <returns>A <see cref="IdentifiedUserInfo{T}"/> or null if not found.</returns>
        public IdentifiedUserInfo<IUserGitHubInfo> FindKnownUserInfo( ISqlCallContext ctx, string googleAccountId )
        {
            using( var c = CreateReaderCommand( googleAccountId ) )
            {
                return ctx[Database].ExecuteSingleRow( c, r => r == null
                                                            ? null
                                                            : DoCreateUserUnfo( googleAccountId, r ) );
            }
        }

        /// <summary>
        /// Raw call to manage GitHubUser. Since this should not be used directly, it is protected.
        /// Actual implementation of the centralized update, create or login procedure.
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="actorId">The acting actor identifier.</param>
        /// <param name="userId">The user identifier for which a GitHub account must be created or updated.</param>
        /// <param name="info">User information to create or update.</param>
        /// <param name="mode">Configures Create, Update only or WithCheck/ActualLogin behavior.</param>
        /// <returns>The result.</returns>
        [SqlProcedure( "sUserGitHubUCL" )]
        protected abstract UCLResult UserGitHubUCL(
            ISqlCallContext ctx,
            int actorId,
            int userId,
            [ParameterSource]IUserGitHubInfo info,
            UCLMode mode );


    }
}
