The most simplistic method of use is to construct a new messagequeue similar to the folowing code.

```

using SimpleMessageQueue;

public class MyClass
{
  public enum Commands
  {
      Add,
      Shutdown
  }

  MessageQueue MQueue;

  public MyClass()
  {
    // This creates a MessageQueue and asks it to identify with this new object and 
    // to spawn a new thread to run the MQueue.Run() loop.
    MQueue = new MessageQueue(this, true);
    
    // This sets up our CommandMessage handler. 
    MQueue.RegisterPacketCallback(Message.MessageType.Command, new MessageQueue.MessageCallback(HandleCommand));
  }
        public void HandleCommand(Message msg)
        {
            CommandMessage Msg = (CommandMessage)msg;
            switch ((Commands)Msg.Command)
            {
                case Commands.Add:
                    DoAdditionAndReply(Msg);
                    break;
                case Commands.Shutdown:
                    MQueue.Shutdown = true;
                    break;
                default:
                    MQueue.Send(Msg.Bounce("Command not supported"));
                    break;
            }
        }
   void DoAdditionAndReply()
   {

   }
}

```