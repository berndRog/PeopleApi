using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AwesomeAssertions;
using PeopleApi._3_Core.People._2_Application.Dtos;
using PeopleApiTest.TestInfrastructure;

namespace PeopleApiTest._5_ApiTests;

public sealed class PeopleControllerE2eT : TestBaseEndToEnd {
   private const string Url = "/peopleapi/v1/people";
   private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

   // Small valid 1x1 PNG used for People requests that include a profile image.
   private static readonly byte[] PngBytes = Convert.FromBase64String(
      "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9WlH0wAAAABJRU5ErkJggg=="
   );

   [Fact]
   public async Task GetAllAsync_emptyDatabase_ok() {
      // Arrange
      // A fresh PeopleApi database is intentionally empty. Seed data belongs to the client.

      // Act
      var response = await Client.GetAsync(Url, _ct);
      var people = await response.Content
         .ReadFromJsonAsync<List<PersonDto>>(_ct);

      // Assert
      response.StatusCode.Should().Be(HttpStatusCode.OK);
      people.Should().NotBeNull();
      people.Should().BeEmpty();
   }

   [Fact]
   public async Task CreateAsync_withoutImage_ok() {
      // Arrange
      var id = Guid.NewGuid();
      using var form = CreatePersonForm(
         id,
         firstName: "Ada",
         lastName: "Lovelace",
         email: "ada.lovelace@example.org",
         phone: "+49 30 123456"
      );

      // Act
      var response = await Client.PostAsync(Url, form, _ct);
      var actual = await response.Content.ReadFromJsonAsync<PersonDto>(_ct);

      // Assert
      response.StatusCode.Should().Be(HttpStatusCode.Created);
      response.Headers.Location.Should().NotBeNull();
      response.Headers.Location!.ToString().Should().Contain(id.ToString());

      actual.Should().NotBeNull();
      actual!.Id.Should().Be(id);
      actual.FirstName.Should().Be("Ada");
      actual.LastName.Should().Be("Lovelace");
      actual.Email.Should().Be("ada.lovelace@example.org");
      actual.Phone.Should().Be("+49 30 123456");
      actual.ImageUrl.Should().BeNull();
   }

   [Fact]
   public async Task CreateAsync_withImage_storesImageAndSetsAbsoluteUrl() {
      // Arrange
      var id = Guid.NewGuid();
      using var form = CreatePersonForm(
         id,
         firstName: "Ada",
         lastName: "Lovelace",
         email: "ada.lovelace@example.org",
         phone: "+49 30 123456",
         imageBytes: PngBytes,
         imageFileName: "portrait.png",
         imageContentType: "image/png"
      );

      // Act
      var response = await Client.PostAsync(Url, form, _ct);
      var actual = await response.Content.ReadFromJsonAsync<PersonDto>(_ct);

      // Assert
      response.StatusCode.Should().Be(HttpStatusCode.Created);
      actual.Should().NotBeNull();
      actual!.ImageUrl.Should().NotBeNullOrWhiteSpace();

      var imageUri = new Uri(actual.ImageUrl!);
      imageUri.IsAbsoluteUri.Should().BeTrue();
      imageUri.Host.Should().Be("localhost");
      imageUri.AbsolutePath.Should().Contain("/images/");

      // Verify that People create really orchestrated the file-system write.
      var fileName = Path.GetFileName(imageUri.AbsolutePath);
      var filePath = Path.Combine(Factory.ImageDirectory, fileName);
      File.Exists(filePath).Should().BeTrue();
      File.ReadAllBytes(filePath).Should().Equal(PngBytes);
   }

   [Fact]
   public async Task GetByIdAsync_ok() {
      // Arrange
      var created = await CreatePersonAsync();

      // Act
      var response = await Client.GetAsync($"{Url}/{created.Id}", _ct);
      var actual = await response.Content.ReadFromJsonAsync<PersonDto>(_ct);

      // Assert
      response.StatusCode.Should().Be(HttpStatusCode.OK);
      actual.Should().NotBeNull();
      actual.Should().BeEquivalentTo(created);
   }

   [Fact]
   public async Task UpdateAsync_withoutImage_keepsExistingImage() {
      // Arrange
      var created = await CreatePersonAsync(withImage: true);
      var oldImageUrl = created.ImageUrl;
      using var form = CreateUpdateForm(
         firstName: "Grace",
         lastName: "Hopper",
         email: "grace.hopper@example.org",
         phone: "+49 30 987654"
      );

      // Act
      var response = await Client.PutAsync($"{Url}/{created.Id}", form, _ct);
      var actual = await response.Content.ReadFromJsonAsync<PersonDto>(_ct);

      // Assert
      response.StatusCode.Should().Be(HttpStatusCode.OK);
      actual.Should().NotBeNull();
      actual!.FirstName.Should().Be("Grace");
      actual.LastName.Should().Be("Hopper");
      actual.ImageUrl.Should().Be(oldImageUrl);

      // No image operation means the original file remains on disk.
      File.Exists(FilePathFromUrl(oldImageUrl!)).Should().BeTrue();
   }

