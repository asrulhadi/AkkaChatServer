//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2020 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2020 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using Akka.Remote;
using Teknomatrik.Utilities;
using Teknomatrik.SysMan.Models;
using Teknomatrik.SysMan.Messages;

// <summary>
// apa yang berlaku
#region Normal
// -> normal... tak dak masalah
// ++++Server Terminated: Removing Sender akka.tcp://MyClient@localhost:25293/user/NetworkSupervisor/$a True
// ++++Server Terminated: Sender akka.tcp://MyClient@localhost:25293/user/NetworkSupervisor/$a
// ++++Disassociated Event Disassociated [akka.tcp://MyServer@10.30.12.13:8081] <- akka.tcp://MyClient@localhost:25293
// [INFO][2/18/2021 1:32:01 AM][Thread 0013][akka.tcp://MyServer@10.30.12.13:8081/system/endpointManager/reliableEndpointWriter-akka.tcp%3A%2F%2FMyClient%40localhost%3A25293-1] Removing receive buffers for [akka.tcp://MyServer@10.30.12.13:8081]->[akka.tcp://MyClient@localhost:25293]
#endregion
#region crash
// 1. crash .. client disconnected..
// [INFO][2/18/2021 1:43:12 AM][Thread 0017][TcpServerHandler (akka://MyServer)] Connection was reset by the remote peer. Channel [[::ffff:10.30.12.13]:8081->[::ffff:10.30.12.180]:25328](Id=175399ef)
// ++++Disassociated Event Disassociated [akka.tcp://MyServer@10.30.12.13:8081] <- akka.tcp://MyClient@localhost:25327
// [INFO][2/18/2021 1:43:12 AM][Thread 0012][akka.tcp://MyServer@10.30.12.13:8081/system/endpointManager/reliableEndpointWriter-akka.tcp%3A%2F%2FMyClient%40localhost%3A25327-4] Removing receive buffers for [akka.tcp://MyServer@10.30.12.13:8081]->[akka.tcp://MyClient@localhost:25327]
// ++++AssociationError: akka.tcp://MyServer@10.30.12.13:8081->akka.tcp://MyClient@localhost:25327 Association failed with akka.tcp://MyClient@localhost:25327
// ++++AssociationError Sender: akka://MyServer/system/endpointManager/reliableEndpointWriter-akka.tcp%3A%2F%2FMyClient%40localhost%3A25327-6/endpointWriter
// ++++Found client matching remote address: [akka.tcp://MyClient@localhost:25327/user/NetworkSupervisor/$a#1681705725]. Terminating it
// [WARNING][2/18/2021 1:43:14 AM][Thread 0015][akka.tcp://MyServer@10.30.12.13:8081/system/endpointManager/reliableEndpointWriter-akka.tcp%3A%2F%2FMyClient%40localhost%3A25327-6/endpointWriter] AssociationError [akka.tcp://MyServer@10.30.12.13:8081] -> akka.tcp://MyClient@localhost:25327: Error [Association failed with akka.tcp://MyClient@localhost:25327] []
// [WARNING][2/18/2021 1:43:14 AM][Thread 0012][remoting (akka://MyServer)] Tried to associate with unreachable remote address [akka.tcp://MyClient@localhost:25327]. Address is now gated for 2000 ms, all messages to this address will be delivered to dead letters. Reason: [Association failed with akka.tcp://MyClient@localhost:25327] Caused by: [System.AggregateException: One or more errors occurred. (Connection refused tcp://MyClient@localhost:25327)
//  ---> Akka.Remote.Transport.InvalidAssociationException: Connection refused tcp://MyClient@localhost:25327
//  ...
// ++++Disassociated Event Disassociated [akka.tcp://MyServer@10.30.12.13:8081] -> akka.tcp://MyClient@localhost:25327
// [INFO][2/18/2021 1:43:14 AM][Thread 0012][akka.tcp://MyServer@10.30.12.13:8081/system/endpointManager/reliableEndpointWriter-akka.tcp%3A%2F%2FMyClient%40localhost%3A25327-6] Removing receive buffers for [akka.tcp://MyServer@10.30.12.13:8081]->[akka.tcp://MyClient@localhost:25327]
#endregion 
#region NetworkDown
// [WARNING][2/18/2021 1:50:05 AM][Thread 0012][akka.tcp://MyServer@10.30.12.13:8081/system/remote-watcher] Detected unreachable: [akka.tcp://MyClient@localhost:25344]
// [WARNING][2/18/2021 1:50:05 AM][Thread 0013][remoting (akka://MyServer)] Association to [akka.tcp://MyClient@localhost:25344] having UID [1384752871] is irrecoverably failed. UID is now quarantined and all messages to this UID will be delivered to dead letters. Remote actorsystem must be restarted to recover from this situation.
// ++++Server Terminated: Removing Sender akka.tcp://MyClient@localhost:25344/user/NetworkSupervisor/$a True
// ++++Server Terminated: Sender akka.tcp://MyClient@localhost:25344/user/NetworkSupervisor/$a
// ++++Disassociated Event Disassociated [akka.tcp://MyServer@10.30.12.13:8081] <- akka.tcp://MyClient@localhost:25344
// [INFO][2/18/2021 1:50:05 AM][Thread 0013][akka.tcp://MyServer@10.30.12.13:8081/system/endpointManager/reliableEndpointWriter-akka.tcp%3A%2F%2FMyClient%40localhost%3A25344-1] Removing receive buffers for [akka.tcp://MyServer@10.30.12.13:8081]->[akka.tcp://MyClient@localhost:25344]
#endregion

