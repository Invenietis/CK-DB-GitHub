using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using CK.Core;
using CK.DB.Actor;
using CK.SqlServer;
using NUnit.Framework;
using System.Linq;
using CK.DB.Auth;
using System.Collections.Generic;
using FluentAssertions;
using static CK.Testing.DBSetupTestHelper;

namespace CK.DB.User.UserGithub.Tests
{
    [TestFixture]
    public class UserGithubTests
    {
        [Test]
        public void create_Github_user_and_check_read_info_object_method()
        {
            var u = TestHelper.StObjMap.Default.Obtain<UserGithubTable>();
            var user = TestHelper.StObjMap.Default.Obtain<UserTable>();
            var infoFactory = TestHelper.StObjMap.Default.Obtain<IPocoFactory<IUserGithubInfo>>();
            using( var ctx = new SqlStandardCallContext() )
            {
                var userName = Guid.NewGuid().ToString();
                int userId = user.CreateUser( ctx, 1, userName );
                var googleAccountId = Guid.NewGuid().ToString( "N" );

                var info = infoFactory.Create();
                info.GithubAccountId = googleAccountId;
                var created = u.CreateOrUpdateGithubUser( ctx, 1, userId, info );
                created.OperationResult.Should().Be( UCResult.Created );
                var info2 = u.FindKnownUserInfo( ctx, googleAccountId );

                info2.UserId.Should().Be( userId );
                info2.Info.GithubAccountId.Should().Be( googleAccountId );

                u.FindKnownUserInfo( ctx, Guid.NewGuid().ToString() ).Should().BeNull();
                user.DestroyUser( ctx, 1, userId );
                u.FindKnownUserInfo( ctx, googleAccountId ).Should().BeNull();
            }
        }

        [Test]
        public async Task create_Github_user_and_check_read_info_object_method_async()
        {
            var u = TestHelper.StObjMap.Default.Obtain<UserGithubTable>();
            var user = TestHelper.StObjMap.Default.Obtain<UserTable>();
            var infoFactory = TestHelper.StObjMap.Default.Obtain<IPocoFactory<IUserGithubInfo>>();
            using( var ctx = new SqlStandardCallContext() )
            {
                var userName = Guid.NewGuid().ToString();
                int userId = await user.CreateUserAsync( ctx, 1, userName );
                var googleAccountId = Guid.NewGuid().ToString( "N" );

                var info = infoFactory.Create();
                info.GithubAccountId = googleAccountId;
                var created = await u.CreateOrUpdateGithubUserAsync( ctx, 1, userId, info );
                created.OperationResult.Should().Be( UCResult.Created );
                var info2 = await u.FindKnownUserInfoAsync( ctx, googleAccountId );

                info2.UserId.Should().Be( userId );
                info2.Info.GithubAccountId.Should().Be( googleAccountId );

                (await u.FindKnownUserInfoAsync( ctx, Guid.NewGuid().ToString() )).Should().BeNull();
                await user.DestroyUserAsync( ctx, 1, userId );
                (await u.FindKnownUserInfoAsync( ctx, googleAccountId )).Should().BeNull();
            }
        }

        [Test]
        public void Github_AuthProvider_is_registered()
        {
            Auth.Tests.AuthTests.CheckProviderRegistration( "Github" );
        }

        [Test]
        public void vUserAuthProvider_reflects_the_user_Github_authentication()
        {
            var u = TestHelper.StObjMap.Default.Obtain<UserGithubTable>();
            var user = TestHelper.StObjMap.Default.Obtain<UserTable>();
            using( var ctx = new SqlStandardCallContext() )
            {
                string userName = "Github auth - " + Guid.NewGuid().ToString();
                var googleAccountId = Guid.NewGuid().ToString( "N" );
                var idU = user.CreateUser( ctx, 1, userName );
                u.Database.ExecuteReader( $"select * from CK.vUserAuthProvider where UserId={idU} and Scheme='Github'" )
                    .Rows.Should().BeEmpty();
                var info = u.CreateUserInfo<IUserGithubInfo>();
                info.GithubAccountId = googleAccountId;
                u.CreateOrUpdateGithubUser( ctx, 1, idU, info );
                u.Database.ExecuteScalar( $"select count(*) from CK.vUserAuthProvider where UserId={idU} and Scheme='Github'" )
                    .Should().Be( 1 );
                u.DestroyGithubUser( ctx, 1, idU );
                u.Database.ExecuteReader( $"select * from CK.vUserAuthProvider where UserId={idU} and Scheme='Github'" )
                    .Rows.Should().BeEmpty();
            }
        }

        [Test]
        public void standard_generic_tests_for_Github_provider()
        {
            var auth = TestHelper.StObjMap.Default.Obtain<Auth.Package>();
            // With IUserGithubInfo POCO.
            var f = TestHelper.StObjMap.Default.Obtain<IPocoFactory<IUserGithubInfo>>();
            CK.DB.Auth.Tests.AuthTests.StandardTestForGenericAuthenticationProvider(
                auth,
                "Github",
                payloadForCreateOrUpdate: ( userId, userName ) => f.Create( i => i.GithubAccountId = "GithubAccountIdFor:" + userName ),
                payloadForLogin: ( userId, userName ) => f.Create( i => i.GithubAccountId = "GithubAccountIdFor:" + userName ),
                payloadForLoginFail: ( userId, userName ) => f.Create( i => i.GithubAccountId = "NO!" + userName )
                );
            // With a KeyValuePair.
            CK.DB.Auth.Tests.AuthTests.StandardTestForGenericAuthenticationProvider(
                auth,
                "Github",
                payloadForCreateOrUpdate: ( userId, userName ) => new[]
                {
                    new KeyValuePair<string,object>( "GithubAccountId", "IdFor:" + userName)
                },
                payloadForLogin: ( userId, userName ) => new[]
                {
                    new KeyValuePair<string,object>( "GithubAccountId", "IdFor:" + userName)
                },
                payloadForLoginFail: ( userId, userName ) => new[]
                {
                    new KeyValuePair<string,object>( "GithubAccountId", ("IdFor:" + userName).ToUpperInvariant())
                }
                );
        }

        [Test]
        public async Task standard_generic_tests_for_Github_provider_Async()
        {
            var auth = TestHelper.StObjMap.Default.Obtain<Auth.Package>();
            var f = TestHelper.StObjMap.Default.Obtain<IPocoFactory<IUserGithubInfo>>();
            await Auth.Tests.AuthTests.StandardTestForGenericAuthenticationProviderAsync(
                auth,
                "Github",
                payloadForCreateOrUpdate: ( userId, userName ) => f.Create( i => i.GithubAccountId = "GithubAccountIdFor:" + userName ),
                payloadForLogin: ( userId, userName ) => f.Create( i => i.GithubAccountId = "GithubAccountIdFor:" + userName ),
                payloadForLoginFail: ( userId, userName ) => f.Create( i => i.GithubAccountId = "NO!" + userName )
                );
        }

    }

}

