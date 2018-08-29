-- SetupConfig: {}
--
create procedure CK.sUserGitHubDestroy
(
	@ActorId int,
	@UserId int
)
as
begin
    if @ActorId <= 0 throw 50000, 'Security.AnonymousNotAllowed', 1;
    if @UserId = 0 throw 50000, 'Argument.InvalidValue', 1;

	--[beginsp]

	--<PreDestroy revert /> 
	
	delete CK.tUserGitHub where UserId = @UserId;

	--<PostDestroy /> 

	--[endsp]
end
