using System;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using CK.Core;
using CK.DB.Actor;
using CK.SqlServer;
using NUnit.Framework;
using System.Linq;
using CK.DB.Auth;
using CK.DB.Auth.AuthScope;
using FluentAssertions;
using static CK.Testing.DBSetupTestHelper;

namespace CK.DB.User.UserGitHub.AuthScope.Tests
{
    [TestFixture]
    public class UserGitHubAuthScopeTests
    {

        [Test]
        public async Task non_user_google_ScopeSet_is_null_Async()
        {
            var user = TestHelper.StObjMap.StObjs.Obtain<UserTable>();
            var p = TestHelper.StObjMap.StObjs.Obtain<Package>();
            Throw.DebugAssert( user != null && p != null );
            using( var ctx = new SqlStandardCallContext() )
            {
                var id = await user.CreateUserAsync( ctx, 1, Guid.NewGuid().ToString() );
                (await p.ReadScopeSetAsync( ctx, id )).Should().BeNull();
            }
        }

        [Test]
        public async Task setting_default_scopes_impact_new_users_Async()
        {
            var user = TestHelper.StObjMap.StObjs.Obtain<UserTable>();
            var p = TestHelper.StObjMap.StObjs.Obtain<Package>();
            var factory = TestHelper.StObjMap.StObjs.Obtain<IPocoFactory<IUserGitHubInfo>>();
            Throw.DebugAssert( user != null && p != null && factory != null );
            using( var ctx = new SqlStandardCallContext() )
            {
                AuthScopeSet original = await p.ReadDefaultScopeSetAsync( ctx );
                original.Contains( "nimp" ).Should().BeFalse();
                original.Contains( "thing" ).Should().BeFalse();
                original.Contains( "other" ).Should().BeFalse();

                {
                    int id = await user.CreateUserAsync( ctx, 1, Guid.NewGuid().ToString() );
                    IUserGitHubInfo userInfo = factory.Create();
                    userInfo.GitHubAccountId = Guid.NewGuid().ToString();
                    await p.UserGitHubTable.CreateOrUpdateGitHubUserAsync( ctx, 1, id, userInfo );
                    var info = await p.UserGitHubTable.FindKnownUserInfoAsync( ctx, userInfo.GitHubAccountId );
                    Throw.DebugAssert( info != null );
                    AuthScopeSet userSet = await p.ReadScopeSetAsync( ctx, info.UserId );
                    userSet.ToString().Should().Be( original.ToString() );
                }
                AuthScopeSet replaced = original.Clone();
                replaced.Add( new AuthScopeItem( "nimp" ) );
                replaced.Add( new AuthScopeItem( "thing", ScopeWARStatus.Rejected ) );
                replaced.Add( new AuthScopeItem( "other", ScopeWARStatus.Accepted ) );
                await p.AuthScopeSetTable.SetScopesAsync( ctx, 1, replaced );
                var readback = await p.ReadDefaultScopeSetAsync( ctx );
                readback.ToString().Should().Be( replaced.ToString() );
                // Default scopes have non W status!
                // This must not impact new users: their satus must always be be W.
                readback.ToString().Should().Contain( "[R]thing" )
                                            .And.Contain( "[A]other" );

                {
                    int id = await user.CreateUserAsync( ctx, 1, Guid.NewGuid().ToString() );
                    IUserGitHubInfo? userInfo = p.UserGitHubTable.CreateUserInfo<IUserGitHubInfo>();
                    userInfo.GitHubAccountId = Guid.NewGuid().ToString();
                    await p.UserGitHubTable.CreateOrUpdateGitHubUserAsync( ctx, 1, id, userInfo, UCLMode.CreateOnly | UCLMode.UpdateOnly );
                    userInfo = (IUserGitHubInfo?)(await p.UserGitHubTable.FindKnownUserInfoAsync( ctx, userInfo.GitHubAccountId ))?.Info;
                    Throw.DebugAssert( userInfo != null );
                    AuthScopeSet userSet = await p.ReadScopeSetAsync( ctx, id );
                    userSet.ToString().Should().Contain( "[W]thing" )
                                               .And.Contain( "[W]other" )
                                               .And.Contain( "[W]nimp" );
                }
                await p.AuthScopeSetTable.SetScopesAsync( ctx, 1, original );
            }
        }

    }

}

