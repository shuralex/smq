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
        public bool Success = false;
        public List<object> cmdArgs;
        public bool WantAck = true;

        // is we requested a blocking call, then we monitor this to tell 
        // when the recieving entity has processed it.
        public ManualResetEvent EventTripped = new ManualResetEvent(false);
        public object Command;

        public CommandMessage()
            :base(MessageType.Command)
        {
        }

   //     public CommandMessage(object[] args)
   //         : base(MessageType.Command, args)
   //     {
   //         WantAck = true;
    //    }
        public CommandMessage(object cmd)
            : base(MessageType.Command)
        {
            WantAck = true;
            Command = cmd;
        }


        public CommandMessage(object cmd, object[] args)
            : base(MessageType.Command, args)
        {
            WantAck = true;
            Command = cmd;
        }

        public CommandMessage(object cmd, object[] args, AckCallback callback)
            : base(MessageType.Command, args)
        {
            AckHandler = callback;
            Command = cmd;
        }

   //     public CommandMessage(object[] args, bool wantAck)
   //         : base(MessageType.Command, args)
  //      {
  //      }

        public AckMessage AckCommand(AckMessage.StatusType status)
        {
            if (!WantAck)
                return null;
            AckMessage ack = new AckMessage(status, this);
            return ack;
        }

        public AckMessage AckCommand(AckMessage.StatusType status, object[] args)
        {
            if (!WantAck)
                return null;
            AckMessage ack = new AckMessage(status, args, this);
            ack.Args = args;
            return ack;
        }

        public AckMessage AckCommand(AckMessage.StatusType status, string msg)
        {
            if (!WantAck)
                return null;
            AckMessage ack = new AckMessage(status, new object[] { msg }, this);
            return ack;
        }

    //    public List<object> CmdArgs()
    //    {
    //        if (null != cmdArgs)
    //            return cmdArgs;
    //        cmdArgs = new List<object>();
    //        for(int x = 1; x < base.Args.Length;x++)
    //            cmdArgs.Add(base.Args[x]);
    //        return cmdArgs;
    //    }

    //    public object CommandType()
    //    {
    //        return Args[0];
   //     }
    }
}
