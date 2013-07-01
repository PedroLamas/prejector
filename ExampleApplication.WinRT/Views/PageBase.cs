using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace ExampleApplication.WinRT.Views
{
    public abstract class PageBase : Page
    {
        private DataTransferManager _dataTransferManager;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (_dataTransferManager == null)
            {
                _dataTransferManager = DataTransferManager.GetForCurrentView();
            }

            _dataTransferManager.DataRequested += OnShareDataRequested;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            _dataTransferManager.DataRequested -= OnShareDataRequested;
        }

        protected abstract void OnShareDataRequested(DataTransferManager sender, DataRequestedEventArgs args);
    }
}