namespace Teknomatrik.SysMan
{
    class Program
    {
        static void Main(string[] args)
        {
            Config config;
            string defaultConfig = @"
            akka {  
                actor {
                    provider = remote
                }
                remote {
                    dot-netty.tcp {
                        port = _PORT_
                        hostname = 0.0.0.0
                        public-hostname = _PUBLIC_HOSTNAME_
                    }
                    retry-gate-closed-for = 2 s
                }
                log-dead-letters = 1
                log-dead-letters-during-shutdown = off
            }";
            string servername = args.Length > 0 ? args[0] : "server";
            IniFile inifile = new IniFile($"{servername}.ini");

            string ip_address = inifile.GetValue("network", "ip_address", "10.30.12.13");
            string hostname = inifile.GetValue("network", "hostname", "0.0.0.0");
            string port = inifile.GetValue("network", "port", "8081");    //args.Length > 0 ? args[0] : "8081";
            string name = inifile.GetValue("general", "name", "instructor1").ToUpper();

            config = ConfigurationFactory.ParseString(defaultConfig.Replace("_PUBLIC_HOSTNAME_","10.30.12.13").Replace("_PORT_", port));

            #region OldCode
            // System.Console.WriteLine($"CTRL+C to stop..");
            // do
            // {
            //     Console.WriteLine($"Starting new actor system");
            //     using var system = ActorSystem.Create("MyServer", config);
            //     system.ActorOf(Props.Create(() => new ChatServerActor()), "ChatServer");
            //     system.WhenTerminated.Wait();
            //     Console.WriteLine($"... actor system terminated");
            // } while (true);                            
            #endregion

            #region NewCode
            Console.Title = $"{name} Port {port} ";
            string filepath = string.Empty;

            //StationServerActor.station = stations.Find(st => st.StationName == args[1].ToUpperInvariant());
            //string filepath = $"{args[1].ToUpperInvariant()}.txt";
            filepath = @$"data\{name}.data";

            Console.WriteLine(config.ToString());
            //Console.WriteLine(StationServerActor.station.StationName);

            using (var system = ActorSystem.Create("MyServer", config))
            {
                system.ActorOf(Props.Create(() => new StationServerActor(filepath)), name);
                //Console.WriteLine(system);
                //Console.WriteLine(serveractor);
                Console.ReadLine();
            }
            #endregion
        } // should be main
    }
    #region OldActorCode
    class ServerSupervisor : ReceiveActor, ILogReceive
    {
        IActorRef server;
        public ServerSupervisor()
        {
            server = Context.ActorOf(Props.Create(() => new ChatServerActor()), "ChatServer");
            Context.Watch(server);
            Receive<Terminated>(ex => 
            {
                System.Console.WriteLine($"Supervisor: Terminated: {ex}");
            });

            // subscribe to events
            Context.System.EventStream.Subscribe<AssociatedEvent>(Self);
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            //return base.SupervisorStrategy();
            return new OneForOneStrategy(
                1,
                TimeSpan.FromSeconds(1),
                x => 
                {
                    return Directive.Restart;
                }
            );
        }
    }

    class ChatServerActor : ReceiveActor, ILogReceive
    {
        private readonly HashSet<IActorRef> _clients = new HashSet<IActorRef>();

