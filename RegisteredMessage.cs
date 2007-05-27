using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

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

        public object[] MatchArgs
        {
            get { return matchArgs; }
            set
            {
                matchArgs = value;
            }
        }

        private object[] matchArgs;
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
            :base(0, null)
        {
            // override the message type to be a registered.
            base.type = MessageType.Registered;
            matchArgs = args;
        }

        public RegisteredMessage(object[] args, CommandMessage.AckCallback callback)
            : base(0, null, callback)
        {
            // override the message type to be a registered.
            base.type = MessageType.Registered;
            matchArgs = args;
        }

        public RegisteredMessage(object args, CommandMessage.AckCallback callback)
            : base(0, null, callback)
        {
            // override the message type to be a registered.
            base.type = MessageType.Registered;
            matchArgs = new object[] { args };
        }

        public RegisteredMessage(object args)
            : base(0, null)
        {
            // override the message type to be a registered.
            base.type = MessageType.Registered;
            matchArgs = new object[] { args };
        }

        public bool Matches(object[] Params)
        {
            if (null == Params || Params.Length == 0)
                return false;
            // if we have more qualifiers than the event, we don't match. we're too specific.
            if (matchArgs.Length > Params.Length)
                return false;
            for (int x = 0; x < matchArgs.Length; x++)
            {
                // compare using the taret object's equals. 
                // so we compare only the same types of objects.
                if (!matchArgs[x].Equals(Params[x])) 
                    return false;
            }
            return true;
        }
    }

    public class UnRegisterMessage :RegisteredMessage
    {
        public UnRegisterMessage() :base()
        {
            // override the message type to be a UnRegisterMessage.
            base.type = MessageType.Registered;
        }

         public UnRegisterMessage(object args)
            : base(args)
        {
            // override the message type to be a UnRegisterMessage.
            base.type = MessageType.UnRegister;
        }

        public UnRegisterMessage(object args, CommandMessage.AckCallback callback)
            : base(args, callback)
        {
            // override the message type to be a UnRegisterMessage.
            base.type = MessageType.UnRegister;
        }

    }
}
