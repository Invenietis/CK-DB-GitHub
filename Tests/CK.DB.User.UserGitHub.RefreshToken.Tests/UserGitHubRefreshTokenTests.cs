using CK.Core;
using CK.DB.Actor;
using CK.SqlServer;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Threading;
using static CK.Testing.DBSetupTestHelper;

namespace CK.DB.User.UserGitHub.RefreshToken.Tests
{
    [TestFixture]
    public class UserGitHubRefreshTokenTests
    {
        [Test]
        public void RefreshToken_and_LastRefreshTokenTime_are_managed()
        {
            var GitHub = TestHelper.StObjMap.StObjs.Obtain<UserGitHubTable>();
            var user = TestHelper.StObjMap.StObjs.Obtain<UserTable>();
            using( var ctx = new SqlStandardCallContext() )
            {
                string userName = "GitHub RefreshToken - " + Guid.NewGuid().ToString();
                var GitHubAccountId = Guid.NewGuid().ToString( "N" );
                var idU = user.CreateUser( ctx, 1, userName );

                var info = GitHub.CreateUserInfo<IUserGitHubInfo>();
                info.GitHubAccountId = GitHubAccountId;
                GitHub.CreateOrUpdateGitHubUser( ctx, 1, idU, info );
                string rawSelect = $"select RefreshToken+'|'+cast(LastRefreshTokenTime as varchar) from CK.tUserGitHub where UserId={idU}";
                GitHub.Database.ExecuteScalar( rawSelect )
                    .Should().Be( "|0001-01-01 00:00:00.00" );

                info.RefreshToken = "a refresh token";
                GitHub.CreateOrUpdateGitHubUser( ctx, 1, idU, info );
                rawSelect = $"select RefreshToken from CK.tUserGitHub where UserId={idU}";
                GitHub.Database.ExecuteScalar( rawSelect )
                    .Should().Be( info.RefreshToken );

                info = (IUserGitHubInfo)GitHub.FindKnownUserInfo( ctx, GitHubAccountId ).Info;
                info.LastRefreshTokenTime.Should().BeAfter( DateTime.UtcNow.AddMonths( -1 ) );
                info.RefreshToken.Should().Be( "a refresh token" );

                var lastUpdate = info.LastRefreshTokenTime;
                Thread.Sleep( 500 );
                info.RefreshToken = null;
                GitHub.CreateOrUpdateGitHubUser( ctx, 1, idU, info );
                info = (IUserGitHubInfo)GitHub.FindKnownUserInfo( ctx, GitHubAccountId ).Info;
                info.LastRefreshTokenTime.Should().Be( lastUpdate );
                info.RefreshToken.Should().Be( "a refresh token" );
            }
        }

    }
}