        public ChatServerActor()
        {
            #region OriginalChatMessage
            /* Not Used Anymore    
            Receive<SayRequest>(message =>
            {
                var response = new SayResponse
                {
                    Username = message.Username,
                    Text = message.Text,
                };
                Console.WriteLine($"++++Received from <{message.Username}>@{Sender.Path}: {message.Text}");
                foreach (var client in _clients) client.Tell(response, Self);
            });

            Receive<ConnectRequest>(message =>
            {
                _clients.Add(Sender);
                Context.Watch(Sender);
                //Context.System.EventStream.Subscribe<Disassociated>(Sender);
                //Context.System.EventStream.Subscribe<AssociationErrorEvent>(Sender);
                Sender.Tell(new ConnectResponse
                {
                    Message = "Hello and welcome to Akka.NET chat example",
                }, Self);
                Console.WriteLine($"++++Client <{message.Username}> connected. {Sender.Path}");
            });

            Receive<NickRequest>(message =>
            {
                var response = new NickResponse
                {
                    OldUsername = message.OldUsername,
                    NewUsername = message.NewUsername,
                };

                foreach (var client in _clients) client.Tell(response, Self);
            });
            */
            #endregion

            Receive<Terminated>(t => 
            {
                Console.WriteLine($"++++Server Terminated: Removing Sender {t.ActorRef.Path} {t.ExistenceConfirmed}");
                Console.WriteLine($"++++Server Terminated: Sender {Sender.Path}");
                //Sender.Tell(Kill.Instance);
                _clients.Remove(Sender);
                Context.Unwatch(Sender);
                //Context.Stop(Sender);
            });
            Receive<UnhandledMessage>(m => 
            {
                System.Console.WriteLine($"Unhandled: {m}");
            });

            Receive<DisassociatedEvent>(d => 
            {
                System.Console.WriteLine($"++++Disassociated Event {d}");
            });
            Receive<AssociationErrorEvent>(e => 
            {
                System.Console.WriteLine($"++++AssociationError: {e.LocalAddress}->{e.RemoteAddress} {e.Cause.Message}");
                System.Console.WriteLine($"++++AssociationError Sender: {Sender.Path}");
                // Search for remote address in the cients list
                foreach (var client in _clients)
                {
                    if (e.RemoteAddress.Equals(client.Path.Address))
                    {
                        System.Console.WriteLine($"++++Found client matching remote address: {client}. Terminating it");
                        // avoid retrying - important as to avoid heartbeat
                        Context.Unwatch(client);
                        client.GracefulStop(TimeSpan.FromMilliseconds(1));
                        _clients.Remove(client);
                    }
                }
                //Sender.Tell(Kill.Instance);
                Sender.GracefulStop(TimeSpan.FromMilliseconds(1));
            });

            ReceiveAny(o => 
            {
                System.Console.WriteLine($"Server Receive Any: {o.GetType()} {o}");
            });

            Context.System.EventStream.Subscribe<DisassociatedEvent>(Self);
            Context.System.EventStream.Subscribe<AssociationErrorEvent>(Self);

        }

        protected override void PreStart()
        {
            base.PreStart();
            System.Console.WriteLine($"**** PreStart");
        }

    }
    #endregion

    class StationServerActor : ReceiveActor, ILogReceive, IWithTimers
    {
        //private readonly Database db;
        private readonly HashSet<IActorRef> _clients = new HashSet<IActorRef>();
        public static Station station;
        private readonly string _filepath;

        public ITimerScheduler Timers { get; set; }

        protected override void PreStart()
        {
            //Timers.StartPeriodicTimer("response", new Response(), TimeSpan.FromSeconds(1));
            //Timers.StartPeriodicTimer("response", new StationRequest(), TimeSpan.FromSeconds(1));
            Console.WriteLine($"server created");
        }

        protected override void PostStop()
        {
            //base.PostStop();
            Console.WriteLine("server post stop");
            //foreach (var client in _clients) client.Tell(new Response() { Message = $"Server {_serverName} Stopped" });
        }

