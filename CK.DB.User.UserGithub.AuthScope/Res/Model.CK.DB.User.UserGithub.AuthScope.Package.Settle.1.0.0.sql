--[beginscript]

-- Creates the default template scope set for new users.
declare @DefaultScopeSetId int;
exec CK.sAuthScopeSetCreate 1, N'', @ScopeSetIdResult = @DefaultScopeSetId output;
update CK.tUserGithub set ScopeSetId = @DefaultScopeSetId where UserId = 0;

-- Replicates the default template on all existing UserGithub.
declare @UserId int;
declare @CUser cursor;
set @CUser = cursor local fast_forward for 
	select UserId from CK.tUserGithub u where u.ScopeSetId = 0;
open @CUser;
fetch from @CUser into @UserId;
while @@FETCH_STATUS = 0
begin
	declare @NewScopeId int;
	exec CK.sAuthScopeSetCopy @ActorId = 1, @ScopeSetId = @DefaultScopeSetId, @ForceWARStatus = 'W', @ScopeSetIdResult = @NewScopeId output
	update CK.tUserGithub set ScopeSetId = @NewScopeId where UserId = @UserId;
	fetch next from @CUser into @UserId;
end
deallocate @CUser;

-- Now that each Github user has a dedicated ScopeSet, we can ensure its unicity
-- so that no two users can share the same ScopeSet.
alter table CK.tUserGithub add
	constraint UK_CK_UserGithub_ScopeSetId unique( ScopeSetId )

--[endscript]
