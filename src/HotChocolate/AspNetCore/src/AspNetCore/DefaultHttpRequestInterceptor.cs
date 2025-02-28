using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore;

/// <summary>
/// The HTTP request interceptor allows to manipulate the GraphQL
/// request creation and the GraphQL request response creation.
/// </summary>
public class DefaultHttpRequestInterceptor : IHttpRequestInterceptor
{
    /// <inheritdoc cref="IHttpRequestInterceptor.OnCreateAsync"/>
    public virtual ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        IQueryRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        var userState = new UserState(context.User);

        requestBuilder.TrySetServices(context.RequestServices);
        requestBuilder.TryAddGlobalState(nameof(HttpContext), context);
        requestBuilder.TryAddGlobalState(nameof(CancellationToken), context.RequestAborted);
        requestBuilder.TryAddGlobalState(nameof(ClaimsPrincipal), userState.User);
        requestBuilder.TryAddGlobalState(WellKnownContextData.UserState, userState);

        if (context.IsTracingEnabled())
        {
            requestBuilder.TryAddGlobalState(WellKnownContextData.EnableTracing, true);
        }

        if (context.IncludeQueryPlan())
        {
            requestBuilder.TryAddGlobalState(WellKnownContextData.IncludeQueryPlan, true);
        }

        return default;
    }
}
