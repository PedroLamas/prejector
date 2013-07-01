using System;

namespace ExampleApplication.WinRT.Models.Application
{
    public sealed class ApplicationState : IApplicationState
    {
        public ApplicationState(Guid guid)
        {
            InstallId = guid;
        }

        /// <summary>
        /// A unique ID for this application installation
        /// </summary>
        public Guid InstallId { get; private set; }
    }
}