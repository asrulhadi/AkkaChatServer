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
            var configStr = @"
akka {  
    actor {
        provider = remote
    }
    remote {
        dot-netty.tcp {
            port = XXXX
            hostname = 0.0.0.0
            public-hostname = 10.30.12.13
        }
        retry-gate-closed-for = 2 s
    }
    log-dead-letters = 1
    log-dead-letters-during-shutdown = off
}";
            var port = args.Length > 0 ? args[0] : "8081";
            var config = ConfigurationFactory.ParseString(configStr.Replace("XXXX", port));
            System.Console.WriteLine($"CTRL+C to stop..");
            do
            {
                Console.WriteLine($"Starting new actor system");
                using var system = ActorSystem.Create("MyServer", config);
                system.ActorOf(Props.Create(() => new ChatServerActor()), "ChatServer");
                system.WhenTerminated.Wait();
                Console.WriteLine($"... actor system terminated");
            } while (true);
        }
    }
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
}

