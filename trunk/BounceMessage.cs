using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleMessageQueue
{
    public class BounceMessage :Message
    {
        public Message BouncedMessage;
        public string Reason;

        public BounceMessage(Message msg)
            : base(MessageType.Bounce)
        {
            recipient = msg.sender;
            BouncedMessage = msg;
        }

        public BounceMessage(Message msg, string reason)
            :base(MessageType.Bounce)
        {
            recipient = msg.sender;
            BouncedMessage = msg;
            Reason = reason;
        }
    }
}
