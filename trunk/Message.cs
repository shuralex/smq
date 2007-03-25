using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleMessageQueue
{
    /// <summary>
    /// A message is a singular packet of information that requests an action be performed 
    /// by another parlt of the botmaster heiarchy of objects.
    /// This can be events, commands, or informaiton passing messages.
    /// 
    /// </summary>
    public class Message
    {
        public enum MessageType
        {
            Info,
            Command,
            Ack,
            Registered,
            UnRegistered,
            Event,
            Bounce
        }

        public object Recipient { get { return recipient; } }
        public object Sender { get { return sender; } }
        MessageType Type { get { return type; } }
        public object[] Args;

        internal object recipient;
        internal object sender;
        internal MessageType type;

        public Message()
        {
        }

        public bool IsSender(Object obj)
        {
            return sender.Equals(obj);
        }

        public bool IsRecipient(Object obj)
        {
            return recipient.Equals(obj);
        }

        public Message(MessageType _type)
        {
            type = _type;
        }

        public Message(MessageType _type, object[] args)
        {
            type = _type;
            Args = args;
        }

        // returns a bounce message indicating that this queue doesn't understand the message.
        public BounceMessage Bounce()
        {
            return new BounceMessage(this);
        }

        public BounceMessage Bounce(string msg)
        {
            return new BounceMessage(this, msg);
        }
    }
}
