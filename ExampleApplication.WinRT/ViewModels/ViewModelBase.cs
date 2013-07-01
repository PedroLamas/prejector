using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExampleApplication.WinRT.Models;
using ExampleApplication.WinRT.Models.Share;

namespace ExampleApplication.WinRT.ViewModels
{
    public abstract class ViewModelBase : NotifyPropertyChangedBase
    {
        [Inject]
        public ISharing Sharing { private get; set; }

        public virtual void ConfigureShareRequest(IShareRequest request)
        {
            Sharing.ConfigureShareRequest(request);
        }
    }
}
