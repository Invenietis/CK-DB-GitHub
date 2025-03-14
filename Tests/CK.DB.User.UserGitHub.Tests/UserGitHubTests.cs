using CK.Core;
using CK.DB.Actor;
using CK.DB.Auth;
using CK.SqlServer;
using CK.Testing;
using Shouldly;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.DB.User.UserGitHub.Tests;

[TestFixture]
public class UserGitHubTests
{
    [Test]
    public void create_GitHub_user_and_check_read_info_object_method()
    {
        var u = SharedEngine.Map.StObjs.Obtain<UserGitHubTable>();
        var user = SharedEngine.Map.StObjs.Obtain<UserTable>();
        var infoFactory = SharedEngine.Map.StObjs.Obtain<IPocoFactory<IUserGitHubInfo>>();
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var userName = Guid.NewGuid().ToString();
            int userId = user.CreateUser( ctx, 1, userName );
            var googleAccountId = Guid.NewGuid().ToString( "N" );

            var info = infoFactory.Create();
            info.GitHubAccountId = googleAccountId;
            var created = u.CreateOrUpdateGitHubUser( ctx, 1, userId, info );
            created.OperationResult.ShouldBe( UCResult.Created );
            var info2 = u.FindKnownUserInfo( ctx, googleAccountId );

            info2.UserId.ShouldBe( userId );
            info2.Info.GitHubAccountId.ShouldBe( googleAccountId );

            u.FindKnownUserInfo( ctx, Guid.NewGuid().ToString() ).ShouldBeNull();
            user.DestroyUser( ctx, 1, userId );
            u.FindKnownUserInfo( ctx, googleAccountId ).ShouldBeNull();
        }
    }

    [Test]
    public async Task create_GitHub_user_and_check_read_info_object_method_Async()
    {
        var u = SharedEngine.Map.StObjs.Obtain<UserGitHubTable>();
        var user = SharedEngine.Map.StObjs.Obtain<UserTable>();
        var infoFactory = SharedEngine.Map.StObjs.Obtain<IPocoFactory<IUserGitHubInfo>>();
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var userName = Guid.NewGuid().ToString();
            int userId = await user.CreateUserAsync( ctx, 1, userName );
            var googleAccountId = Guid.NewGuid().ToString( "N" );

            var info = infoFactory.Create();
            info.GitHubAccountId = googleAccountId;
            var created = await u.CreateOrUpdateGitHubUserAsync( ctx, 1, userId, info );
            created.OperationResult.ShouldBe( UCResult.Created );
            var info2 = await u.FindKnownUserInfoAsync( ctx, googleAccountId );

            info2.UserId.ShouldBe( userId );
            info2.Info.GitHubAccountId.ShouldBe( googleAccountId );

            (await u.FindKnownUserInfoAsync( ctx, Guid.NewGuid().ToString() )).ShouldBeNull();
            await user.DestroyUserAsync( ctx, 1, userId );
            (await u.FindKnownUserInfoAsync( ctx, googleAccountId )).ShouldBeNull();
        }
    }

    [Test]
    public void GitHub_AuthProvider_is_registered()
    {
        Auth.Tests.AuthTests.CheckProviderRegistration( "GitHub" );
    }

    [Test]
    public void vUserAuthProvider_reflects_the_user_GitHub_authentication()
    {
        var u = SharedEngine.Map.StObjs.Obtain<UserGitHubTable>();
        var user = SharedEngine.Map.StObjs.Obtain<UserTable>();
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            string userName = "GitHub auth - " + Guid.NewGuid().ToString();
            var googleAccountId = Guid.NewGuid().ToString( "N" );
            var idU = user.CreateUser( ctx, 1, userName );
            u.Database.ExecuteReader( $"select * from CK.vUserAuthProvider where UserId={idU} and Scheme='GitHub'" )
                .Rows.ShouldBeEmpty();
            var info = u.CreateUserInfo<IUserGitHubInfo>();
            info.GitHubAccountId = googleAccountId;
            u.CreateOrUpdateGitHubUser( ctx, 1, idU, info );
            u.Database.ExecuteScalar( $"select count(*) from CK.vUserAuthProvider where UserId={idU} and Scheme='GitHub'" )
                .ShouldBe( 1 );
            u.DestroyGitHubUser( ctx, 1, idU );
            u.Database.ExecuteReader( $"select * from CK.vUserAuthProvider where UserId={idU} and Scheme='GitHub'" )
                .Rows.ShouldBeEmpty();
        }
    }

    [Test]
    public void standard_generic_tests_for_GitHub_provider()
    {
        var auth = SharedEngine.Map.StObjs.Obtain<Auth.Package>();
        // With IUserGitHubInfo POCO.
        var f = SharedEngine.Map.StObjs.Obtain<IPocoFactory<IUserGitHubInfo>>();
        CK.DB.Auth.Tests.AuthTests.StandardTestForGenericAuthenticationProvider(
            auth,
            "GitHub",
            payloadForCreateOrUpdate: ( userId, userName ) => f.Create( i => i.GitHubAccountId = "GitHubAccountIdFor:" + userName ),
            payloadForLogin: ( userId, userName ) => f.Create( i => i.GitHubAccountId = "GitHubAccountIdFor:" + userName ),
            payloadForLoginFail: ( userId, userName ) => f.Create( i => i.GitHubAccountId = "NO!" + userName )
            );
        // With a KeyValuePair.
        CK.DB.Auth.Tests.AuthTests.StandardTestForGenericAuthenticationProvider(
            auth,
            "GitHub",
            payloadForCreateOrUpdate: ( userId, userName ) => new[]
            {
                new KeyValuePair<string,object>( "GitHubAccountId", "IdFor:" + userName)
            },
            payloadForLogin: ( userId, userName ) => new[]
            {
                new KeyValuePair<string,object>( "GitHubAccountId", "IdFor:" + userName)
            },
            payloadForLoginFail: ( userId, userName ) => new[]
            {
                new KeyValuePair<string,object>( "GitHubAccountId", ("IdFor:" + userName).ToUpperInvariant())
            }
            );
    }

    [Test]
    public async Task standard_generic_tests_for_GitHub_provider_Async()
    {
        var auth = SharedEngine.Map.StObjs.Obtain<Auth.Package>();
        var f = SharedEngine.Map.StObjs.Obtain<IPocoFactory<IUserGitHubInfo>>();
        await Auth.Tests.AuthTests.StandardTestForGenericAuthenticationProviderAsync(
            auth,
            "GitHub",
            payloadForCreateOrUpdate: ( userId, userName ) => f.Create( i => i.GitHubAccountId = "GitHubAccountIdFor:" + userName ),
            payloadForLogin: ( userId, userName ) => f.Create( i => i.GitHubAccountId = "GitHubAccountIdFor:" + userName ),
            payloadForLoginFail: ( userId, userName ) => f.Create( i => i.GitHubAccountId = "NO!" + userName )
            );
    }

}

