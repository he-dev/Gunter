using System;
using System.Threading.Tasks;
using Gunter.Data.Configuration.Abstractions;
using Gunter.Services.Abstractions;

namespace Gunter.Services.DispatchMessage
{
    public class ThrowOperationCanceledException : IDispatchMessage
    {
        public Task InvokeAsync(ITask task)
        {
            throw new OperationCanceledException();
        }
    }
}