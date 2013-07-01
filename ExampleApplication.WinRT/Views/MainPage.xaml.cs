using System;
using ExampleApplication.WinRT.DependencyInjection;
using ExampleApplication.WinRT.Models.Share;
using ExampleApplication.WinRT.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml.Navigation;

namespace ExampleApplication.WinRT.Views
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : PageBase
    {
        private readonly MainViewModel _viewModel;

        public MainPage()
        {
            InitializeComponent();
            _viewModel = Kernel_MainViewModel.Get();
            DataContext = _viewModel;
        }

        protected override void OnShareDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var request = new ShareRequestAdapter(args.Request, Guid.NewGuid());
            _viewModel.ConfigureShareRequest(request);
        }
    }
}