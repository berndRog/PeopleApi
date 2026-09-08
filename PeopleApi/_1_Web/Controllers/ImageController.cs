using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PeopleApi._1_Web.Common;
using PeopleApi._1_Web.ReadModels;
using PeopleApi._2_BuildingBlocks._3_Domain.Enums;
using PeopleApi._2_BuildingBlocks._3_Domain.Errors;
using PeopleApi._3_Core.Images._1_Ports;
using PeopleApi._3_Core.Images._2_Application.Dtos;

namespace PeopleApi._1_Web.Controllers;

/// <summary>
/// Provides direct upload, download and delete operations for reusable image files.
/// Images are stored exclusively in the file system and not in the database.
/// </summary>
[ApiVersion("1.0")]
[Route("peopleapi/v{version:apiVersion}/images")]
[ApiController]
public sealed class ImageController(
   IImageUseCases useCases
) : ControllerBase {

   /// <summary>
   /// Uploads one image file and creates a new image resource.
   /// </summary>
   /// <remarks>
   /// The request must use multipart/form-data and contain the file field named
   /// <c>file</c>. The response contains a generated server-side file name and an
   /// absolute ImageUrl that can be used directly by HTTP clients such as Coil.
   /// </remarks>
   /// <param name="file">JPEG, PNG or WebP image file.</param>
   /// <param name="ct">Cancellation token for the HTTP request.</param>
   /// <returns>Metadata and the absolute URL of the newly stored image.</returns>
   /// <response code="201">The image was stored successfully.</response>
   /// <response code="400">The file is empty, too large or has an unsupported type.</response>
   /// <response code="500">The image could not be written to the file system.</response>
   [HttpPost(Name = "Images_Create")]
   [Consumes("multipart/form-data")]
   [ProducesResponseType(typeof(ImageReadModel), StatusCodes.Status201Created)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
   public async Task<ActionResult<ImageReadModel>> CreateAsync(
      IFormFile file,
      CancellationToken ct
   ) {
      // ASP.NET Core binds IFormFile from multipart/form-data automatically.
      // Do not add [FromForm] to this IFormFile parameter: Swashbuckle uses the
      // native IFormFile metadata when generating the OpenAPI upload operation.
      await using var stream = file.OpenReadStream();

      // Convert the web-specific IFormFile into an application DTO. The Core
      // layer therefore has no dependency on Microsoft.AspNetCore.Http.
      var upload = new ImageUpload(
         stream,
         file.FileName,
         file.ContentType,
         file.Length
      );

      // The use case validates and stores the image as a physical file.
      var result = await useCases.CreateAsync(upload, ct);
      if (result.IsSuccess) {
         // Build an absolute URL from the current request host. If the server was
         // reached by localhost the URL contains localhost; if it was reached by
         // a WLAN IP, that IP is retained automatically.
         var imageUrl = Url.RouteUrl(
            "Images_GetByFileName",
            new { fileName = result.Value.FileName, version = "1" },
            Request.Scheme
         ) ?? throw new InvalidOperationException("Could not create image URL.");

         // Return only API-facing metadata. The actual image content stays on disk.
         var readModel = new ImageReadModel(
            result.Value.FileName,
            imageUrl,
            result.Value.ContentType,
            result.Value.Length,
            result.Value.OriginalFileName
         );

         // 201 Created points directly to GET /images/{fileName}.
         return CreatedAtRoute(
            "Images_GetByFileName",
            new { fileName = result.Value.FileName, version = "1" },
            readModel
         );
      }

      return ToError(result.Error.Status, result.Error);
   }

   /// <summary>
   /// Downloads a previously stored image file.
   /// </summary>
   /// <param name="fileName">Server-generated image file name.</param>
   /// <param name="ct">Cancellation token for the HTTP request.</param>
   /// <returns>The binary image stream with its matching media type.</returns>
   /// <response code="200">The image was found and is returned as binary content.</response>
   /// <response code="404">No image with this file name exists.</response>
   /// <response code="500">The image could not be read from the file system.</response>
   [HttpGet("{fileName}", Name = "Images_GetByFileName")]
   [ProducesResponseType(StatusCodes.Status200OK)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
   public async Task<IActionResult> GetByFileNameAsync(
      [FromRoute] string fileName,
      CancellationToken ct
   ) {
      // Open the physical file through the application/storage abstraction.
      var result = await useCases.OpenAsync(fileName, ct);
      if (result.IsSuccess) {
         // Stream the file to the client instead of loading the full image into RAM.
         // Range processing also allows clients to request byte ranges if needed.
         return File(
            result.Value.Stream,
            result.Value.ContentType,
            enableRangeProcessing: true
         );
      }

      return ToError(result.Error.Status, result.Error);
   }

   /// <summary>
   /// Deletes one image file from the file system.
   /// </summary>
   /// <param name="fileName">Server-generated image file name.</param>
   /// <param name="ct">Cancellation token for the HTTP request.</param>
   /// <response code="204">The image was deleted.</response>
   /// <response code="404">No image with this file name exists.</response>
   /// <response code="500">The image could not be deleted from the file system.</response>
   [HttpDelete("{fileName}", Name = "Images_Delete")]
   [ProducesResponseType(StatusCodes.Status204NoContent)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
   public async Task<IActionResult> DeleteAsync(
      [FromRoute] string fileName,
      CancellationToken ct
   ) {
      // The file name is the technical identifier of the image resource.
      var result = await useCases.DeleteAsync(fileName, ct);
      if (result.IsSuccess)
         return NoContent();

      return ToError(result.Error.Status, result.Error);
   }

   private ObjectResult ToError(
      WebErrorStatus status,
      DomainError error
   ) {
      // Keep ProblemDetails formatting identical to PeopleController.
      var problem = DomainProblemDetailsFactory.FromDomainError(error, HttpContext);
      return status switch {
         WebErrorStatus.NotFound => NotFound(problem),
         WebErrorStatus.InternalServerError =>
            StatusCode(StatusCodes.Status500InternalServerError, problem),
         _ => BadRequest(problem)
      };
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Images bilden eine eigenständige REST-Ressource und werden ausschließlich
 *   im Dateisystem gespeichert; EF Core ist daran nicht beteiligt.
 * - IFormFile gehört nur zur Web-Schicht. Der Controller übersetzt es in ein
 *   neutrales ImageUpload-DTO für den Core.
 * - POST liefert eine vollständige ImageUrl. Android/Coil kann diese URL direkt
 *   verwenden; im AVD wird nur der Host localhost zu 10.0.2.2 substituiert.
 * - Der Dateiname identifiziert die Ressource für GET und DELETE.
 * - Die Trennung erlaubt später beispielsweise Cars dieselbe /images-API zu nutzen.
 * - XML-Dokumentationskommentare beschreiben Zweck, Parameter und HTTP-Responses
 *   direkt im erzeugten Swagger/OpenAPI-Dokument.
 */
