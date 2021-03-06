using Impinj.OctaneSdk;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BobRfid
{
    public class TagStats : INotifyPropertyChanged
    {
        private int _count;

        public int Count
        {
            get
            {
                return _count;
            }
            set
            {
                _count = value;
                OnPropertyChanged();
            }
        }

        public string Epc
        {
            get
            {
                return LastReport?.Epc.ToHexString();
            }
        }

        public Tag LastReport { get; internal set; }
        public DateTime LapStartTime { get; internal set; }
        public DateTime TimeStamp { get; internal set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
