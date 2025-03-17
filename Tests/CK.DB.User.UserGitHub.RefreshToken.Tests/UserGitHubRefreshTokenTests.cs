using CK.Core;
using CK.DB.Actor;
using CK.SqlServer;
using CK.Testing;
using Shouldly;
using NUnit.Framework;
using System;
using System.Threading;
using static CK.Testing.MonitorTestHelper;

namespace CK.DB.User.UserGitHub.RefreshToken.Tests;

[TestFixture]
public class UserGitHubRefreshTokenTests
{
    [Test]
    public void RefreshToken_and_LastRefreshTokenTime_are_managed()
    {
        var GitHub = SharedEngine.Map.StObjs.Obtain<UserGitHubTable>();
        var user = SharedEngine.Map.StObjs.Obtain<UserTable>();
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            string userName = "GitHub RefreshToken - " + Guid.NewGuid().ToString();
            var GitHubAccountId = Guid.NewGuid().ToString( "N" );
            var idU = user.CreateUser( ctx, 1, userName );

            var info = GitHub.CreateUserInfo<IUserGitHubInfo>();
            info.GitHubAccountId = GitHubAccountId;
            GitHub.CreateOrUpdateGitHubUser( ctx, 1, idU, info );
            string rawSelect = $"select RefreshToken+'|'+cast(LastRefreshTokenTime as varchar) from CK.tUserGitHub where UserId={idU}";
            GitHub.Database.ExecuteScalar( rawSelect )
                .ShouldBe( "|0001-01-01 00:00:00.00" );

            info.RefreshToken = "a refresh token";
            GitHub.CreateOrUpdateGitHubUser( ctx, 1, idU, info );
            rawSelect = $"select RefreshToken from CK.tUserGitHub where UserId={idU}";
            GitHub.Database.ExecuteScalar( rawSelect )
                .ShouldBe( info.RefreshToken );

            info = (IUserGitHubInfo)GitHub.FindKnownUserInfo( ctx, GitHubAccountId ).Info;
            info.LastRefreshTokenTime.ShouldBeGreaterThan( DateTime.UtcNow.AddMonths( -1 ) );
            info.RefreshToken.ShouldBe( "a refresh token" );

            var lastUpdate = info.LastRefreshTokenTime;
            Thread.Sleep( 500 );
            info.RefreshToken = null;
            GitHub.CreateOrUpdateGitHubUser( ctx, 1, idU, info );
            info = (IUserGitHubInfo)GitHub.FindKnownUserInfo( ctx, GitHubAccountId ).Info;
            info.LastRefreshTokenTime.ShouldBe( lastUpdate );
            info.RefreshToken.ShouldBe( "a refresh token" );
        }
    }

}
