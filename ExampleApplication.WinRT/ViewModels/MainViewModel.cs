using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ExampleApplication.WinRT.Models;
using Nokia.Entertainment.Music.Client.Commands;

namespace ExampleApplication.WinRT.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            GetDataCommand = new DelegateCommand(OnGetDataCommand);
            Data = new ObservableCollection<string>();
        }

        [Inject]
        public IDataService DataService { private get; set; }

        public ICommand GetDataCommand { get; private set; }

        public ObservableCollection<string> Data { get; private set; }

        private void OnGetDataCommand()
        {
            IList<string> newData = DataService.GetData();

            Data.Clear();

            foreach (string item in newData)
            {
                Data.Add(item);
            }
        }
    }
}