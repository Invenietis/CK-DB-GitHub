--[beginscript]

create table CK.tUserGithub
(
	UserId int not null,
	-- The Github account identifier is the key to identify a Github user.
	GithubAccountId varchar(36) collate Latin1_General_100_BIN2 not null,
	LastLoginTime datetime2(2) not null,
	constraint PK_CK_UserGithub primary key (UserId),
	constraint FK_CK_UserGithub_UserId foreign key (UserId) references CK.tUser(UserId),
	constraint UK_CK_UserGithub_GithubAccountId unique( GithubAccountId )
);

insert into CK.tUserGithub( UserId, GithubAccountId, LastLoginTime ) 
	values( 0, '', sysutcdatetime() );

--[endscript]
