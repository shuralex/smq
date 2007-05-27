using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;


namespace SimpleMessageQueue
{
    /// <summary>
    /// manages the sending and receiving of Messages between various objects.
    /// This will guarantee disassociation of events between threads if the 
    /// sender and receiver are different threads, unlike event calls.
    /// This disassociation allows for Async events resulting in better performance
    /// of time sensitive threads such as a network interface.
    /// </summary>
    public class MessageQueue
    {
        /// <summary>
        /// inbound messages to be processed.
        /// </summary>
        Queue Messages;
        Queue Outbound;
        /// <summary>
        /// messages that we need to send out upon activity happening
        /// </summary>
        List<Message> RegisteredMessages;

        /// <summary>
        /// returns a list of all registered messages that we have recieved
        /// </summary>
        public List<RegisteredMessage> Registrations 
        {
            get { List<RegisteredMessage> msgs = new List<RegisteredMessage>(RegisteredMessages.Count);
            foreach (Message m in RegisteredMessages)
                msgs.Add((RegisteredMessage)m);
            return msgs;
                } 
        }
        /// <summary>
        /// run until this is tripped.
        /// </summary>
        bool shutdown = false;
        public bool processing = false;
        
        /// <summary>
        /// for queue syncing.
        /// </summary>
        private object SyncRoot;
        /// <summary>
        /// who we know about. Identity is based off of the ToString() value of the receiving object.
        /// We store this string and it's associated (discovered) MQueue object thus avoiding multiple
        /// introspections (which I assume can be slow.)
        /// </summary>
        Dictionary<object, MessageQueue> ValidRecipients;

        /// <summary>
        /// all the registered messages that we sent out.
        /// </summary>
        public List<RegisteredMessage> SentRegisteredMessages;

        // so we know who we are.
        object Identity { get { return identity; } }
        internal object identity;

        /// <summary>
        /// for knowing when a new message has arrived.
        /// </summary>
        System.Threading.Thread LocalThread;

        bool InLocalExec = false;
        public delegate void MessageCallback(Message msg);

        Dictionary<Message.MessageType, List<MessageCallback>> MessageCallbacks;

        public Thread QueueThread { get { return queue_thread; } }
        /// <summary>
        /// Exists only if this simulator is being actively communicated with.
        /// </summary>
        private Thread queue_thread;


        /// <summary>
        /// Handles recieving, sending, and queueing of messages between threads.
        /// </summary>
        /// <param name="sender"></param>
        public MessageQueue(Object sender)
        {
            InitMessageQueue(sender, false);
        }

        /// <summary>
        /// Handles recieving, sending, and queueing of messages between threads.
        /// </summary>
        /// <param name="sender"></param>
        public MessageQueue(Object sender, bool InitThread)
        {
            InitMessageQueue(sender, InitThread);
        }

        /// <summary>
        /// Handles recieving, sending, and queueing of messages between threads.
        /// </summary>
        /// <param name="sender"></param>
        public void InitMessageQueue(Object sender, bool CreateThread)
        {
            Messages = new Queue();
            Outbound = new Queue();
            RegisteredMessages = new List<Message>();
            ValidRecipients = new Dictionary<object, MessageQueue>();
            SentRegisteredMessages = new List<RegisteredMessage>();
            RegisteredMessages = new List<Message>();
            SyncRoot = new object();
            MessageCallbacks = new Dictionary<Message.MessageType, List<MessageCallback>>();
            identity = sender;
            CaptureThread();
            if (CreateThread)
            {
                queue_thread = new System.Threading.Thread(Run);
                queue_thread.Name = "SMQ: " + identity.ToString();
                queue_thread.IsBackground = true;
                queue_thread.Start();
            }
        }

