using HotChocolate.Resolvers;

namespace HotChocolate.Types;

#pragma warning disable CA1812

internal sealed class ExecutableMiddleware
{
    private readonly FieldDelegate _next;

    public ExecutableMiddleware(FieldDelegate next)
    {
        _next = next;
    }

    public async ValueTask InvokeAsync(IMiddlewareContext context)
    {
        await _next(context).ConfigureAwait(false);

        if (context.Result is IExecutable executable)
        {
            context.Result = await executable
                .ToListAsync(context.RequestAborted)
                .ConfigureAwait(false);
        }
    }
}
