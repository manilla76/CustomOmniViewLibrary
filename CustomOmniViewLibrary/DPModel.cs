using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomOmniViewLibrary
{
    public class DPModel<T> : ViewModelBase where T : IComparable
    {
        private Thermo.Datapool.Datapool.ITagInfo value;
        private string name;
        private T highAlarm;
        private T lowAlarm;
        private bool isHighAlarm;
        private bool isLowAlarm;
        private bool isSubscribed = false;

        public string Name { get => name; set => Set(ref name, value); }
        public Thermo.Datapool.Datapool.ITagInfo Value
        {
            get => value;
            set
            {
                Set(ref this.value, value);
                if (isSubscribed == false)
                {
                    Value.UpdateValueEvent += Value_UpdateValueEvent;
                    isSubscribed = true;
                }
                IsHighAlarm = Value.AsDouble.CompareTo(HighAlarm) == 1;
                IsLowAlarm = Value.AsDouble.CompareTo(LowAlarm) == -1;
                OnProptertyChanged();
            }
        }

        public T HighAlarm { get => highAlarm; set { Set(ref highAlarm, value); IsHighAlarm = Value?.AsDouble.CompareTo(HighAlarm) == 1; } }
        public T LowAlarm { get => lowAlarm; set { Set(ref lowAlarm, value); IsLowAlarm = Value?.AsDouble.CompareTo(LowAlarm) == -1; } }
        public bool IsHighAlarm { get => isHighAlarm; private set => Set(ref isHighAlarm, value); } 
        public bool IsLowAlarm { get => isLowAlarm; private set => Set(ref isLowAlarm, value); }
        public string ProductName { get; set; }

        public void InitValue(Thermo.Datapool.Datapool.ITagInfo value)
        {
            Value = value;
            Value.UpdateValueEvent += Value_UpdateValueEvent;
        }

        private void Value_UpdateValueEvent(Thermo.Datapool.Datapool.ITagInfo e)
        {
            Value = e;
        }

        public void SetValue(Thermo.Datapool.Datapool.ITagInfo value)
        {
            Value = value;
        }
        public void SetName(string name)
        {
            Name = name;
        }

        public void Test()
        {
            
        }
    }
}
