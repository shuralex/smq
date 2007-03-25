using System;
using System.Collections.Generic;
using System.Text;

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
        Queue<Message> Messages;
        Queue<Message> Outbound;
        /// <summary>
        /// messages that we need to send out upon activity happening
        /// </summary>
        List<Message> RegisteredMessages;

        /// <summary>
        /// run until this is tripped.
        /// </summary>
        public bool Shutdown = false;

        /// <summary>
        /// who we know about. Identity is based off of the ToString() value of the receiving object.
        /// We store this string and it's associated (discovered) MQueue object thus avoiding multiple
        /// introspections (which I assume can be slow.)
        /// </summary>
        Dictionary<object, MessageQueue> ValidRecipients;

        // so we know who we are.
        object Identity { get { return identity; } }
        internal object identity;

        System.Threading.Thread LocalThread;

        bool InLocalExec = false;
        public delegate void MessageCallback(Message msg);

        Dictionary<Message.MessageType, List<MessageCallback>> MessageCallbacks;

        /// <summary>
        /// Exists only if this simulator is being actively communicated with.
        /// </summary>
        System.Threading.Thread Thread;


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
            MessageCallbacks = new Dictionary<Message.MessageType, List<MessageCallback>>();
            Messages = new Queue<Message>();
            Outbound = new Queue<Message>();
            RegisteredMessages = new List<Message>();
            ValidRecipients = new Dictionary<object, MessageQueue>();
            identity = sender;
            CaptureThread();
            if (CreateThread)
            {
                Thread = new System.Threading.Thread(Run);
                Thread.Start();
            }
        }

        /// <summary>
        /// Simple loop to run as the entry point of a thread.
        /// This provides a clean method to give a queue and it's object 
        /// it's own thread without the user worying about thread management.
        /// It fully automates the thread setup and execution relieving the user to just ask for it.
        /// </summary>
        void Run()
        {
            // capture our curent thread so that we know to always queue messages 
            // instead of parsing them on the same thread as the sender
            CaptureThread();
            while (!Shutdown)
            {
                if (!ProcessMessages())
                    System.Threading.Thread.Sleep(5);
            }
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
            if(!ValidRecipients.ContainsKey(ID))
                ValidRecipients.Add(ID, r);
        }

        public void RegisterPacketCallback(Message.MessageType type, MessageCallback callback)
        {
            if (!MessageCallbacks.ContainsKey(type))
                MessageCallbacks.Add(type,new List<MessageCallback>());
            MessageCallbacks[type].Add(callback);
        }

        public void UnregisterPacketCallback(Message.MessageType type, MessageCallback callback)
        {
            if (MessageCallbacks.ContainsKey(type))
                return;
            if(MessageCallbacks[type].Contains(callback))
                MessageCallbacks[type].Remove(callback);
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
                return true;
            }
            else if (msg.type == Message.MessageType.UnRegistered)
            {
                foreach (RegisteredMessage Rmsg in RegisteredMessages)
                {
                    //shoudl we be also matching up the AckCallback object?
                    if (msg.sender == Rmsg.sender
                        && ArgsMatch(msg.Args, Rmsg.Args))
                        RegisteredMessages.Remove(Rmsg);
                    return true;
                }
            }
            Messages.Enqueue(msg);
            if (LocalThread == System.Threading.Thread.CurrentThread)
            {
                InLocalExec = true;
                ProcessMessages();
                InLocalExec = false;
                while (Outbound.Count > 0)
                    Send(Outbound.Dequeue());
            }
            return true;
        }

        /// <summary>
        /// sends the message tot he target queue.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool Send(Message msg)
        {
            if (null == msg || null == msg.recipient)
                return true; // so that unwanted acks are silently dropped.
            MessageQueue RecipientMQ;
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


            msg.sender = this.identity;
                if (!RecipientMQ.ValidRecipients.ContainsKey(Identity))
                    RecipientMQ.ValidRecipients.Add(Identity, this);

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
                return RecipientMQ.Recv(msg);
            }
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
                    System.Reflection.Binder binder;
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
            object t = identity;
            if(Messages.Count == 0)
                return false;
            while (Messages.Count > 0)
            {
                Message msg = Messages.Dequeue();
                // ack and bounce messages get passed off to the AckHandler that is set within the CommandMessage
                if ((msg.type == Message.MessageType.Ack 
                    || msg.type == Message.MessageType.Bounce)
                    && ((AckMessage)msg).Command != null 
                    && ((AckMessage)msg).Command.AckHandler != null)
                {
                    ((AckMessage)msg).Command.AckHandler((AckMessage)msg);
                }
                else if (MessageCallbacks.ContainsKey(msg.type)
                    && MessageCallbacks[msg.type].Count > 0)
                {
                    foreach (MessageCallback c in MessageCallbacks[msg.type])
                    {
                        if (null != c)
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
            }
            return true;
        }

        public bool MatchRegistered(object[] MatchArgs, AckMessage.StatusType status,  object[] DataArgs)
        {
            bool Sent = false;
            foreach (RegisteredMessage RegMsg in RegisteredMessages)
            {
                if (ArgsMatch(RegMsg.Args, MatchArgs))
                {
                    // we send an EventMessage based on this match using the args provided.
                    Sent |= Send(new AckMessage(status, DataArgs, (CommandMessage)RegMsg));
                }
            }
            return Sent;
        }

        bool ArgsMatch(object[] args1, object[] args2)
        {
            if (args1.Length > args2.Length)
                return false;
            for (int x = 0; x < args1.Length; x++)
            {
                if ((!args1[x].Equals(args2[x])) 
                    || (args1[x] == args2[x]))
                    return false;
            }
            return true;
        }
    }
}
