// See https://aka.ms/new-console-template for more information
using Nats_Server;

Console.WriteLine("Hello, World!");
Nats.Connect();
Nats.SubscribePubSub();
Nats.SubscribeQueueGroups();
Nats.SubscribeWildcards("nats.*.wildcards");
Nats.SubscribeRequestResponse();
Nats.SubscribeStreaming("c1");
while (!Nats.DiconnectNats)
{
}