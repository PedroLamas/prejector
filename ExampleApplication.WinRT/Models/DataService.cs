using System;
using System.Collections.Generic;

namespace ExampleApplication.WinRT.Models
{
    public sealed class DataService : IDataService
    {
        public IList<string> GetData()
        {
            var data = new List<string>();

            for (int i = 0; i < 20; i++)
            {
                data.Add(Guid.NewGuid().ToString("N"));
            }

            return data;
        }
    }
}