   [Fact]
   public async Task UpdateAsync_withImage_replacesImageAndDeletesOldFile() {
      // Arrange
      var created = await CreatePersonAsync(withImage: true);
      var oldImagePath = FilePathFromUrl(created.ImageUrl!);
      File.Exists(oldImagePath).Should().BeTrue();

      using var form = CreateUpdateForm(
         firstName: "Grace",
         lastName: "Hopper",
         email: "grace.hopper@example.org",
         phone: "+49 30 987654",
         imageBytes: PngBytes,
         imageFileName: "replacement.png",
         imageContentType: "image/png"
      );

      // Act
      var response = await Client.PutAsync($"{Url}/{created.Id}", form, _ct);
      var actual = await response.Content.ReadFromJsonAsync<PersonDto>(_ct);

      // Assert
      response.StatusCode.Should().Be(HttpStatusCode.OK);
      actual.Should().NotBeNull();
      actual!.ImageUrl.Should().NotBeNullOrWhiteSpace();
      actual.ImageUrl.Should().NotBe(created.ImageUrl);

      // The new image exists and the old image was removed after the DB update.
      File.Exists(FilePathFromUrl(actual.ImageUrl!)).Should().BeTrue();
      File.Exists(oldImagePath).Should().BeFalse();
   }

   [Fact]
   public async Task UpdateAsync_removeImage_clearsUrlAndDeletesOldFile() {
      // Arrange
      var created = await CreatePersonAsync(withImage: true);
      var oldImagePath = FilePathFromUrl(created.ImageUrl!);
      File.Exists(oldImagePath).Should().BeTrue();

      using var form = CreateUpdateForm(
         firstName: created.FirstName,
         lastName: created.LastName,
         email: created.Email,
         phone: created.Phone,
         removeImage: true
      );

      // Act
      var response = await Client.PutAsync($"{Url}/{created.Id}", form, _ct);
      var actual = await response.Content.ReadFromJsonAsync<PersonDto>(_ct);

      // Assert
      response.StatusCode.Should().Be(HttpStatusCode.OK);
      actual.Should().NotBeNull();
      actual!.ImageUrl.Should().BeNull();
      File.Exists(oldImagePath).Should().BeFalse();
   }

   [Fact]
   public async Task UpdateAsync_imageAndRemoveImage_returnsBadRequest() {
      // Arrange
      var created = await CreatePersonAsync(withImage: true);
      using var form = CreateUpdateForm(
         firstName: created.FirstName,
         lastName: created.LastName,
         email: created.Email,
         phone: created.Phone,
         imageBytes: PngBytes,
         imageFileName: "replacement.png",
         imageContentType: "image/png",
         removeImage: true
      );

      // Act
      var response = await Client.PutAsync($"{Url}/{created.Id}", form, _ct);

      // Assert
      response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
   }

   [Fact]
   public async Task DeleteAsync_removesPersonAndReferencedImage() {
      // Arrange
      var created = await CreatePersonAsync(withImage: true);
      var imagePath = FilePathFromUrl(created.ImageUrl!);
      File.Exists(imagePath).Should().BeTrue();

      // Act
      var deleteResponse = await Client.DeleteAsync($"{Url}/{created.Id}", _ct);
      var getResponse = await Client.GetAsync($"{Url}/{created.Id}", _ct);

      // Assert
      deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
      getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
      File.Exists(imagePath).Should().BeFalse();
   }

   [Fact]
   public async Task GetByIdAsync_unknownId_returnsNotFound() {
      // Arrange
      var unknownId = Guid.NewGuid();

      // Act
      var response = await Client.GetAsync($"{Url}/{unknownId}", _ct);

      // Assert
      response.StatusCode.Should().Be(HttpStatusCode.NotFound);
   }

   [Fact]
   public async Task CreateAsync_duplicateId_returnsConflict() {
      // Arrange
      var id = Guid.NewGuid();
      using var firstForm = CreatePersonForm(
         id,
         "Ada",
         "Lovelace",
         "ada.lovelace@example.org",
         "+49 30 123456"
      );
      var firstResponse = await Client.PostAsync(Url, firstForm, _ct);
      firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

      using var secondForm = CreatePersonForm(
         id,
         "Ada",
         "Lovelace",
         "ada.lovelace@example.org",
         "+49 30 123456",
         imageBytes: PngBytes,
         imageFileName: "duplicate.png",
         imageContentType: "image/png"
      );

      // Act
      var secondResponse = await Client.PostAsync(Url, secondForm, _ct);

      // Assert
      secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

      // The image was written before the duplicate-id check in the People use case.
      // Compensation must remove it when creating the Person fails.
      Directory.GetFiles(Factory.ImageDirectory).Should().BeEmpty();
   }

