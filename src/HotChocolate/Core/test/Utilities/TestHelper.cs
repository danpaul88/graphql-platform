using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.StarWars;
using HotChocolate.Types;
using Xunit.Abstractions;

namespace HotChocolate.Tests;

public static class TestHelper
{
    public static Task<IExecutionResult> ExpectValid(
        string query,
        Action<IRequestExecutorBuilder>? configure = null,
        Action<IQueryRequestBuilder>? request = null,
        IServiceProvider? requestServices = null)
    {
        return ExpectValid(
            query,
            new TestConfiguration
            {
                ConfigureRequest = request, Configure = configure, Services = requestServices,
            });
    }

    public static async Task<IExecutionResult> ExpectValid(
        string query,
        TestConfiguration? configuration,
        CancellationToken cancellationToken = default)
    {
        // arrange
        var executor = await CreateExecutorAsync(configuration);
        var request = CreateRequest(configuration, query);

        // act
        var result = await executor.ExecuteAsync(request, cancellationToken);

        // assert
        Assert.Null(Assert.IsType<QueryResult>(result).Errors);
        return result;
    }

    public static Task ExpectError(
        string sdl,
        string query,
        Action<IRequestExecutorBuilder>? configure = null,
        Action<IQueryRequestBuilder>? request = null,
        IServiceProvider? requestServices = null,
        params Action<IError>[] elementInspectors) =>
        ExpectError(
            query,
            b =>
            {
                b.AddDocumentFromString(sdl).UseNothing();
                configure?.Invoke(b);
            },
            request,
            requestServices,
            elementInspectors);

    public static Task ExpectError(
        string query,
        Action<IRequestExecutorBuilder>? configure = null,
        Action<IQueryRequestBuilder>? request = null,
        IServiceProvider? requestServices = null,
        params Action<IError>[] elementInspectors)
    {
        return ExpectError(
            query,
            new TestConfiguration
            {
                Configure = configure, ConfigureRequest = request, Services = requestServices,
            },
            elementInspectors);
    }

    public static async Task ExpectError(
        string query,
        TestConfiguration? configuration,
        params Action<IError>[] elementInspectors)
    {
        // arrange
        var executor = await CreateExecutorAsync(configuration);
        var request = CreateRequest(configuration, query);

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        IQueryResult queryResult = Assert.IsType<QueryResult>(result);
        Assert.NotNull(queryResult.Errors);

        if (elementInspectors.Length > 0)
        {
            Assert.Collection(queryResult.Errors!, elementInspectors);
        }

        await queryResult.MatchSnapshotAsync();
    }

    public static async Task<T> CreateTypeAsync<T>()
        where T : INamedType
    {
        var schema = await CreateSchemaAsync(
            c => c
                .AddQueryType(d => d.Name("Query").Field("foo").Resolve("result"))
                .AddType<T>()
                .ModifyOptions(o => o.StrictValidation = false));
        return schema.Types.OfType<T>().Single();
    }

    public static async Task<T> CreateTypeAsync<T>(T type)
        where T : INamedType
    {
        var schema = await CreateSchemaAsync(type);
        return schema.GetType<T>(type.Name);
    }

    public static Task<ISchema> CreateSchemaAsync(
        INamedType type)
    {
        return CreateSchemaAsync(
            c => c
                .AddQueryType(d => d.Name("Query").Field("foo").Resolve("result"))
                .AddType(type)
                .ModifyOptions(o => o.StrictValidation = false));
    }

    public static async Task<ISchema> CreateSchemaAsync(
        Action<IRequestExecutorBuilder> configure,
        bool strict = false)
    {
        var executor = await CreateExecutorAsync(
            c =>
            {
                configure.Invoke(c);
                c.ModifyOptions(o => o.StrictValidation = strict);
            });
        return executor.Schema;
    }

    public static async Task<IRequestExecutor> CreateExecutorAsync(
        Action<IRequestExecutorBuilder>? configure = null,
        ITestOutputHelper? output = null)
    {
        var configuration = new TestConfiguration { Configure = configure, };

        return await CreateExecutorAsync(configuration, output);
    }

    private static async ValueTask<IRequestExecutor> CreateExecutorAsync(
        TestConfiguration? configuration,
        ITestOutputHelper? output = null)
    {
        var builder = new ServiceCollection().AddGraphQL();

        if (configuration?.Configure is { } c)
        {
            c.Invoke(builder);
        }
        else
        {
            AddDefaultConfiguration(builder, output);
        }

        return await builder.Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync();
    }

    public static IQueryRequest CreateRequest(
        TestConfiguration? configuration,
        string query)
    {
        configuration ??= new TestConfiguration();

        var builder = QueryRequestBuilder.New().SetQuery(query);

        if (configuration.Services is { } services)
        {
            builder.SetServices(services);
        }

        if (configuration.ConfigureRequest is { } configure)
        {
            configure(builder);
        }

        return builder.Create();
    }

    public static void AddDefaultConfiguration(
        IRequestExecutorBuilder builder,
        ITestOutputHelper? output = null)
    {
        if (output is not null)
        {
            builder.AddDiagnosticEventListener(_ => new SubscriptionTestDiagnostics(output));
        }

        builder
            .AddStarWarsTypes()
            .AddInMemorySubscriptions()
            .Services
            .AddStarWarsRepositories();
    }
}