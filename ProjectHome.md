SimpleMessageQueue is a lightweight Message Queue library that is designed to be as easy to use as events without the thread entanglement dangers of events and handlers.

Simple message queue is just that. a really simple, easy to use message queue that doesn't force you to manage the details of a queue. It brings along the power of thread disassociation on events that the regular C# event and callback system can't provide without a bunch of custom support code.

# Features #
  * Lightweight, easilly extended Message class.
  * Commands with or without Ack callbacks.
  * Triggers via WaitOne being returned to tell when a message has been processed.
  * Auto-spawning of a thread for each queue Run()ner.
  * (Un)RegisteredMessage for event-style Acks.
  * Multi-Ack capable with statusmessages for progress reports.
  * Generic object[.md](.md) payload in all Message classes.
  * CommandMessage class that suports a Command enum with data payload for easy CommandMessage code path switching.
  * Auto-discovery of the Queue within your target Object. No need to set a specific name for the receiving queue.
  * Custom labeling/association of Queues for one queue for multiple target objects on a single queue runner.(usefull for small objects that don't really need their own thread).

SMQ is used by SecondLife Client Library (SLCL) to provide a reliable and clean thread disassociation within it's various Manager classes.

If you use SMQ in a commercial product please consider purchasing a support contract with TerraBox.com. Support is handled in a professional bug tracking system with phone, email, and on-site based on your needs. It can be purchased as business hours or 24x7 availability  with hourly or per-incident use.