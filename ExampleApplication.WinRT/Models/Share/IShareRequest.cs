using System;

namespace ExampleApplication.WinRT.Models.Share
{
    public interface IShareRequest
    {
        void FailWithDisplayText(string format);
        string ApplicationName { get; }
        string Title { get; set; }
        string Description { get; set; }
        Guid Id { get; }
        void SetUri(Uri uri);
        void SetText(string text);
    }
}