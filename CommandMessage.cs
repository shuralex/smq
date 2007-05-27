using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;

namespace SimpleMessageQueue
{
    public class CommandMessage :Message
    {
        public delegate void AckCallback(AckMessage msg);
        public AckCallback AckHandler;
        public AckMessage.StatusType Status = AckMessage.StatusType.WaitingToStart;
        public List<object> cmdArgs;
        public bool WantAck = false;
        /// <summary>
        /// for when we use a WaitOne call to signal completion.
        /// </summary>
        public object AckData;

        // is we requested a blocking call, then we monitor this to tell 
        // when the recieving entity has processed it.
        public ManualResetEvent StatusChanged; // = new ManualResetEvent(false);
        public object Command;

        public CommandMessage()
            :base(MessageType.Command)
        {
        }

        public CommandMessage(object cmd)
            : base(MessageType.Command)
        {
            Command = cmd;
        }

        public CommandMessage(object cmd, AckCallback callback)
            : base(MessageType.Command)
        {
            AckHandler = callback;
            WantAck = true;
            Command = cmd;
        }

        public CommandMessage(object cmd, bool useEvent)
            : base(MessageType.Command)
        {
            StatusChanged = new ManualResetEvent(false);
            WantAck = false;
            Command = cmd;
        }
        public CommandMessage(object cmd, object args)
            : base(MessageType.Command, args)
        {
            Command = cmd;
        }

        public CommandMessage(object cmd, object args, AckCallback callback)
            : base(MessageType.Command, args)
        {
            AckHandler = callback;
            WantAck = true;
            Command = cmd;
        }

        public CommandMessage(object cmd, object args, bool useEvent)
            : base(MessageType.Command, args)
        {
            StatusChanged = new ManualResetEvent(false);
            StatusChanged.Reset();
            WantAck = false;
            Command = cmd;
        }
        //     public CommandMessage(object[] args, bool wantAck)
   //         : base(MessageType.Command, args)
  //      {
  //      }

        public AckMessage AckCommand(AckMessage.StatusType status)
        {
            if (StatusChanged != null)
                StatusChanged.Set();
            if (!WantAck)
                return null;
            AckMessage ack = new AckMessage(status, this);
            
            return ack;
        }

        public AckMessage AckCommand(AckMessage.StatusType status, object args)
        {
            if (StatusChanged != null)
                StatusChanged.Set();
            if (!WantAck)
                return null;
            AckMessage ack = new AckMessage(status, args, this);
            return ack;
        }

        public AckMessage AckCommand(AckMessage.StatusType status, string msg)
        {
            if (StatusChanged != null)
                StatusChanged.Set();
            if (!WantAck)
                return null;
            AckMessage ack = new AckMessage(status, msg, this);
            return ack;
        }

        public void WaitForStatus()
        {
            if (null == StatusChanged)
                return;
            StatusChanged.WaitOne();
            StatusChanged.Reset();
        }
    }
}
