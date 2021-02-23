using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using ReactiveUI.Fody.Helpers;

namespace Teknomatrik.SysMan.Models
{
    public enum StationStatus
    {
        FAILED = -1,
        HALF_READY,
        FULL_READY,
        NO_CONNECTION
    }

    public class Station
    { 
        public string StationName { get; set; }
        [Reactive] public StationStatus Status { get; set; }
        public ObservableCollection<Host> Hosts { get; set; }

        public string IPAddress;
    }
}
