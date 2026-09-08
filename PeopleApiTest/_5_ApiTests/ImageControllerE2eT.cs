using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AwesomeAssertions;
using PeopleApi._1_Web.ReadModels;
using PeopleApiTest.TestInfrastructure;

namespace PeopleApiTest._5_ApiTests;

public sealed class ImageControllerE2eT : TestBaseEndToEnd {
   private const string Url = "/peopleapi/v1/images";
   private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

   // Small valid 1x1 PNG used to exercise real binary file I/O.
   private static readonly byte[] PngBytes = Convert.FromBase64String(
      "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9WlH0wAAAABJRU5ErkJggg=="
   );

   [Fact]
   public async Task CreateAsync_storesFileAndReturnsAbsoluteUrl() {
      // Arrange
      using var content = CreateMultipart(PngBytes, "portrait.png", "image/png");

      // Act
      var response = await Client.PostAsync(Url, content, _ct);
      var image = await response.Content
         .ReadFromJsonAsync<ImageReadModel>(_ct);

      // Assert
      response.StatusCode.Should().Be(HttpStatusCode.Created);
      image.Should().NotBeNull();
      image!.FileName.Should().EndWith(".png");
      image.OriginalFileName.Should().Be("portrait.png");
      image.ContentType.Should().Be("image/png");
      image.Length.Should().Be(PngBytes.Length);

      // TestServer uses localhost. The production server may instead expose a
      // WLAN IP; ImageController always uses the host of the current request.
      var imageUri = new Uri(image.ImageUrl);
      imageUri.IsAbsoluteUri.Should().BeTrue();
      imageUri.Host.Should().Be("localhost");
      imageUri.AbsolutePath.Should().EndWith($"/{image.FileName}");

      // Verify the infrastructure side effect: a real file exists on disk.
      var filePath = Path.Combine(Factory.ImageDirectory, image.FileName);
      File.Exists(filePath).Should().BeTrue();
      File.ReadAllBytes(filePath).Should().Equal(PngBytes);
   }

   [Fact]
   public async Task GetByFileNameAsync_returnsStoredBytes() {
      // Arrange
      var image = await UploadPngAsync();

      // Act
      var response = await Client.GetAsync($"{Url}/{image.FileName}", _ct);
      var actualBytes = await response.Content.ReadAsByteArrayAsync(_ct);

      // Assert
      response.StatusCode.Should().Be(HttpStatusCode.OK);
      response.Content.Headers.ContentType?.MediaType.Should().Be("image/png");
      actualBytes.Should().Equal(PngBytes);
   }

   [Fact]
   public async Task DeleteAsync_removesStoredFile() {
      // Arrange
      var image = await UploadPngAsync();
      var filePath = Path.Combine(Factory.ImageDirectory, image.FileName);
      File.Exists(filePath).Should().BeTrue();

      // Act
      var deleteResponse = await Client.DeleteAsync($"{Url}/{image.FileName}", _ct);
      var getResponse = await Client.GetAsync($"{Url}/{image.FileName}", _ct);

      // Assert
      deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
      File.Exists(filePath).Should().BeFalse();
      getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
   }

   [Fact]
   public async Task GetByFileNameAsync_unknownFile_returnsNotFound() {
      // Arrange
      const string missingFile = "00000000000000000000000000000000.png";

      // Act
      var response = await Client.GetAsync($"{Url}/{missingFile}", _ct);

      // Assert
      response.StatusCode.Should().Be(HttpStatusCode.NotFound);
   }

   [Fact]
   public async Task DeleteAsync_unknownFile_returnsNotFound() {
      // Arrange
      const string missingFile = "00000000000000000000000000000000.png";

      // Act
      var response = await Client.DeleteAsync($"{Url}/{missingFile}", _ct);

      // Assert
      response.StatusCode.Should().Be(HttpStatusCode.NotFound);
   }

   [Fact]
   public async Task CreateAsync_unsupportedType_returnsBadRequestAndStoresNothing() {
      // Arrange
      var bytes = new byte[] { 1, 2, 3, 4 };
      using var content = CreateMultipart(bytes, "payload.exe", "application/octet-stream");

      // Act
      var response = await Client.PostAsync(Url, content, _ct);

      // Assert
      response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
      Directory.GetFiles(Factory.ImageDirectory).Should().BeEmpty();
   }

   [Fact]
   public async Task CreateAsync_fileTooLarge_returnsBadRequestAndStoresNothing() {
      // Arrange
      // The test factory configures MaxFileSizeBytes = 1024.
      var bytes = new byte[1025];
      using var content = CreateMultipart(bytes, "large.png", "image/png");

      // Act
      var response = await Client.PostAsync(Url, content, _ct);

      // Assert
      response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
      Directory.GetFiles(Factory.ImageDirectory).Should().BeEmpty();
   }

   private async Task<ImageReadModel> UploadPngAsync() {
      // Upload through the public API so later GET/DELETE tests start from the same
      // state a real client would create.
      using var content = CreateMultipart(PngBytes, "portrait.png", "image/png");
      var response = await Client.PostAsync(Url, content, _ct);
      response.StatusCode.Should().Be(HttpStatusCode.Created);

      var image = await response.Content.ReadFromJsonAsync<ImageReadModel>(_ct);
      image.Should().NotBeNull();
      return image!;
   }

   private static MultipartFormDataContent CreateMultipart(
      byte[] bytes,
      string fileName,
      string contentType
   ) {
      // Build the same multipart/form-data body that Swagger or Android sends.
      var form = new MultipartFormDataContent();
      var fileContent = new ByteArrayContent(bytes);
      fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

      // The form field name must match ImageController's IFormFile parameter.
      form.Add(fileContent, "file", fileName);
      return form;
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Diese Tests führen echtes Datei-I/O in einem temporären Verzeichnis aus.
 * - POST prüft nicht nur den HTTP-Status, sondern auch den tatsächlich erzeugten
 *   Dateinamen, die absolute ImageUrl und die gespeicherten Binärdaten.
 * - GET verifiziert Content-Type und Byte-Inhalt; DELETE verifiziert sowohl den
 *   HTTP-Status als auch das physische Entfernen der Datei.
 * - Fehlerfälle demonstrieren die Whitelist und die konfigurierte Dateigröße.
 * - Für Images wird keinerlei Datenbank benötigt oder vorbereitet.
 */
