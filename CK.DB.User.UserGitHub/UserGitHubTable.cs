using CK.SqlServer;
using CK.Core;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using CK.DB.Auth;
using System.Diagnostics.CodeAnalysis;

namespace CK.DB.User.UserGitHub;

/// <summary>
/// GitHub authentication provider.
/// </summary>
[SqlTable( "tUserGitHub", Package = typeof( Package ) )]
[Versions( "2.0.1" )]
[SqlObjectItem( "transform:sUserDestroy" )]
public abstract partial class UserGitHubTable : SqlTable, IGenericAuthenticationProvider<IUserGitHubInfo>
{
    [AllowNull]
    IPocoFactory<IUserGitHubInfo> _infoFactory;

    /// <summary>
    /// Gets "GitHub" that is the name of the GitHub provider.
    /// </summary>
    public string ProviderName => "GitHub";

    public bool CanCreatePayload => true;

    object IGenericAuthenticationProvider.CreatePayload() => _infoFactory.Create();

    void StObjConstruct( IPocoFactory<IUserGitHubInfo> infoFactory )
    {
        _infoFactory = infoFactory;
    }

    IUserGitHubInfo IGenericAuthenticationProvider<IUserGitHubInfo>.CreatePayload() => _infoFactory.Create();

    /// <summary>
    /// Creates a <see cref="IUserGitHubInfo"/> poco.
    /// </summary>
    /// <returns>A new instance.</returns>
    public T CreateUserInfo<T>() where T : IUserGitHubInfo => (T)_infoFactory.Create();

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
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The result.</returns>
    public async Task<UCLResult> CreateOrUpdateGitHubUserAsync( ISqlCallContext ctx, int actorId, int userId, IUserGitHubInfo info, UCLMode mode = UCLMode.CreateOrUpdate, CancellationToken cancellationToken = default )
    {
        var r = await GitHubUserUCLAsync( ctx, actorId, userId, info, mode, cancellationToken ).ConfigureAwait( false );
        return r;
    }

    /// <summary>
    /// Challenges <see cref="IUserGitHubInfo"/> data to identify a user.
    /// Note that a successful challenge may have side effects such as updating claims, access tokens or other data
    /// related to the user and this provider.
    /// </summary>
    /// <param name="ctx">The call context to use.</param>
    /// <param name="info">The payload to challenge.</param>
    /// <param name="actualLogin">Set it to false to avoid login side-effect (such as updating the LastLoginTime) on success.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The <see cref="LoginResult"/>.</returns>
    public async Task<LoginResult> LoginUserAsync( ISqlCallContext ctx, IUserGitHubInfo info, bool actualLogin = true, CancellationToken cancellationToken = default )
    {
        var mode = actualLogin
                    ? UCLMode.UpdateOnly | UCLMode.WithActualLogin
                    : UCLMode.UpdateOnly | UCLMode.WithCheckLogin;
        var r = await GitHubUserUCLAsync( ctx, 1, 0, info, mode, cancellationToken ).ConfigureAwait( false );
        return r.LoginResult;
    }

    /// <summary>
    /// Destroys a GitHubUser for a user.
    /// </summary>
    /// <param name="ctx">The call context to use.</param>
    /// <param name="actorId">The acting actor identifier.</param>
    /// <param name="userId">The user identifier for which GitHub account information must be destroyed.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The awaitable.</returns>
    [SqlProcedure( "sUserGitHubDestroy" )]
    public abstract Task DestroyGitHubUserAsync( ISqlCallContext ctx, int actorId, int userId, CancellationToken cancellationToken = default );

    /// <summary>
    /// Raw call to manage GitHubUser. Since this should not be used directly, it is protected.
    /// Actual implementation of the centralized update, create or login procedure.
    /// </summary>
    /// <param name="ctx">The call context to use.</param>
    /// <param name="actorId">The acting actor identifier.</param>
    /// <param name="userId">The user identifier for which a GitHub account must be created or updated.</param>
    /// <param name="info">User information to create or update.</param>
    /// <param name="mode">Configures Create, Update only or WithCheck/ActualLogin behavior.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The result.</returns>
    [SqlProcedure( "sUserGitHubUCL" )]
    protected abstract Task<UCLResult> GitHubUserUCLAsync( ISqlCallContext ctx,
                                                           int actorId,
                                                           int userId,
                                                           [ParameterSource] IUserGitHubInfo info,
                                                           UCLMode mode,
                                                           CancellationToken cancellationToken );

