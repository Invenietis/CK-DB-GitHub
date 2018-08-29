--[beginscript]

create table CK.tUserGitHub
(
	UserId int not null,
	-- The GitHub account identifier is the key to identify a GitHub user.
	GitHubAccountId varchar(36) collate Latin1_General_100_BIN2 not null,
	LastLoginTime datetime2(2) not null,
	constraint PK_CK_UserGitHub primary key (UserId),
	constraint FK_CK_UserGitHub_UserId foreign key (UserId) references CK.tUser(UserId),
	constraint UK_CK_UserGitHub_GitHubAccountId unique( GitHubAccountId )
);

insert into CK.tUserGitHub( UserId, GitHubAccountId, LastLoginTime ) 
	values( 0, '', sysutcdatetime() );

--[endscript]
