using System.Collections.Generic;

namespace ExampleApplication.WinRT.Models
{
    public interface IDataService
    {
        IList<string> GetData();
    }
}