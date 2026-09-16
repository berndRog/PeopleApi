using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PeopleApi._3_Core.People._2_Application.Dtos;
using PeopleApi._4_Infrastructure.Persistence.Database;
using PeopleApiTest.TestInfrastructure;

namespace PeopleApiTest._5_ApiTests;

public sealed class PeopleControllerE2eT : TestBaseEndToEnd {
   private const string Url = "/peopleapi/v1/people";
   private const string LocalImagePath =
      "/data/user/0/de.rogallab.mobile/files/people510/ada.jpg";

   private readonly CancellationToken _ct =
      TestContext.Current.CancellationToken;

   [Fact]
   public async Task GetAllAsync_emptyDatabase_ok() {
      var response = await Client.GetAsync(Url, _ct);
      var people = await response.Content.ReadFromJsonAsync<List<PersonDto>>(_ct);

      response.StatusCode.Should().Be(HttpStatusCode.OK);
      people.Should().NotBeNull();
      people.Should().BeEmpty();
   }

   [Fact]
   public async Task CreateAsync_jsonWithLocalImagePath_ok() {
      var expected = NewPerson(imageUrl: LocalImagePath);

      var response = await Client.PostAsJsonAsync(Url, expected, _ct);
      var actual = await response.Content.ReadFromJsonAsync<PersonDto>(_ct);

      response.StatusCode.Should().Be(HttpStatusCode.Created);
      response.Headers.Location.Should().NotBeNull();
      response.Headers.Location!.ToString().Should().Contain(expected.Id.ToString());
      actual.Should().BeEquivalentTo(expected);
   }

   [Fact]
   public async Task CreateAsync_formattedContactData_isStoredCanonical() {
      var request = NewPerson() with {
         Email = "  Ada.Lovelace@Example.ORG ",
         Phone = "+49 (0)30 / 1234-56"
      };

      var response = await Client.PostAsJsonAsync(Url, request, _ct);
      var actual = await response.Content.ReadFromJsonAsync<PersonDto>(_ct);

      response.StatusCode.Should().Be(HttpStatusCode.Created);
      actual.Should().NotBeNull();
      actual!.Email.Should().Be("ada.lovelace@example.org");
      actual.Phone.Should().Be("+4930123456");
   }

   [Fact]
   public async Task CreateAsync_persistedTimestamps_areUtcDateTime() {
      var created = await CreatePersonAsync();

      using var scope = Factory.Services.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
      var person = await dbContext.People
         .AsNoTracking()
         .SingleAsync(item => item.Id == created.Id, _ct);

      person.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
      person.UpdatedAt.Kind.Should().Be(DateTimeKind.Utc);
   }

   [Fact]
   public async Task GetByIdAsync_ok() {
      var created = await CreatePersonAsync(LocalImagePath);

      var response = await Client.GetAsync($"{Url}/{created.Id}", _ct);
      var actual = await response.Content.ReadFromJsonAsync<PersonDto>(_ct);

      response.StatusCode.Should().Be(HttpStatusCode.OK);
      actual.Should().BeEquivalentTo(created);
   }

   [Fact]
   public async Task UpdateAsync_replacesImageUrlString_ok() {
      var created = await CreatePersonAsync(LocalImagePath);
      var update = created with {
         FirstName = "Grace",
         LastName = "Hopper",
         Email = "grace.hopper@example.org",
         Phone = "+4930987654",
         ImageUrl = "content://media/external/images/media/42"
      };

      var response = await Client.PutAsJsonAsync(
         $"{Url}/{created.Id}",
         update,
         _ct
      );
      var actual = await response.Content.ReadFromJsonAsync<PersonDto>(_ct);

      response.StatusCode.Should().Be(HttpStatusCode.OK);
      actual.Should().BeEquivalentTo(update);
   }

   [Fact]
   public async Task UpdateAsync_nullImageUrl_clearsStoredString() {
      var created = await CreatePersonAsync(LocalImagePath);
      var update = created with { ImageUrl = null };

      var response = await Client.PutAsJsonAsync(
         $"{Url}/{created.Id}",
         update,
         _ct
      );
      var actual = await response.Content.ReadFromJsonAsync<PersonDto>(_ct);

      response.StatusCode.Should().Be(HttpStatusCode.OK);
      actual.Should().NotBeNull();
      actual!.ImageUrl.Should().BeNull();
   }

   [Fact]
   public async Task UpdateAsync_routeIdIdentifiesResource() {
      var created = await CreatePersonAsync();
      var unrelatedBodyId = Guid.NewGuid();
      var update = created with {
         Id = unrelatedBodyId,
         FirstName = "Grace"
      };

      var response = await Client.PutAsJsonAsync(
         $"{Url}/{created.Id}",
         update,
         _ct
      );
      var actual = await response.Content.ReadFromJsonAsync<PersonDto>(_ct);

      response.StatusCode.Should().Be(HttpStatusCode.OK);
      actual.Should().NotBeNull();
      actual!.Id.Should().Be(created.Id);
      actual.Id.Should().NotBe(unrelatedBodyId);
   }

   [Fact]
   public async Task DeleteAsync_removesPerson() {
      var created = await CreatePersonAsync(LocalImagePath);

      var deleteResponse = await Client.DeleteAsync($"{Url}/{created.Id}", _ct);
      var getResponse = await Client.GetAsync($"{Url}/{created.Id}", _ct);

      deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
      getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
   }

   [Fact]
   public async Task GetByIdAsync_unknownId_returnsNotFound() {
      var response = await Client.GetAsync($"{Url}/{Guid.NewGuid()}", _ct);

      response.StatusCode.Should().Be(HttpStatusCode.NotFound);
   }

   [Fact]
   public async Task CreateAsync_duplicateId_returnsConflict() {
      var person = NewPerson();

      var firstResponse = await Client.PostAsJsonAsync(Url, person, _ct);
      var secondResponse = await Client.PostAsJsonAsync(Url, person, _ct);

      firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
      secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
   }

   [Fact]
   public async Task CreateAsync_invalidName_returnsBadRequest() {
      var person = NewPerson() with { FirstName = "A" };

      var response = await Client.PostAsJsonAsync(Url, person, _ct);

      response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
   }

   private async Task<PersonDto> CreatePersonAsync(
      string? imageUrl = null
   ) {
      var person = NewPerson(imageUrl);

      var response = await Client.PostAsJsonAsync(Url, person, _ct);
      response.StatusCode.Should().Be(HttpStatusCode.Created);

      var created = await response.Content.ReadFromJsonAsync<PersonDto>(_ct);
      created.Should().NotBeNull();
      return created!;
   }

   private static PersonDto NewPerson(
      string? imageUrl = null
   ) => new(
      Guid.NewGuid(),
      "Ada",
      "Lovelace",
      "ada.lovelace@example.org",
      "+4930123456",
      imageUrl
   );
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Die Tests verwenden application/json für POST und PUT.
 * - ImageUrl wird wie Email und Phone als nullable String übertragen.
 * - Lokale Android-Pfade und Content-URIs werden unverändert gespeichert.
 * - Es werden keine Dateien hochgeladen, erzeugt, gelesen oder gelöscht.
 * - CRUD, NotFound, Conflict und Domain-Validierung bleiben Teil des Vertrags.
 */
