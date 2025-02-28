using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using CookieCrumble;
using HotChocolate.Execution.Configuration;

namespace HotChocolate.Types;

public class SchemaTests
{
    [Fact]
    public async Task SchemaSnapshot()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddCustomModule()
                .BuildSchemaAsync();

        schema.MatchMarkdownSnapshot();
    }
    
    [Fact]
    public async Task ExecuteRootField()
    {
        var services = new ServiceCollection()
            .AddGraphQL()
            .AddCustomModule();
        
        var result = await services.ExecuteRequestAsync("{ foo }");

        result.MatchMarkdownSnapshot();
    }
    
    [Fact]
    public async Task ExecuteWithMiddleware()
    {
        var services = new ServiceCollection()
            .AddSingleton<Service1>()
            .AddSingleton<Service2>()
            .AddSingleton<Service3>()
            .AddGraphQL()
            .AddCustomModule()
            .UseRequest<SomeRequestMiddleware>()
            .UseDefaultPipeline();
        
        var result = await services.ExecuteRequestAsync("{ foo }");

        result.MatchMarkdownSnapshot();
    }
}