        public StationServerActor(string filepath)
        {
            //db = new Database();
            _filepath = filepath;
            /*
            Receive<SayRequest>(message =>
            {
                var response = new SayResponse
                {
                    Username = message.Username,
                    Text = message.Text,
                };
                foreach (var client in _clients) client.Tell(response, Self);
            });

            Receive<ConnectRequest>(message =>
            {
                _clients.Add(Sender);
                Console.WriteLine("Received connect request");
                //Sender.Tell(new ConnectResponse() { Message = $"{Self.Path}| {station.Status}" }, Self);
            });
            */
            Receive<StationRequest>(message =>
            {                
                Sender.Tell(new StationResponse
                {
                    Station = ReadFromFile(_filepath)
                });
            });

            Receive<StatusRequest>(message =>
            {
                StationStatus status = StationStatus.FAILED;
                /*
                foreach (var station in db.Stations)
                {
                    if (station.StationName == message.StationName)
                    {
                        status = station.Status;

                        var response = new StatusResponse
                        {
                            StationName = message.StationName,
                            StationStatus = status
                        };

                        Sender.Tell(response, Self);

                        return;
                    }
                }
                Sender.Tell(new Response { Message = "Station does not exist" });
                */
                Sender.Tell(
                    new StatusResponse
                        {
                            StationName = Self.Path.Name,
                            StationStatus = status
                        });
                Console.WriteLine($"Name: {Self.Path.Name}");
                
            });

            Receive<HostRequest>(message =>
            {
                // foreach (var station in db.Stations)
                // {
                //     if (station.StationName == message.StationName)
                //     {
                //         var response = new HostResponse
                //         {
                //             StationName = message.StationName,
                //             StationHosts = station.Hosts.ToList()
                //         };

                //         Sender.Tell(response, Self);

                //         return;
                //     };
                // }

                Sender.Tell(new Response { Message = "Station does not exist" });
            });

            Receive<ProcessRequest>(message =>
            {
                // foreach (var station in db.Stations)
                // {
                //     foreach (var host in station.Hosts)
                //     {
                //         if (host.HostName == message.HostName)
                //         {
                //             //Console.WriteLine("eee");
                //             var response = new ProcessResponse
                //             {
                //                 HostName = message.HostName,
                //                 HostProcesses = host.Processes.ToList()
                //             };

                //             Sender.Tell(response, Self);

                //             return;
                //         }
                //     }
                // }

                Sender.Tell(new Response { Message = "Host does not exist" });
            });

            //return random status to simulate changing status
            Receive<Response>(resp =>
            {
                var rand = new Random();
                station.Status = (StationStatus)rand.Next(-1, 2);
                foreach(var host in station.Hosts)
                {
                    host.Status = (HostStatus)rand.Next(-1, 3);
                    foreach(var proc in host.Processes)
                        proc.Status = (ProcessStatus)rand.Next(-1, 3);
                }
            });
        }

        Station ReadFromFile(string path)
        {
            Station st = new Station();

/*
            try
            {
                using StreamReader sr = new StreamReader(path);
                //Console.WriteLine("readfromfile");
                while (sr.Peek() >= 0)
                {
                    string line = sr.ReadLine();
                    line = line.Trim();

                    if(line == "/station")
                    {
                        line = sr.ReadLine();
                        st.StationName = line.Split(',')[0].ToUpperInvariant();
                        st.Status = (StationStatus)Enum.Parse(typeof(StationStatus), line.Split(',')[1].ToUpperInvariant());
                        continue;
                    }

                    if (line == "/hosts")
                    {
                        st.Hosts = new ObservableCollection<Host>();

                        while ((char)sr.Peek() != '/')
                        {
                            line = sr.ReadLine();
                            st.Hosts.Add(new Host()
                            {
                                HostName = line.Split(',')[0],
                                Type = line.Split(',')[1],
                                Status = (HostStatus)Enum.Parse(typeof(HostStatus), line.Split(',')[2].ToUpperInvariant()),
                                Station = st
                            });
                        }
                        continue;
                    }

                    if (line == "/processes")
                    {
                        foreach (var h in st.Hosts)
                            h.Processes = new ObservableCollection<Process>();

                        while ((char)sr.Peek() != '/' && sr.Peek() != -1)
                        {
                            line = sr.ReadLine();
                            string hostname = line.Split(',')[0];
                            int i = 0;
                            foreach (Host h in st.Hosts)
                            {
                                if (h.HostName == hostname) break;
                                i++;
                            }

                            st.Hosts[i].Processes.Add(new Process()
                            {
                                Host = st.Hosts[i],
                                ProcessName = line.Split(',')[1],
                                Status = (ProcessStatus)Enum.Parse(typeof(ProcessStatus), line.Split(',')[2].ToUpperInvariant())
                            });
                        }
                        continue;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
            }
*/

            return st;
        }
    }
}

