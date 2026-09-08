using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using PeopleApiTest.TestInfrastructure;

namespace PeopleApiTest._5_ApiTests;

public sealed class PeopleCountControllerE2eT : TestBaseEndToEnd {
   private const string Url = "/peopleapi/v1/people/count";
   private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

   [Fact]
   public async Task CountAsync_ok() {
      // Arrange
      // Program initializes a fresh test database with 26 deterministic seed people.

      // Act
      var response = await Client.GetAsync(Url, _ct);
      var count = await response.Content.ReadFromJsonAsync<int>(_ct);

      // Assert
      response.StatusCode.Should().Be(HttpStatusCode.OK);
      count.Should().Be(26);
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Der Count-Endpunkt wird über den realen HTTP-Pfad geprüft.
 * - Der Client kann damit vor einem optionalen Seed prüfen, ob bereits Personen
 *   auf dem Server vorhanden sind, ohne die komplette Liste laden zu müssen.
 */