        /// <summary>
        /// Simple loop to run as the entry point of a thread.
        /// This provides a clean method to give a queue and it's object 
        /// it's own thread without the user worying about thread management.
        /// It fully automates the thread setup and execution relieving the user to just ask for it.
        /// </summary>
        public void Run()
        {
            // capture our curent thread so that we know to always queue messages 
            // instead of parsing them on the same thread as the sender
            CaptureThread();
            while (!shutdown)
            {
                //Console.WriteLine("TICK");
                ProcessMessages();
            }
        }

        /// <summary>
        /// please make certain that you shutdown the queue this way so that things are cleaned up.
        /// I need a way to hook into the destructor but i'm not that savvy in C# yet. ;-P
        /// </summary>
        public void Shutdown()
        {
            shutdown = true;
            RevokeRegisteredMessages();
            // now we toss out any messages in the queue.
            while (Messages.Count > 0)
            {
                Message msg;
                lock (SyncRoot)
                {
                     msg = (Message)Messages.Dequeue();
                }
                if (msg != null &&  msg is CommandMessage)
                    Ack((CommandMessage)msg, AckMessage.StatusType.Shutdown);
            }
            // remove our identity from everyone taht's sent us messages so that we are GC'd.
            RemoveCachedEntries();
         //  Console.WriteLine(this.identity.ToString() + " Shutting down.");
        }

        void RemoveCachedEntries()
        {
            foreach (MessageQueue queue in ValidRecipients.Values)
            {
                if (queue.identity != this.identity)
                {
                    lock (queue)
                    {
                        lock (queue.ValidRecipients)
                        {
                            if (queue.ValidRecipients.ContainsKey(identity))
                                queue.ValidRecipients.Remove(identity);
                        }
                    }
                }
            }
        }
        ~MessageQueue()
        {
            RevokeRegisteredMessages();
            Shutdown();
        }
        /// <summary>
        /// This should revoke all registrations that we have sent.
        /// </summary>
        public void RevokeRegisteredMessages()
        {
            foreach (RegisteredMessage msg in SentRegisteredMessages)
            {
                if(msg.recipient != this.identity)
                    Send(new UnRegisterMessage(msg.Data, msg.AckHandler), msg.recipient);
            }
            SentRegisteredMessages.Clear();
        }

        public void CaptureThread()
        {
            LocalThread = System.Threading.Thread.CurrentThread;
        }

        /// <summary>
        /// a method to pre-setup a receiver instead of at-send-time discovery.
        /// we can also pre-set special identities on receivers that we create ourselves.
        /// </summary>
        /// <param name="r"></param>
        public void RegisterReceiver(string ID, MessageQueue r)
        {
            lock (ValidRecipients)
            {
                if (!ValidRecipients.ContainsKey(ID))
                    ValidRecipients.Add(ID, r);
            }
        }

        public void RegisterPacketCallback(Message.MessageType type, MessageCallback callback)
        {
            if (!MessageCallbacks.ContainsKey(type))
                MessageCallbacks.Add(type, new List<MessageCallback>());
            MessageCallbacks[type].Add(callback);
        }

        public void UnregisterPacketCallback(Message.MessageType type, MessageCallback callback)
        {
            if (MessageCallbacks.ContainsKey(type))
                foreach (MessageCallback rcb in MessageCallbacks[type])
                    if (callback == rcb)
                    {
                        MessageCallbacks[type].Remove(callback);
                        return;
                    }
        }

        /// <summary>
        /// sets the sender/recipient of a message
        /// </summary>
        /// <param name="recipient">recieving object.</param>
        /// <returns></returns>
        public bool Recv(Message msg)
        {
                if (msg.type == Message.MessageType.Registered)
                {
                    RegisteredMessages.Add(msg);
                  //  return true;
                }
                else if (msg.type == Message.MessageType.UnRegister)
                {
                    foreach (RegisteredMessage Rmsg in RegisteredMessages)
                    {
                        //shoudl we be also matching up the AckCallback object?
                        if (msg.sender == Rmsg.sender
                            && Rmsg.Matches((object[])msg.Data))
                            RegisteredMessages.Remove(Rmsg);
                    //    return true;
                    }
                }

                // all messages get a chance for processing.
                lock (SyncRoot)
                {
                    Messages.Enqueue(msg);
                    Monitor.Pulse(SyncRoot);
                }                // signal we're done.
                return true;
        }

