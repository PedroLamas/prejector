using System;
using Windows.ApplicationModel.DataTransfer;

namespace ExampleApplication.WinRT.Models.Share
{
    internal sealed class ShareRequestAdapter : IShareRequest
    {
        private readonly DataPackage _data;
        private readonly DataRequest _dataRequest;
        private readonly DataPackagePropertySet _properties;

        public ShareRequestAdapter(DataRequest dataRequest, Guid id)
        {
            _dataRequest = dataRequest;
            _data = _dataRequest.Data;
            _properties = _data.Properties;
            _properties.ApplicationName = "ExampleApplication.WinRT";
            Id = id;
        }

        public void FailWithDisplayText(string text)
        {
            _dataRequest.FailWithDisplayText(text);
        }

        public string ApplicationName
        {
            get { return _properties.ApplicationName; }
        }

        public string Title
        {
            get { return _properties.Title; }
            set { _properties.Title = value; }
        }

        public string Description
        {
            get { return _properties.Description; }
            set { _properties.Description = value; }
        }

        public Guid Id { get; private set; }

        public void SetUri(Uri uri)
        {
            _data.SetUri(uri);
        }

        public void SetText(string text)
        {
            _data.SetText(text);
        }
    }
}