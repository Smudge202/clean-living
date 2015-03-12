using FluentAssertions;
using Xunit;

namespace CleanLiving.GameEngine.Tests
{
    public class ActorTests
    {
        [Fact]
        public void GivenNewActorWhenHealthRequestedReturnsFullHealth()
        {
            var fullHealth = 1m;
            var target = new Actor();
            target.Health.Should().Be(fullHealth);
        }
    }
}
