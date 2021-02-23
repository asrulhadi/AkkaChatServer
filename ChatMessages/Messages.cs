//-----------------------------------------------------------------------
// <copyright file="Messages.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2020 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2020 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Actor;
using System.Collections.Generic;
using Teknomatrik.SysMan.Models;

namespace Teknomatrik.SysMan.Messages
{
    /* original - Not Used
    public class ConnectRequest
    {
        public string Username { get; set; }
    }

    public class ConnectResponse
    {
        public string Message { get; set; }
    }

    public class NickRequest
    {
        public string OldUsername { get; set; }
        public string NewUsername { get; set; }
    }

    public class NickResponse
    {
        public string OldUsername { get; set; }
        public string NewUsername { get; set; }
    }

    public class SayRequest
    {
        public string Username { get; set; }
        public string Text { get; set; }
    }

    public class SayResponse
    {
        public string Username { get; set; }
        public string Text { get; set; }
    }
    */
    public class StatusRequest
    {
        public string StationName { get; set; }
    }

    public class StatusResponse
    {
        public string StationName { get; set; }
        public StationStatus StationStatus { get; set; }
    }

    public class HostRequest
    {
        public string StationName { get; set; }
    }

    public class HostResponse
    {
        public string StationName { get; set; }
        public List<Host> StationHosts { get; set; }
    }

    public class ProcessRequest
    {
        public string HostName { get; set; }
    }

    public class ProcessResponse
    {
        public string HostName { get; set; }
        public List<Process> HostProcesses { get; set; }
    }

    public class StationRequest
    {
        public StationRequest() { }
    }

    public class StationResponse
    {
        //public List<Station> StationList { get; set; }
        public Station Station { get; set; }
    }
}
