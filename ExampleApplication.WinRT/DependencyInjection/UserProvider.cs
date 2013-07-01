using System;
using ExampleApplication.WinRT.Models.Application;

namespace ExampleApplication.WinRT.DependencyInjection
{
    public class ApplicationStateProvider
    {
        public ApplicationState Create()
        {
            return new ApplicationState(Guid.NewGuid());
        }
    }
}