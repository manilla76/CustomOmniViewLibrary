using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomOmniViewLibrary
{
    public class QcModel : ViewModelBase
    {
        private string qc;
        public string Qc { get => qc; set => Set(ref qc, value); }
        private double modifier;
        public double Modifier { get => modifier;
            set { Set(ref modifier, value); } }
    }
}
