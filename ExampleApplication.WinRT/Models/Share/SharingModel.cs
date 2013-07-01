using System;
using ExampleApplication.WinRT.Models.Application;

namespace ExampleApplication.WinRT.Models.Share
{
    /// <summary>
    ///     A model representing sharing in the application
    /// </summary>
    public sealed class SharingModel : ISharing
    {
        /// <summary>
        ///     An example of how models can depend on each other
        /// </summary>
        [Inject]
        public IApplicationState ApplicationState { private get; set; }

        public void ConfigureShareRequest(IShareRequest request)
        {
            string url = string.Format("http://prejector.com/{0}/{1}", ApplicationState.InstallId, request.Id);
            var uri = new Uri(url, UriKind.Absolute);

            request.SetUri(uri);
            request.Title = request.Id.ToString();
            request.Description = "A sharing event";
        }
    }
}