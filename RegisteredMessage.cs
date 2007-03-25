using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleMessageQueue
{
    /// <summary>
    /// A registered message is one that isn't acted on immediately.
    /// Instead, it is "registered" with the recipient's MQueue. 
    /// The recipient will then call into it's MQueue with MatchRegistered with 2 arrays of Objects.
    /// the first object array is what to match against. The second is the data to send in the Info packet.
    /// 
    /// This uses COmmandMessage in a slightly odd way, the command param is left to 0.
    /// See Handler.AddClient() for an example.
    /// </summary>
    public class RegisteredMessage :CommandMessage
    {
        /// <summary>
        /// And empty registered packet.
        /// </summary>
        public RegisteredMessage()
        {
            // override the message type to be a registered.
            base.type = MessageType.Registered;
        }

        /// <summary>
        /// A registered message with args to match against.
        /// usefull for construction and passing to MQueue.Send(new msg....);
        /// </summary>
        /// <param name="args"></param>
        public RegisteredMessage(object[] args)
            :base(0, args)
        {
            // override the message type to be a registered.
            base.type = MessageType.Registered;
        }

        public RegisteredMessage(object[] args, CommandMessage.AckCallback callback)
            : base(0, args, callback)
        {
            // override the message type to be a registered.
            base.type = MessageType.Registered;
        }
    }

    public class UnRegisteredMessage :CommandMessage
    {
        public UnRegisteredMessage() :base()
        {
            // override the message type to be a registered.
            base.type = MessageType.Registered;
        }

        /// <summary>
        /// A registered message with args to match against.
        /// usefull for construction and passing to MQueue.Send(new msg....);
        /// </summary>
        /// <param name="args"></param>
        public UnRegisteredMessage(object[] args)
            : base(null, args)
        {
            // override the message type to be a registered.
            base.type = MessageType.UnRegistered;
        }

        public UnRegisteredMessage(object[] args, CommandMessage.AckCallback callback)
            : base(null, args, callback)
        {
            // override the message type to be a registered.
            base.type = MessageType.UnRegistered;
        }

    }
}
