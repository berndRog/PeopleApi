using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using PeopleApiTest.TestInfrastructure;

namespace PeopleApiTest._5_ApiTests;

public sealed class PeopleCountControllerE2eT : TestBaseEndToEnd {
   private const string Url = "/peopleapi/v1/people/count";
   private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

   [Fact]
   public async Task CountAsync_emptyDatabase_returnsZero() {
      // Arrange
      // A fresh PeopleApi database is intentionally empty. Seed data belongs to the client.

      // Act
      var response = await Client.GetAsync(Url, _ct);
      var count = await response.Content.ReadFromJsonAsync<int>(_ct);

      // Assert
      response.StatusCode.Should().Be(HttpStatusCode.OK);
      count.Should().Be(0);
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Der Count-Endpunkt wird über den realen HTTP-Pfad geprüft.
 * - Eine frisch erzeugte PeopleApi-Datenbank ist bewusst leer. Der Android-Client
 *   kann dadurch count == 0 erkennen und seine Beispieldaten selbst übertragen.
 * - Dafür muss nicht die komplette Personenliste geladen werden.
 */