        /// <summary>
        /// common action of doing a "blocking" send of a command.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public CommandMessage SendAndWait(CommandMessage msg, object recipient)
        {
            if (null == msg.StatusChanged)
            {
                // no matter how the user constructed things, we override since we know better! *grin*
                msg.StatusChanged = new ManualResetEvent(false);
                msg.StatusChanged.Reset();
                msg.WantAck = true; // without this we don't wait.
            }
            Send(msg, recipient);
            msg.StatusChanged.WaitOne();
            return msg;
        }

        /// <summary>
        /// sends the message tot he target queue.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool Send(Message msg)
        {
            //     try
            //   {

            if (null == msg || null == msg.recipient)
                return true; // so that unwanted acks are silently dropped.
            MessageQueue RecipientMQ;
            lock (ValidRecipients)
            {
                if (!ValidRecipients.ContainsKey(msg.recipient))
                {
                    RecipientMQ = HasMQueue(msg.recipient);
                    if (null == RecipientMQ)
                    {
                        Console.WriteLine(ToString() + ": Unknown recipient " + msg.recipient);
                        return false;
                    }
                    ValidRecipients.Add(msg.recipient, (MessageQueue)RecipientMQ);
                }
                else
                    RecipientMQ = ValidRecipients[msg.recipient];
            }
            if (null == msg.sender)
                msg.sender = this.identity;
            //       if (!RecipientMQ.ValidRecipients.ContainsKey(Identity))
            //           RecipientMQ.ValidRecipients.Add(Identity, this);

            // the idea is to prevent infinite recursion, but I think it's impossible 
            // anyays due to the nature of the queues.
            // instead we need to find a way of walkign all queues in the one thread. *sigh*
            if (InLocalExec && (msg.type == Message.MessageType.Command))
            {
                Outbound.Enqueue(msg);
                return true;
            }
            else
            {
                if (msg.type == Message.MessageType.Registered)
                {
                    // save for later cleanup.
                    SentRegisteredMessages.Add((RegisteredMessage)msg);
                }
                return RecipientMQ.Recv(msg);
            }

            //      catch (Exception e)
            //    {
            //      Console.WriteLine(e.ToString());
            //    return false;
            //}
        }

        MessageQueue HasMQueue(object obj)
        {
            // just testing out things via debugger.
            Type objType = obj.GetType();
            System.Reflection.MemberInfo[] membersInfo = objType.GetMembers();
            foreach (System.Reflection.MemberInfo o in membersInfo)
            {
                string[] fullname = o.ToString().Split(new char[] { ' ' });
                if(fullname[0] == "SimpleMessageQueue.MessageQueue")
                {
                    return (MessageQueue)objType.InvokeMember(fullname[1], System.Reflection.BindingFlags.GetField, null, obj, new object[] { });
                }
            }
            return null;
        }

        public bool Send( Message msg, Object Recipient)
        {
            msg.recipient = Recipient;
            return Send(msg);
        }

        public bool MessagesWaiting()
        {
            return Messages.Count > 0;
        }

