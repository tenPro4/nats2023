// See https://aka.ms/new-console-template for more information
using Nats_Server;

Console.WriteLine("Hello, World!");

Nats.Connect();

string ALLOWED_OPTIONS = "123456qQ";
while (!Nats.DiconnectNats)
{
    Console.Clear();

    Console.WriteLine("NATS demo producer");
    Console.WriteLine("==================");
    Console.WriteLine("Select mode:");
    Console.WriteLine("1) Pub / Sub");
    Console.WriteLine("2) Load-balancing (queue groups)");
    Console.WriteLine("3) Request / Response (explicit)");
    Console.WriteLine("4) Request / Response (implicit)");
    Console.WriteLine("5) Wildcards");
    Console.WriteLine("6) Streaming");
    Console.WriteLine("q) Quit");

    ConsoleKeyInfo input;
    do
    {
        input = Console.ReadKey(true);
    } while (!ALLOWED_OPTIONS.Contains(input.KeyChar));

    switch (input.KeyChar)
    {
        case '1':
            Nats.PubSub("message from producer");
            break;
        case '2':
            Nats.QueueGroups();
            break;
        case '3':
            Nats.RequestResponseExplicit();
            break;
        case '4':
            Nats.RequestResponseImplicit();
            break;
        case '5':
            Nats.Wildcards();
            break;
        case '6':
            Nats.PublishStreaming();
            break;
        case 'Q':
            Nats.Disconnect();
            Nats.CloseNats();
            continue;
    }

    Console.WriteLine();
    Console.WriteLine("Done. Press any key to continue...");
    Console.ReadKey(true);
    Console.Clear();
}