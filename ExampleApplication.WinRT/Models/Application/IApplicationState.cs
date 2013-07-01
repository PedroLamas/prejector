using System;

namespace ExampleApplication.WinRT.Models.Application
{
    public interface IApplicationState
    {
        /// <summary>
        /// A unique ID for this application installation
        /// </summary>
        Guid InstallId { get; }
    }
}