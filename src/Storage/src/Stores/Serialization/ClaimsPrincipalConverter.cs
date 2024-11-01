// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using IdentityModel;

#pragma warning disable 1591

namespace IdentityServer4.Stores.Serialization
{
    public class ClaimsPrincipalConverter : JsonConverter<ClaimsPrincipal>
    {
        public override ClaimsPrincipal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var source = JsonSerializer.Deserialize<ClaimsPrincipalLite>(ref reader, options);
            var claims = source.Claims.Select(x => new Claim(x.Type, x.Value, x.ValueType));
            var id = new ClaimsIdentity(claims, source.AuthenticationType, JwtClaimTypes.Name, JwtClaimTypes.Role);
            var target = new ClaimsPrincipal(id);
            return target;
        }

        public override void Write(Utf8JsonWriter writer, ClaimsPrincipal value, JsonSerializerOptions options)
        {
            var target = new ClaimsPrincipalLite
            {
                AuthenticationType = value.Identity.AuthenticationType,
                Claims = value.Claims.Select(x => new ClaimLite { Type = x.Type, Value = x.Value, ValueType = x.ValueType }).ToArray()
            };
            JsonSerializer.Serialize(writer, target, options);
        }
    }
}
