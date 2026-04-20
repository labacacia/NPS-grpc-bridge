// Copyright 2026 INNO LOTUS PTY LTD
// SPDX-License-Identifier: Apache-2.0

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace LabAcacia.GrpcBridge;

/// <summary>DI + pipeline extensions for the gRPC bridge.</summary>
public static class GrpcBridgeExtensions
{
    /// <summary>
    /// Register the gRPC bridge service with the given upstream configuration.
    /// Each upstream gets its own typed <c>HttpClient</c> via <c>IHttpClientFactory</c>.
    /// Call <see cref="MapGrpcBridge"/> in your pipeline to expose the service over gRPC.
    /// </summary>
    public static IServiceCollection AddGrpcBridge(
        this IServiceCollection services,
        Action<GrpcBridgeOptions> configure)
    {
        var opts = new GrpcBridgeOptions { Upstreams = Array.Empty<NwpUpstream>() };
        configure(opts);
        if (opts.Upstreams.Count == 0)
            throw new InvalidOperationException("GrpcBridgeOptions.Upstreams MUST contain at least one entry.");

        var dup = opts.Upstreams.GroupBy(u => u.Name).FirstOrDefault(g => g.Count() > 1);
        if (dup is not null)
            throw new InvalidOperationException($"Duplicate upstream name '{dup.Key}' in GrpcBridgeOptions.Upstreams.");

        services.AddSingleton(opts);
        services.AddHttpClient();
        services.AddGrpc();

        services.AddSingleton<IReadOnlyDictionary<string, NwpUpstreamClient>>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>();
            return opts.Upstreams.ToDictionary(
                u => u.Name,
                u => new NwpUpstreamClient(http.CreateClient($"grpc-bridge:{u.Name}"), u));
        });

        services.AddSingleton<NwpBridgeService>();

        return services;
    }

    /// <summary>
    /// Register the gRPC service endpoint for <see cref="NwpBridgeService"/>.
    /// The service is served at its default path
    /// <c>/labacacia.grpc_bridge.v1.NwpBridge</c>.
    /// </summary>
    public static GrpcServiceEndpointConventionBuilder MapGrpcBridge(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGrpcService<NwpBridgeService>();
    }
}
