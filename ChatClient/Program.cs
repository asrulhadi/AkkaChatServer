//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2020 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2020 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using Akka.Remote;
using ChatMessages;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChatClient
{
    class Program
    {
        ProbeActor p;
        public Program(IPEndPoint ip, string user = "ProbeClient")
        {
            p = new ProbeActor(ip) { Name = user };
            this.WhenAnyValue(t => t.p.Connected)
                .Subscribe(b => Console.WriteLine($"From VM Connection {ip.Address} {ip.Port} = {b}"));
        }
        public Task RunMe()
        {
            p.CheckAgent();
            //p.StartSendProbe(); <- automatically send if connected
            //p.WaitForTermination();
            return p.WhenTerminated;
            //p.sendProbe.Cancel();
            //Console.Write("Continue with new actor system: ");
        }
        static void Main(string[] args)
        {
            Dictionary<int, Task> tasks = new Dictionary<int, Task>();
            do
            {
                // execute client
                foreach (var port in new int[]{ 8081, 8082 })
                {
                    if (!tasks.ContainsKey(port))
                    {
                        IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse("10.30.12.13"),port);
                        tasks[port] = (new Program(endpoint, args.Length < 1 ? "dev2" : args[0])).RunMe();
                    }
                }
                // wait for one to terminate
                var tt = tasks.Values.ToArray();
                var i = Task.WaitAny(tt);
                // remove the one terminate
                foreach (var port in tasks.Keys)
                {
                    if (tasks[port] == tt[i])
                    {
                        tasks.Remove(port);
                        Console.WriteLine($"Restarting {port}");
                    }
                }
            } while (true/*Console.ReadKey(true).Key == ConsoleKey.Y*/);
        }

    }

    class ConnectionSupervisor : ReceiveActor, ILogReceive
    {
        string user;
        IActorRef chatClient = null;
        public ConnectionSupervisor(string _user)
        {
            user = _user;
            CreateNewClient();
            Receive<string>(_ => StartReading());
            Receive<Terminated>(_ => Console.WriteLine("Network Terminated"));
            Receive<AssociatedEvent>(e =>
            {
                Console.WriteLine($"Supervisor Associated Event {e.LocalAddress} {e.RemoteAddress}");
                //Console.WriteLine("Calling OnConnected Function");
                //OnConnected();
            });
        }
        private void CreateNewClient()
        {
            // if probeClient previously already exist.
            if (chatClient is object)
            {
                chatClient.GracefulStop(TimeSpan.FromMilliseconds(100)).Wait();
                Context.Unwatch(chatClient);
            }
            // create the new
            chatClient = Context.ActorOf(Props.Create<ProbeClientActor>("10.30.12.13", 8081, user));
            Context.Watch(chatClient);
            // start monitoring the events...
            // Listen for Association
            Context.System.EventStream.Subscribe<AssociatedEvent>(chatClient);

        }
        public void StartReading()
        {
            while (true)
            {
                var input = Console.ReadLine();
                if (input.StartsWith("/"))
                {
                    var parts = input.Split(' ');
                    var cmd = parts[0].ToLowerInvariant();
                    var rest = string.Join(" ", parts.Skip(1));

                    if (cmd == "/nick")
                    {
                        chatClient.Tell(new NickRequest
                        {
                            NewUsername = rest
                        });
                    }
                    if (cmd == "/exit")
                    {
                        Console.WriteLine("exiting");
                        break;
                    }
                    if (cmd == "/recreate")
                    {
                        CreateNewClient();
                    }
                }
                else
                {
                    chatClient.Tell(new SayRequest()
                    {
                        Text = input,
                    });
                }
            }
            Console.WriteLine("Finish Client");
            Context.Stop(chatClient);
            Context.Stop(Self);
            Context.System.Terminate();
        }
    }

    // Summary of connection
    #region Server not ready
    // Di manakah anda?
    // Init Anchor: [akka.tcp://MyServer@10.30.12.13:8081/]
    // ProbeClientActor PreStart
    // Starting to Connect...
    // Not Found Retrying...False
    // [WARN][18/02/21 2:52:00 AM][Thread 0013][akka.tcp://MyClient@localhost:25648/system/endpointManager/reliableEndpointWriter-akka.tcp%3A%2F%2FMyServer%4010.30.12.13%3A8081-1/endpointWriter] AssociationError [akka.tcp://MyClient@localhost:25648] -> akka.tcp://MyServer@10.30.12.13:8081: Error [Association failed with akka.tcp://MyServer@10.30.12.13:8081] []
    // [WARN][18/02/21 2:52:00 AM][Thread 0014][remoting(akka://MyClient)] Tried to associate with unreachable remote address [akka.tcp://MyServer@10.30.12.13:8081]. Address is now gated for 5000 ms, all messages to this address will be delivered to dead letters. Reason: [Association failed with akka.tcp://MyServer@10.30.12.13:8081] Caused by: [System.AggregateException: One or more errors occurred. (No connection could be made because the target machine actively refused it. tcp://MyServer@10.30.12.13:8081)
    //  --->Akka.Remote.Transport.InvalidAssociationException: No connection could be made because the target machine actively refused it.tcp://MyServer@10.30.12.13:8081
    // Not Found Retrying...False
    // [INFO][18/02/21 2:52:00 AM][Thread 0023][akka://MyClient/system/endpointManager/reliableEndpointWriter-akka.tcp%3A%2F%2FMyServer%4010.30.12.13%3A8081-1/endpointWriter] Message [AckIdleCheckTimer] from akka://MyClient/system/endpointManager/reliableEndpointWriter-akka.tcp%3A%2F%2FMyServer%4010.30.12.13%3A8081-1/endpointWriter to akka://MyClient/system/endpointManager/reliableEndpointWriter-akka.tcp%3A%2F%2FMyServer%4010.30.12.13%3A8081-1/endpointWriter was not delivered. [1] dead letters encountered. If this is not an expected behavior then akka://MyClient/system/endpointManager/reliableEndpointWriter-akka.tcp%3A%2F%2FMyServer%4010.30.12.13%3A8081-1/endpointWriter may have terminated unexpectedly. This logging can be turned off or adjusted with configuration settings 'akka.log-dead-letters' and 'akka.log-dead-letters-during-shutdown'.
    // [INFO][18/02/21 2:52:00 AM][Thread 0015][akka.tcp://MyClient@localhost:25648/system/endpointManager/reliableEndpointWriter-akka.tcp%3A%2F%2FMyServer%4010.30.12.13%3A8081-1] Removing receive buffers for [akka.tcp://MyClient@localhost:25648]->[akka.tcp://MyServer@10.30.12.13:8081]
    // Not Found Retrying...False
    // Resolved: akka.tcp://MyServer@10.30.12.13:8081/Name/ChatServer
    // ProbeClientActor PreStart...
    #endregion
    #region Crash
    // Only Gated -> Quarantined?
    // Disaccociated Event akka.tcp://MyClient@localhost:25648 akka.tcp://MyServer@10.30.12.13:8081
    // IActorRef => Akka.Actor.LocalActorRef
    // Remote Ref => [akka://MyClient/user/NetworkSupervisor/$a#1150076048]
    // ChatClient PostStop
    // [WARN][18/02/21 2:56:05 AM][Thread 0012][akka.tcp://MyClient@localhost:25648/system/endpointManager/reliableEndpointWriter-akka.tcp%3A%2F%2FMyServer%4010.30.12.13%3A8081-2] Association with remote system akka.tcp://MyServer@10.30.12.13:8081 has failed; address is now gated for 5000 ms. Reason is: [Akka.Remote.EndpointDisassociatedException: Disassociated
    //   at Akka.Remote.EndpointWriter.PublishAndThrow(Exception reason, LogLevel level, Boolean needToThrow)
    //   ...
    // [ERRO][18/02/21 2:56:05 AM][Thread 0012][akka://MyClient/system/endpointManager/reliableEndpointWriter-akka.tcp%3A%2F%2FMyServer%4010.30.12.13%3A8081-2/endpointWriter] Disassociated
    // Cause: Akka.Remote.EndpointDisassociatedException: Disassociated
    //   at Akka.Remote.EndpointWriter.PublishAndThrow(Exception reason, LogLevel level, Boolean needToThrow)
    //   ...
    // [INFO][18/02/21 2:56:05 AM][Thread 0025][akka://MyClient/user/NetworkSupervisor/$a] Message [DisassociatedEvent] from akka://MyClient/system/endpointManager/reliableEndpointWriter-akka.tcp%3A%2F%2FMyServer%4010.30.12.13%3A8081-2/endpointWriter to akka://MyClient/user/NetworkSupervisor/$a was not delivered. [2] dead letters encountered. If this is not an expected behavior then akka://MyClient/user/NetworkSupervisor/$a may have terminated unexpectedly. This logging can be turned off or adjusted with configuration settings 'akka.log-dead-letters' and 'akka.log-dead-letters-during-shutdown'.
    // [WARN][18/02/21 2:56:12 AM][Thread 0015][akka.tcp://MyClient@localhost:25648/system/endpointManager/reliableEndpointWriter-akka.tcp%3A%2F%2FMyServer%4010.30.12.13%3A8081-2/endpointWriter] AssociationError [akka.tcp://MyClient@localhost:25648] -> akka.tcp://MyServer@10.30.12.13:8081: Error [Association failed with akka.tcp://MyServer@10.30.12.13:8081] []
    // [WARN][18/02/21 2:56:12 AM][Thread 0012][remoting(akka://MyClient)] Tried to associate with unreachable remote address [akka.tcp://MyServer@10.30.12.13:8081]. Address is now gated for 5000 ms, all messages to this address will be delivered to dead letters. Reason: [Association failed with akka.tcp://MyServer@10.30.12.13:8081] Caused by: [System.AggregateException: One or more errors occurred. (No connection could be made because the target machine actively refused it. tcp://MyServer@10.30.12.13:8081)
    // --->Akka.Remote.Transport.InvalidAssociationException: No connection could be made because the target machine actively refused it.tcp://MyServer@10.30.12.13:8081
    //   ...
    // [INFO][18/02/21 2:56:12 AM][Thread 0014][akka.tcp://MyClient@localhost:25648/system/endpointManager/reliableEndpointWriter-akka.tcp%3A%2F%2FMyServer%4010.30.12.13%3A8081-2] Removing receive buffers for [akka.tcp://MyClient@localhost:25648]->[akka.tcp://MyServer@10.30.12.13:8081]
    #endregion
    #region NetworkDown
    // [WARN][18/02/21 3:43:20 AM][Thread 0015][akka.tcp://MyClient@localhost:25898/system/remote-watcher] Detected unreachable: [akka.tcp://MyServer@10.30.12.13:8081]
    // Terminated: <Terminated>: [akka.tcp://MyServer@10.30.12.13:8081/user/ChatServer#314314286] - ExistenceConfirmed=True
    // [WARN][18/02/21 3:43:20 AM][Thread 0012][remoting(akka://MyClient)] Association to [akka.tcp://MyServer@10.30.12.13:8081] having UID [1044321879] is irrecoverably failed. UID is now quarantined and all messages to this UID will be delivered to dead letters. Remote actorsystem must be restarted to recover from this situation.
    // ++++Quarantined akka.tcp://MyServer@10.30.12.13:8081
    // ChatClient PostStop
    // [INFO][18/02/21 3:43:20 AM][Thread 0012][akka.tcp://MyClient@localhost:25898/system/endpointManager/reliableEndpointWriter-akka.tcp%3A%2F%2FMyServer%4010.30.12.13%3A8081-1] Removing receive buffers for [akka.tcp://MyClient@localhost:25898]->[akka.tcp://MyServer@10.30.12.13:8081]
    // [INFO][18/02/21 3:43:20 AM][Thread 0010][akka://MyClient/user/NetworkSupervisor/$a] Message [DisassociatedEvent] from akka://MyClient/system/endpointManager/reliableEndpointWriter-akka.tcp%3A%2F%2FMyServer%4010.30.12.13%3A8081-1/endpointWriter to akka://MyClient/user/NetworkSupervisor/$a was not delivered. [1] dead letters encountered. If this is not an expected behavior then akka://MyClient/user/NetworkSupervisor/$a may have terminated unexpectedly. This logging can be turned off or adjusted with configuration settings 'akka.log-dead-letters' and 'akka.log-dead-letters-during-shutdown'.
    // [ERRO][18/02/21 3:43:35 AM][Thread 0012][akka.tcp://MyClient@localhost:25898/system/transports/akkaprotocolmanager.tcp.0/akkaProtocol-tcp%3A%2F%2FMyServer%4010.30.12.13%3A8081-2] No response from remote for outbound association. Associate timed out after [15000 ms].
    // [INFO][18/02/21 3:43:35 AM][Thread 0011][akka://MyClient/system/transports/akkaprotocolmanager.tcp.0/akkaProtocol-tcp%3A%2F%2FMyServer%4010.30.12.13%3A8081-2] Message [Failure] without sender to akka://MyClient/system/transports/akkaprotocolmanager.tcp.0/akkaProtocol-tcp%3A%2F%2FMyServer%4010.30.12.13%3A8081-2 was not delivered. [2] dead letters encountered. If this is not an expected behavior then akka://MyClient/system/transports/akkaprotocolmanager.tcp.0/akkaProtocol-tcp%3A%2F%2FMyServer%4010.30.12.13%3A8081-2 may have terminated unexpectedly. This logging can be turned off or adjusted with configuration settings 'akka.log-dead-letters' and 'akka.log-dead-letters-during-shutdown'.
    // [INFO][18/02/21 3:43:35 AM][Thread 0011][akka://MyClient/user/NetworkSupervisor/$a] Message [AssociationErrorEvent] from akka://MyClient/system/endpointManager/reliableEndpointWriter-akka.tcp%3A%2F%2FMyServer%4010.30.12.13%3A8081-2/endpointWriter to akka://MyClient/user/NetworkSupervisor/$a was not delivered. [3] dead letters encountered. If this is not an expected behavior then akka://MyClient/user/NetworkSupervisor/$a may have terminated unexpectedly. This logging can be turned off or adjusted with configuration settings 'akka.log-dead-letters' and 'akka.log-dead-letters-during-shutdown'.
    // [WARN][18/02/21 3:43:35 AM][Thread 0013][akka.tcp://MyClient@localhost:25898/system/endpointManager/reliableEndpointWriter-akka.tcp%3A%2F%2FMyServer%4010.30.12.13%3A8081-2/endpointWriter] AssociationError [akka.tcp://MyClient@localhost:25898] -> akka.tcp://MyServer@10.30.12.13:8081: Error [Association failed with akka.tcp://MyServer@10.30.12.13:8081] []
    // [INFO][18/02/21 3:43:35 AM][Thread 0012][remoting(akka://MyClient)] Quarantined address [akka.tcp://MyServer@10.30.12.13:8081] is still unreachable or has not been restarted. Keeping it quarantined.
    #endregion
}

