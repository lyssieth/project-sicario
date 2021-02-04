﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SicarioPatch.App.Infrastructure;

namespace SicarioPatch.App
{
    public static class StartupExtensions
    {
        public static AuthenticationBuilder AddDiscord(this AuthenticationBuilder builder, IConfigurationSection config)
        {
            return builder.AddDiscord(opts =>
            {
                opts.ClientId = config.GetValue<string>("ClientId");
                opts.ClientSecret = config.GetValue<string>("ClientSecret");
                foreach (var scope in config.GetValue<List<string>>("Scopes", new List<string> {"identify", "email"}))
                {
                    opts.Scope.Add(scope);
                }
            });

        }

        public static IServiceCollection AddAuthHandlers(this IServiceCollection services, IConfigurationSection config)
        {
            services.AddAuthorization(opts => opts.UseUserPolicies(config));
            return services.AddSingleton<IAuthorizationHandler, UserAccessHandler>()
                .AddSingleton<IAuthorizationHandler, UploadAccessHandler>();
        }

        public static AuthorizationOptions UseUserPolicies(this AuthorizationOptions opts, IConfigurationSection config)
        {
            var conf = config.Get<AccessOptions>();
            opts.AddPolicy(Policies.IsUser, policy => policy.Requirements.Add(new UserRequirement(conf)));
            opts.AddPolicy(Policies.IsUploader, policy => policy.Requirements.Add(new UploaderRequirement(conf)));
            return opts;
        }
        
        
    }
}