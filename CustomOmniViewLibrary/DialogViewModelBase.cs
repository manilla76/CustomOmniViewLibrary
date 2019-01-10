using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomOmniViewLibrary
{
    public class DialogViewModelBase : ViewModelBase
    {
        private bool? dialogResult;

        public event EventHandler Closing;

        public string Title { get; private set; }

        public bool? DialogResult { get => dialogResult; set => Set(ref dialogResult, value); }

        public void Close()
        {
            Closing?.Invoke(this, EventArgs.Empty);
        }
    }
}
