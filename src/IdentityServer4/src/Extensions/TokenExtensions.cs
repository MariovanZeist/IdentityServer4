// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using IdentityModel;
using IdentityServer4.Configuration;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;

namespace IdentityServer4.Extensions
{
    /// <summary>
    /// Extensions for Token
    /// </summary>
    public static class TokenExtensions
    {
        /// <summary>
        /// Creates the default JWT payload.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="clock">The clock.</param>
        /// <param name="options">The options</param>
        /// <param name="logger">The logger.</param>
        /// <returns></returns>
        /// <exception cref="Exception">
        /// </exception>
        public static JwtPayload CreateJwtPayload(this Token token, ISystemClock clock, IdentityServerOptions options, ILogger logger)
        {
            try
            {
                var payload = new JwtPayload(
                token.Issuer,
                null,
                null,
                clock.UtcNow.UtcDateTime,
                clock.UtcNow.UtcDateTime.AddSeconds(token.Lifetime));

                if (token.Audiences.Any())
                {
                    if (token.Audiences.Count == 1)
                    {
                        payload.Add(JwtClaimTypes.Audience, token.Audiences.First());
                    }
                    else
                    {
                        payload.Add(JwtClaimTypes.Audience, token.Audiences);
                    }
                }

                // add confirmation claim (if present)
                if (token.Confirmation.IsPresent())
                {
                    payload.Add(JwtClaimTypes.Confirmation,
                        JsonSerializer.Deserialize<JsonElement>(token.Confirmation));
                }

                // scope claims
                var scopeClaims = token.Claims.Where(x => x.Type == JwtClaimTypes.Scope).ToArray();
                if (!scopeClaims.IsNullOrEmpty())
                {
                    var scopeValues = scopeClaims.Select(x => x.Value).ToArray();

                    if (options.EmitScopesAsSpaceDelimitedStringInJwt)
                    {
                        payload.Add(JwtClaimTypes.Scope, string.Join(" ", scopeValues));
                    }
                    else
                    {
                        payload.Add(JwtClaimTypes.Scope, scopeValues);
                    }
                }

                // amr claims
                var amrClaims = token.Claims.Where(x => x.Type == JwtClaimTypes.AuthenticationMethod).ToArray();
                if (!amrClaims.IsNullOrEmpty())
                {
                    var amrValues = amrClaims.Select(x => x.Value).Distinct().ToArray();
                    payload.Add(JwtClaimTypes.AuthenticationMethod, amrValues);
                }

                var simpleClaimTypes = token.Claims.Where(c =>
                        c.Type != JwtClaimTypes.AuthenticationMethod && c.Type != JwtClaimTypes.Scope)
                    .Select(c => c.Type)
                    .Distinct();

                // other claims
                foreach (var claimType in simpleClaimTypes)
                {
                    // we ignore claims that are added by the above code for token verification
                    if (!payload.ContainsKey(claimType))
                    {
                        var claims = token.Claims.Where(c => c.Type == claimType).ToArray();

                        if (claims.Count() > 1)
                        {
                            payload.Add(claimType, AddObjects(claims));
                        }
                        else
                        {
                            payload.Add(claimType, AddObject(claims.First()));
                        }
                    }
                }

                return payload;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error creating a JSON valued claim");
                throw;
            }
        }

        public static IEnumerable<object> AddObjects(IEnumerable<Claim> claims)
        {
            foreach (var claim in claims)
            {
                yield return AddObject(claim);
            }
        }

        public static object AddObject(Claim claim)
        {
            if (claim.ValueType == ClaimValueTypes.Boolean)
            {
                return bool.Parse(claim.Value);
            }

            if (claim.ValueType == ClaimValueTypes.Integer || claim.ValueType == ClaimValueTypes.Integer32)
            {
                return int.Parse(claim.Value);
            }

            if (claim.ValueType == ClaimValueTypes.Integer64)
            {
                return long.Parse(claim.Value);
            }

            if (claim.ValueType == ClaimValueTypes.Double)
            {
                return double.Parse(claim.Value);
            }

            if (claim.ValueType == IdentityServerConstants.ClaimValueTypes.Json)
            {
                return JsonSerializer.Deserialize<JsonElement>(claim.Value);
            }

            return claim.Value;
        }
    }
}