--[beginscript]

alter table CK.tUserGitHub add
	RefreshToken varchar(max) collate Latin1_General_100_CI_AS not null constraint DF_TEMP1 default(N''),
	LastRefreshTokenTime datetime2(2) not null constraint DF_TEMP2 default('0001-01-01');

alter table CK.tUserGitHub drop constraint DF_TEMP1;
alter table CK.tUserGitHub drop constraint DF_TEMP2;

--[endscript]