        /// <summary>
        /// steps through each message.
        /// in a single-thread env with only one call into ProcessMessages 
        /// we really should look at all our potential recipients to see 
        /// if any are on the local thread and walk them as well while we are in here.
        /// this way, just one call into a queue will trigger all queues to process,
        /// thus eliminating multiple queue calls on the work loop.
        /// </summary>
        /// <returns></returns>
        public bool ProcessMessages()
        {
            // clear the waiting signal. That way, 
            // if we get one while processing this,
            // or near the end we won't miss it.
            lock (SyncRoot)
            {
                while (Messages.Count == 0 && !shutdown)
                {
                    if (shutdown)
                    {
                        // tell anyone else to go ahead and finish up.
                        Monitor.PulseAll(SyncRoot);
                        return false;
                    }
                    try
                    {
                        if (!Monitor.Wait(SyncRoot))
                            throw new TimeoutException();
                        //Monitor.PulseAll(SyncWrite);
                    }
                    catch(Exception except)
                    {
                        Console.WriteLine(except.ToString());
                        Monitor.PulseAll(SyncRoot);
                        throw;
                    }
                }
            }
            while (Messages.Count > 0)
            {
                Message msg = null;
                processing = true;
                lock (SyncRoot)
                    msg = (Message)Messages.Dequeue();
                if (msg != null)
                {
                    // ack and bounce messages get passed off to the AckHandler that is set within the CommandMessage
                    if (msg.type == Message.MessageType.Ack
                        || msg.type == Message.MessageType.Bounce)
                    {
                        if (((AckMessage)msg).Command != null
                        && ((AckMessage)msg).Command.AckHandler != null)
                        {
                            string sendername = msg.sender.ToString();
                            if (sendername.Contains("SLClient"))
                            {
                                int x = 1;
                                x += 3;
                            }
                            ((AckMessage)msg).Command.AckHandler((AckMessage)msg);
                        }
                    }
                    if (MessageCallbacks.ContainsKey(msg.type))
                    {
                        foreach (MessageCallback c in MessageCallbacks[msg.type])
                        {

                            try
                            {
                                c(msg);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.ToString());
                            }
                        }
                    }
                }
                processing = false;
            }            
            return true;
        }

        public bool MatchRegistered(object MatchArg, AckMessage.StatusType status)
        {
            return MatchRegistered(MatchArg, status, null);
        }
        /// <summary>
        /// simpler matching than creating an object array yourself.
        /// </summary>
        /// <param name="MatchArg">single object to match against.</param>
        /// <param name="status">status flag to send.</param>
        /// <param name="DataArgs">data to send.</param>
        /// <returns></returns>
        public bool MatchRegistered(object MatchArg, AckMessage.StatusType status, object data)
        {
            return MatchRegistered(new object[] { MatchArg }, status, data, null);
        }

        public bool MatchRegistered(object[] MatchArgs, AckMessage.StatusType status, object data)
        {
            return MatchRegistered(MatchArgs, status, data, null);
        }

        public bool MatchRegistered(object[] MatchArgs, AckMessage.StatusType status, object data, AckMessage OrigAck)
        {
            bool Sent = false;
            foreach (RegisteredMessage RegMsg in RegisteredMessages)
            {
                if (RegMsg.Matches(MatchArgs))
                {
                    // we send an EventMessage based on this match using the args provided.
                    AckMessage ack;
                    ack = new AckMessage(status, data, (CommandMessage)RegMsg);
                    ack.MatchParams = MatchArgs;
                    if (null != OrigAck)
                    {
                        if (RegMsg.sender == OrigAck.sender)// really next a Next type jump.
                            goto End;
                        ack.sender = OrigAck.sender; // override source.
                    }
                    Sent |= Send(ack);
                End:
                    // stupid labels must come before code.
                    int x = 1;
                    x += 1;
                }
            }
            return Sent;
        }


        public int MessagesWaitingCount()
        {
            return Messages.Count;
        }

        public bool Ack(CommandMessage msg, AckMessage.StatusType status, object args)
        {
            AckMessage ack = msg.AckCommand(status, args);

            // signal with the ManualResetEvent before sending the ack just in case.
            if (null != msg.StatusChanged)
            {
                msg.StatusChanged.Set();
            }
            return Send(ack);
        }
        public bool Ack(CommandMessage msg, AckMessage.StatusType status)
        {
            return Ack(msg, status, null);
        }
    }
}
