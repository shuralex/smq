using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleMessageQueue
{
    /// <summary>
    /// An event packet is an answer to a registered message
    /// </summary>
    public class EventMessage :Message
    {
        RegisteredMessage Reg;

        public EventMessage()
            : base(MessageType.Event)
        {

        }

        /// <summary>
        /// a simple event with no data to return back
        /// </summary>
        /// <param name="reg"></param>
        public EventMessage(RegisteredMessage reg)
        {
            base.type = MessageType.Event;
            Reg = reg;
        }

        /// <summary>
        /// An event WITH data.
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="args"></param>
        public EventMessage(RegisteredMessage reg, object[] args)
        {
            Reg = reg;
            base.Args = args;
        }


    }
}
