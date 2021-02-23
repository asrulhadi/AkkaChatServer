using ReactiveUI.Fody.Helpers;

namespace Teknomatrik.SysMan.Models
{
    public enum ProcessStatus
    {
        RUNNING,
        NOT_RUNNING
    }

    public class Process
    {
        public string ProcessName { get; set; }
        [Reactive] public ProcessStatus Status { get; set; }

        public Host Host;

        public string HostName => Host.HostName;
    }
}