   private async Task<PersonDto> CreatePersonAsync(bool withImage = false) {
      // Use the public API for test setup so the complete HTTP/application/storage
      // path is exercised consistently.
      using var form = CreatePersonForm(
         Guid.NewGuid(),
         firstName: "Ada",
         lastName: "Lovelace",
         email: "ada.lovelace@example.org",
         phone: "+49 30 123456",
         imageBytes: withImage ? PngBytes : null,
         imageFileName: withImage ? "portrait.png" : null,
         imageContentType: withImage ? "image/png" : null
      );

      var response = await Client.PostAsync(Url, form, _ct);
      response.StatusCode.Should().Be(HttpStatusCode.Created);

      var person = await response.Content.ReadFromJsonAsync<PersonDto>(_ct);
      person.Should().NotBeNull();
      return person!;
   }

   private MultipartFormDataContent CreatePersonForm(
      Guid id,
      string firstName,
      string lastName,
      string? email,
      string? phone,
      byte[]? imageBytes = null,
      string? imageFileName = null,
      string? imageContentType = null
   ) {
      var form = new MultipartFormDataContent();
      form.Add(new StringContent(firstName), "FirstName");
      form.Add(new StringContent(lastName), "LastName");
      form.Add(new StringContent(id.ToString()), "Id");

      if (email is not null)
         form.Add(new StringContent(email), "Email");
      if (phone is not null)
         form.Add(new StringContent(phone), "Phone");

      AddImage(form, imageBytes, imageFileName, imageContentType);
      return form;
   }

   private MultipartFormDataContent CreateUpdateForm(
      string firstName,
      string lastName,
      string? email,
      string? phone,
      byte[]? imageBytes = null,
      string? imageFileName = null,
      string? imageContentType = null,
      bool removeImage = false
   ) {
      var form = new MultipartFormDataContent();
      form.Add(new StringContent(firstName), "FirstName");
      form.Add(new StringContent(lastName), "LastName");
      form.Add(new StringContent(removeImage.ToString()), "RemoveImage");

      if (email is not null)
         form.Add(new StringContent(email), "Email");
      if (phone is not null)
         form.Add(new StringContent(phone), "Phone");

      AddImage(form, imageBytes, imageFileName, imageContentType);
      return form;
   }

   private static void AddImage(
      MultipartFormDataContent form,
      byte[]? imageBytes,
      string? imageFileName,
      string? imageContentType
   ) {
      if (imageBytes is null)
         return;

      var fileContent = new ByteArrayContent(imageBytes);
      fileContent.Headers.ContentType = new MediaTypeHeaderValue(
         imageContentType ?? "application/octet-stream"
      );
      form.Add(fileContent, "Image", imageFileName ?? "image.bin");
   }

   private string FilePathFromUrl(string imageUrl) {
      var uri = new Uri(imageUrl);
      var fileName = Path.GetFileName(uri.AbsolutePath);
      return Path.Combine(Factory.ImageDirectory, fileName);
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Die Tests prüfen People-CRUD über den realen HTTP-Endpunkt statt über Mocks.
 * - Eine neue Testdatenbank ist bewusst leer; Beispieldaten werden nicht mehr von
 *   der WebAPI erzeugt, sondern können vom Android-Client gesendet werden.
 * - People-POST und -PUT verwenden multipart/form-data, weil optional eine
 *   Bilddatei Bestandteil desselben fachlichen Requests sein kann.
 * - Create mit Bild prüft, dass die WebAPI automatisch eine ImageUrl erzeugt und
 *   tatsächlich eine Datei im temporären Image-Verzeichnis schreibt.
 * - Update prüft alle drei Bildzustände: beibehalten, ersetzen und entfernen.
 * - Beim Ersetzen und Löschen wird zusätzlich geprüft, dass veraltete Bilddateien
 *   physisch entfernt werden.
 * - DELETE verifiziert sowohl das Entfernen der Person als auch das Aufräumen
 *   ihres von der API verwalteten Bildes.
 * - Fehlerfälle wie NotFound, Conflict und widersprüchliche Image-Operationen
 *   gehören zum API-Vertrag und werden deshalb ebenfalls automatisiert getestet.
 */
