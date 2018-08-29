using System;
using System.Data.SqlClient;
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

namespace CK.DB.User.UserGithub.AuthScope.Tests
{
    [TestFixture]
    public class UserGithubAuthScopeTests
    {

        [Test]
        public async Task non_user_google_ScopeSet_is_null()
        {
            var user = TestHelper.StObjMap.StObjs.Obtain<UserTable>();
            var p = TestHelper.StObjMap.StObjs.Obtain<Package>();
            using( var ctx = new SqlStandardCallContext() )
            {
                var id = await user.CreateUserAsync( ctx, 1, Guid.NewGuid().ToString() );
                (await p.ReadScopeSetAsync( ctx, id )).Should().BeNull();
            }
        }

        [Test]
        public async Task setting_default_scopes_impact_new_users()
        {
            var user = TestHelper.StObjMap.StObjs.Obtain<UserTable>();
            var p = TestHelper.StObjMap.StObjs.Obtain<Package>();
            var factory = TestHelper.StObjMap.StObjs.Obtain<IPocoFactory<IUserGithubInfo>>();
            using( var ctx = new SqlStandardCallContext() )
            {
                AuthScopeSet original = await p.ReadDefaultScopeSetAsync( ctx );
                original.Contains( "nimp" ).Should().BeFalse();
                original.Contains( "thing" ).Should().BeFalse();
                original.Contains( "other" ).Should().BeFalse();

                {
                    int id = await user.CreateUserAsync( ctx, 1, Guid.NewGuid().ToString() );
                    IUserGithubInfo userInfo = factory.Create();
                    userInfo.GithubAccountId = Guid.NewGuid().ToString();
                    await p.UserGithubTable.CreateOrUpdateGithubUserAsync( ctx, 1, id, userInfo );
                    var info = await p.UserGithubTable.FindKnownUserInfoAsync( ctx, userInfo.GithubAccountId );
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
                    IUserGithubInfo userInfo = p.UserGithubTable.CreateUserInfo<IUserGithubInfo>();
                    userInfo.GithubAccountId = Guid.NewGuid().ToString();
                    await p.UserGithubTable.CreateOrUpdateGithubUserAsync( ctx, 1, id, userInfo, UCLMode.CreateOnly | UCLMode.UpdateOnly );
                    userInfo = (IUserGithubInfo)(await p.UserGithubTable.FindKnownUserInfoAsync( ctx, userInfo.GithubAccountId )).Info;
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

