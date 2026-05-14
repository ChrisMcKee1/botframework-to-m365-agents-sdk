// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Source: https://github.com/microsoft/Agents/blob/main/samples/dotnet/quickstart/AspNetExtensions.cs
//
// This is a SAMPLE-LOCAL helper. The Microsoft 365 Agents SDK does not ship a
// built-in `AddAgentAspNetAuthentication` extension. Every Agents SDK sample
// copies this file (or its equivalent) into the project to wire inbound JWT
// validation for Azure Bot Service / Entra ID tokens.
//
// Read the TokenValidation section from appsettings.json and registers a JWT
// bearer scheme. Setting TokenValidation:Enabled = false skips registration
// (useful for local debugging with the Microsoft 365 Agents Playground).

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

public static class AspNetExtensions
{
    private static readonly ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> _openIdMetadataCache = new();

    public static void AddAgentAspNetAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string tokenValidationSectionName = "TokenValidation")
    {
        var section = configuration.GetSection(tokenValidationSectionName);

        if (!section.Exists() || !section.GetValue("Enabled", true))
        {
            System.Diagnostics.Trace.WriteLine("AddAgentAspNetAuthentication: Auth disabled");
            return;
        }

        var options = section.Get<TokenValidationOptions>()
            ?? throw new InvalidOperationException("TokenValidation section is empty");

        if (options.Audiences == null || options.Audiences.Count == 0)
        {
            throw new ArgumentException("TokenValidation:Audiences requires at least one ClientId");
        }

        foreach (var audience in options.Audiences)
        {
            if (!Guid.TryParse(audience, out _))
            {
                throw new ArgumentException("TokenValidation:Audiences values must be a GUID");
            }
        }

        // Defaults — Public cloud issuers + Azure Bot Service issuer.
        if (options.ValidIssuers == null || options.ValidIssuers.Count == 0)
        {
            options.ValidIssuers =
            [
                "https://api.botframework.com",
                "https://sts.windows.net/d6d49420-f39b-4df7-a1dc-d59a935871db/",
                "https://login.microsoftonline.com/d6d49420-f39b-4df7-a1dc-d59a935871db/v2.0",
                "https://sts.windows.net/f8cdef31-a31e-4b4a-93e4-5f571e91255a/",
                "https://login.microsoftonline.com/f8cdef31-a31e-4b4a-93e4-5f571e91255a/v2.0",
                "https://sts.windows.net/69e9b82d-4842-4902-8d1e-abc5b98a55e8/",
                "https://login.microsoftonline.com/69e9b82d-4842-4902-8d1e-abc5b98a55e8/v2.0",
            ];

            if (!string.IsNullOrEmpty(options.TenantId) && Guid.TryParse(options.TenantId, out _))
            {
                options.ValidIssuers.Add(string.Format(CultureInfo.InvariantCulture,
                    "https://sts.windows.net/{0}/", options.TenantId));
                options.ValidIssuers.Add(string.Format(CultureInfo.InvariantCulture,
                    "https://login.microsoftonline.com/{0}/v2.0", options.TenantId));
            }
        }

        if (string.IsNullOrEmpty(options.AzureBotServiceOpenIdMetadataUrl))
        {
            options.AzureBotServiceOpenIdMetadataUrl =
                "https://login.botframework.com/v1/.well-known/openidconfiguration";
        }

        if (string.IsNullOrEmpty(options.OpenIdMetadataUrl))
        {
            options.OpenIdMetadataUrl =
                "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration";
        }

        var openIdRefresh = options.OpenIdMetadataRefresh ?? BaseConfigurationManager.DefaultAutomaticRefreshInterval;

        services.AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(o =>
        {
            o.SaveToken = true;
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5),
                ValidIssuers = options.ValidIssuers,
                ValidAudiences = options.Audiences,
                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,
            };

            o.TokenValidationParameters.EnableAadSigningKeyIssuerValidation();

            o.Events = new JwtBearerEvents
            {
                OnMessageReceived = async ctx =>
                {
                    var auth = ctx.Request.Headers.Authorization.ToString();
                    if (string.IsNullOrEmpty(auth))
                    {
                        ctx.Options.TokenValidationParameters.ConfigurationManager ??=
                            o.ConfigurationManager as BaseConfigurationManager;
                        await Task.CompletedTask.ConfigureAwait(false);
                        return;
                    }

                    var parts = auth.Split(' ');
                    if (parts.Length != 2 || parts[0] != "Bearer")
                    {
                        ctx.Options.TokenValidationParameters.ConfigurationManager ??=
                            o.ConfigurationManager as BaseConfigurationManager;
                        await Task.CompletedTask.ConfigureAwait(false);
                        return;
                    }

                    var token = new JwtSecurityToken(parts[1]);
                    var issuer = token.Claims.FirstOrDefault(c => c.Type == "iss")?.Value;

                    var metadataUrl = options.AzureBotServiceTokenHandling &&
                        string.Equals(issuer, "https://api.botframework.com", StringComparison.OrdinalIgnoreCase)
                            ? options.AzureBotServiceOpenIdMetadataUrl!
                            : options.OpenIdMetadataUrl!;

                    ctx.Options.TokenValidationParameters.ConfigurationManager =
                        _openIdMetadataCache.GetOrAdd(metadataUrl, key =>
                            new ConfigurationManager<OpenIdConnectConfiguration>(
                                key,
                                new OpenIdConnectConfigurationRetriever(),
                                new HttpClient())
                            {
                                AutomaticRefreshInterval = openIdRefresh,
                            });

                    await Task.CompletedTask.ConfigureAwait(false);
                },
                OnTokenValidated = _ => Task.CompletedTask,
                OnForbidden = _ => Task.CompletedTask,
                OnAuthenticationFailed = _ => Task.CompletedTask,
            };
        });
    }

    public class TokenValidationOptions
    {
        public IList<string>? Audiences { get; set; }
        public string? TenantId { get; set; }
        public IList<string>? ValidIssuers { get; set; }
        public bool IsGov { get; set; } = false;
        public string? AzureBotServiceOpenIdMetadataUrl { get; set; }
        public string? OpenIdMetadataUrl { get; set; }
        public bool AzureBotServiceTokenHandling { get; set; } = true;
        public TimeSpan? OpenIdMetadataRefresh { get; set; }
    }
}
