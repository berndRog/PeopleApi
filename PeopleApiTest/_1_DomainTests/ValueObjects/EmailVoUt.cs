using AwesomeAssertions;
using PeopleApi._2_BuildingBlocks._3_Domain.ValueObjects;

namespace PeopleApiTest._1_DomainTests.ValueObjects;

public sealed class EmailVoUt {
   [Fact]
   public void Create_validInput_normalizesCanonicalValue() {
      var result = EmailVo.Create("  Ada.Lovelace@Example.ORG ");

      result.IsSuccess.Should().BeTrue();
      result.Value.Value.Should().Be("ada.lovelace@example.org");
   }

   [Theory]
   [InlineData(null)]
   [InlineData("")]
   [InlineData("ada.example.org")]
   [InlineData("ada@example")]
   public void Create_invalidInput_fails(string? input) {
      var result = EmailVo.Create(input);

      result.IsFailure.Should().BeTrue();
   }
}
