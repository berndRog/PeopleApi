using AwesomeAssertions;
using PeopleApi._2_BuildingBlocks._3_Domain.ValueObjects;

namespace PeopleApiTest._1_DomainTests.ValueObjects;

public sealed class PhoneVoUt {
   [Theory]
   [InlineData("+49 (0)511 / 1234-567", "+495111234567")]
   [InlineData("0049 0511 1234 567", "+495111234567")]
   [InlineData("0511 1234 567", "05111234567")]
   public void Create_validInput_normalizesCanonicalValue(
      string input,
      string expected
   ) {
      var result = PhoneVo.Create(input);

      result.IsSuccess.Should().BeTrue();
      result.Value.Value.Should().Be(expected);
   }

   [Theory]
   [InlineData(null)]
   [InlineData("")]
   [InlineData("123")]
   [InlineData("phone 0511 1234567")]
   public void Create_invalidInput_fails(string? input) {
      var result = PhoneVo.Create(input);

      result.IsFailure.Should().BeTrue();
   }
}
