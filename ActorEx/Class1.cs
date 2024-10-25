using Akka.Actor;

namespace ActorEx;

public readonly record struct WaveGreeting;
public readonly record struct VocalGreeting(string Name);

public readonly record struct GreetingResponse(string Message);

public sealed class PersonActor : ReceiveActor
{
    public PersonActor()
    {
        Receive<VocalGreeting>(m => Sender.Tell(new GreetingResponse($"Hi, {m.Name}!")));
        Receive<WaveGreeting>(_ => Sender.Tell(new GreetingResponse("*Waving*!")));
    }
} 