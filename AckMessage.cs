using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleMessageQueue
{
    /// <summary>
    /// A response to a CommandMessage.
    /// This lets the sending party know that their message was received and acted upon.
    /// We can set a specific Ack handler per message so that we don't do a bunch of detection and switching
    /// of the ack message. Instead we go straight to the method that knows what to do with this Ack.
    /// </summary>
    public class AckMessage : Message
    {
        public enum StatusType
        {
            WaitingToStart,
            Started,
            Progress,
            Completed,
            Failed,
            Busy,
            Shutdown,
            UnknownError
        }

        public CommandMessage Command;

        /// <summary>
        /// Status of original command. For trackign wtf is going on.
        /// The requestor shoudl receive an ack at each step of the 
        /// way if the command is for an async call.
        /// </summary>
        public StatusType Status = StatusType.UnknownError;

        /// <summary>
        /// for handling forwarded messages.
        /// Get's filled with what was used to match this message.
        /// </summary>
        public object[] MatchParams;

        public AckMessage()
            : base(MessageType.Ack)
        {
        }

        /// <summary>
        /// Status type ack. Dropping result in favor of status to support partial responses.
        /// </summary>
        /// <param name="s"></param>
        public AckMessage(StatusType s)
            : base(MessageType.Ack)
        {
            Status = s;
        }

        /// <summary>
        /// auto-building of the ack.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="m"></param>
        public AckMessage(StatusType s, CommandMessage m)
            : base(MessageType.Ack)
        {
            Status = s;
            Command = m;
            recipient = m.sender;
        }

        public AckMessage(StatusType s, string msg, CommandMessage m)
            : base(MessageType.Ack, msg)
        {
            Status = s;
            Command = m;
            recipient = m.sender;
        }

        public AckMessage(StatusType s, object args, CommandMessage m)
            : base(MessageType.Ack, args)
        {
            Status = s;
            Command = m;
            recipient = m.sender;
        }
    }
}
