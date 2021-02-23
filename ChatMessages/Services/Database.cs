using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Teknomatrik.SysMan.Models;

namespace Teknomatrik.SysMan.Services
{
    //dummy data for ui test purposes
    public class Database
    {
        public ObservableCollection<Station> Stations { get; }

        public Database()
        {
            int i = 0;
            int a = 0;

            Stations = GetStations();

            foreach(var st in Stations)
            {
                st.Hosts = GetHosts(i, st);
                foreach(var host in st.Hosts)
                {
                    host.Processes = GetProcesses(a, host);
                    foreach(var proc in host.Processes)
                    {
                        a++;
                    }
                    i++;
                }
            }
        }

        public ObservableCollection<Station> GetStations() => new ObservableCollection<Station>
        {
            new Station { StationName = "INSTRUCTOR1",  Status = StationStatus.HALF_READY  },
            new Station { StationName = "INSTRUCTOR2",  Status = StationStatus.FULL_READY  },
            new Station { StationName = "DEBRIEF",      Status = StationStatus.FULL_READY  },
            new Station { StationName = "MAINBRIDGE",   Status = StationStatus.HALF_READY  },
            new Station { StationName = "AUXBRIDGE",    Status = StationStatus.HALF_READY  },
            new Station { StationName = "CUBICLE1",     Status = StationStatus.FAILED      },
            new Station { StationName = "CUBICLE2",     Status = StationStatus.HALF_READY  },
            new Station { StationName = "CUBICLE3",     Status = StationStatus.HALF_READY  },
            new Station { StationName = "CUBICLE4",     Status = StationStatus.FAILED      }
        };

        public ObservableCollection<Host> GetHosts(int i, Station st)
        {
            ObservableCollection<Host> hst = new ObservableCollection<Host>();

            for (int a = 0; a < 3; a++)
                hst.Add(new Host { HostName = "Host " + (i + a), Status = HostStatus.AVAILABLE, Station = st });

            return hst;
        }

        public ObservableCollection<Process> GetProcesses(int i, Host h)
        {
            ObservableCollection<Process> pr = new ObservableCollection<Process>();

            for(int a = 0; a < 5; a++)
                pr.Add(new Process { ProcessName = "Process " + (i + a), Status = ProcessStatus.NOT_RUNNING, Host = h });

            return pr;
        }
    }
}
