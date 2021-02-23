using Akka.Actor;
using Akka.Event;
using Akka.Remote;
using Akka.Configuration;
using ChatMessages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI.Fody.Helpers;
using ReactiveUI;
using System.Net;

namespace ChatClient
{
    public class ProbeActor : ReactiveObject
    {
        protected Config config = ConfigurationFactory.ParseString(@"
akka {  
    loglevel = ""ERROR""    
    log-dead-letters = 1
    log-dead-letters-during-shutdown = off
    actor {
        provider = remote
    }
    remote {
        dot-netty.tcp {
		    port = 0
		    hostname = localhost
        }
        log-remote-lifecycle-events = off
        transport-failure-detector {
            acceptable-heartbeat-pause = 60 s
        }
    }
}
");
        
        public string Name = "ProbeClient";

        protected IPEndPoint ips;
        protected IActorRef probeClient;
        protected ActorSystem actorSystem;
        protected CancellationTokenSource sendProbe = new CancellationTokenSource();
        
        [Reactive] public bool Connected { get; set; } = false;
        public ProbeActor(IPEndPoint endPoint, string _user = "ProbeClient")
        {
            ips = endPoint;
            Name = _user;
            //Console.WriteLine($"*************** Starting new Client ****************");
            actorSystem = ActorSystem.Create("MyClient", config);

            this.WhenAnyValue(t => t.Connected)
                .Subscribe(c => 
                Console.WriteLine($"Connection {ips.Address} {ips.Port} = {c}")
                );
        }
        public void CheckAgent()
        {
            var chatClientProp = Props.Create<ProbeClientActor>(ips.Address.ToString(), ips.Port, Name, this);
            probeClient = actorSystem.ActorOf(chatClientProp, "client");
        }
        protected void StartSendProbe()
        {
            Task.Run(() =>
            {
                int count = 1;
                Console.WriteLine("*** Start of sending probe");
                do
                {
                    probeClient.Tell(new SayRequest()
                    {
                        Text = $"Probe no {count}",
                    });
                    Console.Write(".");
                    count++;
                    Thread.Sleep(1000);
                } while (!sendProbe.IsCancellationRequested);
                probeClient.GracefulStop(TimeSpan.FromMilliseconds(100))
                    .ContinueWith(r => Console.WriteLine($"Client stop with {r.Result}"));
                Console.WriteLine("*** End of sending probe");
                // stopping Actor System
                actorSystem.Terminate();
            }
            , sendProbe.Token
            );
        }
        public void WaitForTermination()
        {
            //Console.WriteLine($"Uptime: {actorSystem.Uptime}");
            //Console.WriteLine("*** Wating for termination");
            actorSystem.WhenTerminated.Wait();
            //Console.WriteLine("*** Actor System Terminated");
            // Stop sending probe as we should terminate
            sendProbe.Cancel();
        }
        public Task WhenTerminated => actorSystem.WhenTerminated;
    }

    // user actor
    class ProbeClientActor : ReceiveActor, ILogReceive
    {
        private IActorRef serverRef;
        private readonly ActorSelection _server;
        public bool Connected { get; set; } = false;
        ProbeActor prog;

        public ProbeClientActor(string ip = "10.30.12.13", int port = 8081, string user = "localuser", ProbeActor _prog = null)
        {
            prog = _prog;
            _server = Context.ActorSelection($"akka.tcp://MyServer@{ip}:{port}/user/ChatServer");

            #region Message Processing
            Receive<ConnectRequest>(cr =>
            {
                _server.Tell(cr);
            });

            Receive<ConnectResponse>(rsp =>
            {
            });

            Receive<NickResponse>(nrsp =>
            {
                Console.WriteLine("{0} is now known as {1}", nrsp.OldUsername, nrsp.NewUsername);
            });

            Receive<SayRequest>(sr =>
            {
                sr.Username = "User";
                _server.Tell(sr);
            });

            Receive<SayResponse>(srsp =>
            {
                //Console.WriteLine("{0}: {1}", srsp.Username, srsp.Text);
            });
            #endregion

            Receive<Terminated>(t =>
            {
                prog.Connected = false;
                //Console.WriteLine("Terminated: " + t.ToString());
                Context.Stop(serverRef);
                Connected = false;
                Context.System.Terminate();
            });

            // Listen to Association Error
            Receive<AssociationErrorEvent>(e =>
            {
                Console.WriteLine($"Association Error {e.LocalAddress}. Restarting {Connected}");
                if (Connected) (Self as LocalActorRef).Restart(new Exception());
            });
            // Listen for Disassociated
            Receive<DisassociatedEvent>(e =>
            {
                prog.Connected = false;
                Console.WriteLine($"Disaccociated Event {e.LocalAddress} {e.RemoteAddress}");
                //LocalActorRef localActorRef = Self as LocalActorRef;
                Context.Unwatch(serverRef);
                Context.System.EventStream.Unsubscribe<DisassociatedEvent>(Self);
                Context.System.EventStream.Unsubscribe<AssociationErrorEvent>(Self);
                Self.GracefulStop(TimeSpan.FromMilliseconds(1));
                Context.System.Terminate();
            });
            // Listen for Association
            Context.System.EventStream.Subscribe<AssociatedEvent>(Self);
            Receive<AssociatedEvent>(e =>
            {
                //Console.WriteLine($"Associated Event {e.LocalAddress} {e.RemoteAddress}");
                //Console.WriteLine("Calling OnConnected Function");
                Context.System.EventStream.Subscribe<DisassociatedEvent>(Self);
                Context.System.EventStream.Subscribe<AssociationErrorEvent>(Self);
                prog.Connected = true;
            });

            // Quarantined
            Context.System.EventStream.Subscribe<QuarantinedEvent>(Self);
            Receive<QuarantinedEvent>(q =>
            {
                //Console.WriteLine($"++++Quarantined {q.Address}");
                Context.System.Stop(Self);
            });
            Receive<RemotingLifecycleEvent>(r =>
            {
                Console.WriteLine($"****RemotingLifecycleEvent: {r}");
            });

        }
        protected override void PreStart()
        {
            base.PreStart();
            // try connect to probeServer. actor client is started after succesfully connected
            try
            {
                Task<IActorRef> tt;
                Console.WriteLine($"Start connection to server");
                do
                {
                    try
                    {
                        tt = _server.ResolveOne(TimeSpan.FromSeconds(1));
                        tt.Wait();
                        prog.Connected = Connected = true;
                        serverRef = tt.Result;
                        Context.Watch(serverRef);
                    }
                    catch (AggregateException e)
                    {
                        if (e.InnerExceptions[0] is ActorNotFoundException err)
                        {
                            //Console.WriteLine($"Not Found Retrying...{Connected}");
                        }
                        else
                        {
                            Console.WriteLine($"AggregateException {Connected} {e}");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Exception: {Connected} {e}");
                    }
                    Thread.Sleep(1000);
                } while (!Connected);
            }
            catch (Exception err)
            {
                Console.WriteLine("Resolved err: " + err);
            }
        }
        protected override void PostRestart(Exception reason)
        {
            base.PostRestart(reason);
            Console.WriteLine("ChatClient PostRestart");
        }

        protected override void PostStop()
        {
            base.PostStop();
            Console.WriteLine("ChatClient PostStop");
        }
    }

}