    /// <summary>
    /// Finds a user by its GitHub account identifier.
    /// Returns null if no such user exists.
    /// </summary>
    /// <param name="ctx">The call context to use.</param>
    /// <param name="googleAccountId">The GitHub account identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A <see cref="IdentifiedUserInfo{T}"/> object or null if not found.</returns>
    public async Task<IdentifiedUserInfo<IUserGitHubInfo>?> FindKnownUserInfoAsync( ISqlCallContext ctx, string googleAccountId, CancellationToken cancellationToken = default )
    {
        using( var c = CreateReaderCommand( googleAccountId ) )
        {
            return await ctx[Database].ExecuteSingleRowAsync( c, r => r == null
                                                                ? null
                                                                : DoCreateUserUnfo( googleAccountId, r ), cancellationToken )
                                      .ConfigureAwait( false );
        }
    }

    /// <summary>
    /// Creates a the reader command parametrized with the GitHub account identifier.
    /// Single-row returned columns are defined by <see cref="AppendUserInfoColumns(StringBuilder)"/>.
    /// </summary>
    /// <param name="googleAccountId">GitHub account identifier to look for.</param>
    /// <returns>A ready to use reader command.</returns>
    SqlCommand CreateReaderCommand( string googleAccountId )
    {
        StringBuilder b = new StringBuilder( "select " );
        AppendUserInfoColumns( b ).Append( " from CK.tUserGitHub where GitHubAccountId=@A" );
        var c = new SqlCommand( b.ToString() );
        c.Parameters.Add( new SqlParameter( "@A", googleAccountId ) );
        return c;
    }

    IdentifiedUserInfo<IUserGitHubInfo> DoCreateUserUnfo( string googleAccountId, SqlDataRow r )
    {
        var info = _infoFactory.Create();
        info.GitHubAccountId = googleAccountId;
        FillUserGitHubInfo( info, r, 1 );
        return new IdentifiedUserInfo<IUserGitHubInfo>( r.GetInt32( 0 ), info );
    }

    /// <summary>
    /// Adds the columns name to read.
    /// </summary>
    /// <param name="b">The string builder.</param>
    /// <returns>The string builder.</returns>
    protected virtual StringBuilder AppendUserInfoColumns( StringBuilder b )
    {
        var props = _infoFactory.PocoClassType.GetProperties().Where( p => p.Name != nameof( IUserGitHubInfo.GitHubAccountId ) );
        return props.Any() ? b.Append( "UserId, " ).AppendStrings( props.Select( p => p.Name ) ) : b.Append( "UserId " );
    }

    /// <summary>
    /// Fill UserInfo properties from reader.
    /// </summary>
    /// <param name="info">The info to fill.</param>
    /// <param name="r">The record.</param>
    /// <param name="idx">The index of the first column.</param>
    /// <returns>The updated index.</returns>
    protected virtual int FillUserGitHubInfo( IUserGitHubInfo info, SqlDataRow r, int idx )
    {
        var props = _infoFactory.PocoClassType.GetProperties().Where( p => p.Name != nameof( IUserGitHubInfo.GitHubAccountId ) );
        foreach( var p in props )
        {
            p.SetValue( info, r.GetValue( idx++ ) );
        }
        return idx;
    }

    #region IGenericAuthenticationProvider explicit implementation.

    UCLResult IGenericAuthenticationProvider.CreateOrUpdateUser( ISqlCallContext ctx, int actorId, int userId, object payload, UCLMode mode )
    {
        IUserGitHubInfo info = _infoFactory.ExtractPayload( payload );
        return CreateOrUpdateGitHubUser( ctx, actorId, userId, info, mode );
    }

    LoginResult IGenericAuthenticationProvider.LoginUser( ISqlCallContext ctx, object payload, bool actualLogin )
    {
        IUserGitHubInfo info = _infoFactory.ExtractPayload( payload );
        return LoginUser( ctx, info, actualLogin );
    }

    Task<UCLResult> IGenericAuthenticationProvider.CreateOrUpdateUserAsync( ISqlCallContext ctx, int actorId, int userId, object payload, UCLMode mode, CancellationToken cancellationToken )
    {
        IUserGitHubInfo info = _infoFactory.ExtractPayload( payload );
        return CreateOrUpdateGitHubUserAsync( ctx, actorId, userId, info, mode, cancellationToken );
    }

    Task<LoginResult> IGenericAuthenticationProvider.LoginUserAsync( ISqlCallContext ctx, object payload, bool actualLogin, CancellationToken cancellationToken )
    {
        IUserGitHubInfo info = _infoFactory.ExtractPayload( payload );
        return LoginUserAsync( ctx, info, actualLogin, cancellationToken );
    }

    void IGenericAuthenticationProvider.DestroyUser( ISqlCallContext ctx, int actorId, int userId, string? schemeSuffix )
    {
        DestroyGitHubUser( ctx, actorId, userId );
    }

    Task IGenericAuthenticationProvider.DestroyUserAsync( ISqlCallContext ctx, int actorId, int userId, string? schemeSuffix, CancellationToken cancellationToken )
    {
        return DestroyGitHubUserAsync( ctx, actorId, userId, cancellationToken );
    }

    #endregion
}
