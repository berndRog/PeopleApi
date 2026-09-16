using AwesomeAssertions;
using PeopleApi._3_Core.People._3_Domain.Entities;

namespace PeopleApiTest._1_DomainTests.Entities;

public sealed class PersonUt {
   [Fact]
   public void Create_utcTimestamp_initializesAggregateRoot() {
      var utcNow = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);

      var result = Person.Create(
         Guid.NewGuid(), "Ada", "Lovelace", null, null, null, utcNow
      );

      result.IsSuccess.Should().BeTrue();
      result.Value.CreatedAt.Should().Be(utcNow);
      result.Value.UpdatedAt.Should().Be(utcNow);
      result.Value.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
   }

   [Fact]
   public void Create_localTimestamp_throws() {
      var localNow = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Local);

      var action = () => Person.Create(
         Guid.NewGuid(), "Ada", "Lovelace", null, null, null, localNow
      );

      action.Should().Throw<ArgumentException>();
   }
}
