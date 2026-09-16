using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace HumanOS.SimulationLabs.Api.Common;

/// <summary>Populates the invocation-scoped <see cref="ICurrentUserContext"/> before the Function body runs.</summary>
public sealed class CurrentUserInitializationMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var currentUser = context.InstanceServices.GetService(typeof(ICurrentUserContext)) as CurrentUserContext;
        currentUser?.Initialize(context);

        await next(context);
    }
}
