using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace SicarioPatch.App.Infrastructure;

public abstract class UserAuthorization
{
    // ReSharper disable once InconsistentNaming
    private protected readonly AccessOptions? _opts;
    private readonly Func<AccessOptions, List<string>>? _selector;
    private const string UserIdClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
    private const string DiscriminatorClaim = "urn:discord:user:discriminator";

    private UserAuthorization(AccessOptions opts)
    {
        _opts = opts;
    }

    protected UserAuthorization(AccessOptions opts, Func<AccessOptions, List<string>> userFunc) : this(opts)
    {
        _selector = userFunc;
    }

    internal bool AllowAllAuthenticated =>
        _opts != null && _selector != null && _selector(_opts).Any(static u => u == "*");

    internal bool AllowsUser(ClaimsPrincipal principal)
    {
        return _opts != null
               && _selector != null
               && _selector(_opts).Any(u =>
               {
                   var user = u.ToLower();
                   return user.All(char.IsDigit)
                       ? user == principal.FindFirst(UserIdClaim)?.Value.ToLower()
                       : user.Contains("#")
                           ? user ==
                             $"{principal.Identity?.Name?.ToLower()}#{principal.FindFirst(DiscriminatorClaim)?.Value}"
                           : user == principal.Identity?.Name?.ToLower();
               });
    }
}

[PublicAPI]
public sealed class UserRequirement : UserAuthorization, IAuthorizationRequirement
{
    internal UserRequirement(AccessOptions opts) : base(opts, static o => o.AllowedUsers)
    {
    }

    internal bool AllowAll => _opts != null && _opts.AllowedUsers.Any(static u => u == "**");

    public List<string>? AllowedUsers => _opts?.AllowedUsers;
}

[PublicAPI]
public sealed class UploaderRequirement : UserAuthorization, IAuthorizationRequirement
{
    internal UploaderRequirement(AccessOptions opts) : base(opts, static o => o.AllowedUploaders)
    {
    }

    public List<string>? AllowedUsers => _opts?.AllowedUploaders;
}

[PublicAPI]
public sealed class UserAccessHandler : AuthorizationHandler<UserRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        UserRequirement requirement)
    {
        if (requirement.AllowAll)
            context.Succeed(requirement);
        else if (requirement.AllowAllAuthenticated && !string.IsNullOrWhiteSpace(context.User.Identity?.Name))
            context.Succeed(requirement);
        else if (requirement.AllowsUser(context.User)) context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

[PublicAPI]
public sealed class UploadAccessHandler : AuthorizationHandler<UploaderRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        UploaderRequirement requirement)
    {
        if (requirement.AllowAllAuthenticated)
            context.Succeed(requirement);
        else if (context.User.Identity?.Name != null && requirement.AllowsUser(context.User))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}