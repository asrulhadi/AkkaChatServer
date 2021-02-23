using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;

namespace Teknomatrik.SysMan.Models
{
    public enum HostStatus
    {
        AVAILABLE,
        NOT_AVAILABLE,
        DISCONNECTED
    }

    public class Host
    {
        public string HostName { get; set; }
        public string Type { get; set; }
        [Reactive] public HostStatus Status { get; set; }
        public ObservableCollection<Process> Processes { get; set; }

        public Station Station;

        public string IPAddress;
    }
}
