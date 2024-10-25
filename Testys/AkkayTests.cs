using ActorEx;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using FluentAssertions;
using Xunit;

namespace Testys;

public sealed class AkkayTests : TestKit
{
    [Fact]
    public void name()
    {
        var actorSystem = ActorSystem.Create("MyActorSystem");
        var reff = actorSystem.ActorOf<PersonActor>("Steve");
        var ree = reff.Ask(new WaveGreeting()).Result;
        var raff = actorSystem.ActorSelection("Steve");
        var probe = CreateTestProbe();
        var actor = Sys.ActorOf(Akka.Actor.Props.Create<PersonActor>());
        
        actor.Tell(new WaveGreeting(), probe.Ref);
        var response = probe.ExpectMsg<GreetingResponse>();

        response.Message.Should().Be("Hi, Bob!");

    }
    
}