using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PeopleApi._1_Web.Common;
using PeopleApi._1_Web.Dtos;
using PeopleApi._2_BuildingBlocks._3_Domain.Enums;
using PeopleApi._2_BuildingBlocks._3_Domain.Errors;
using PeopleApi._3_Core.Images._1_Ports;
using PeopleApi._3_Core.Images._2_Application.Dtos;
using PeopleApi._3_Core.People._1_Ports;
using PeopleApi._3_Core.People._2_Application.Dtos;

namespace PeopleApi._1_Web.Controllers;

/// <summary>
/// Provides CRUD operations for people. Optional profile images are orchestrated
/// server-side so clients do not need separate image upload/delete requests.
/// </summary>
[ApiVersion("1.0")]
[Route("peopleapi/v{version:apiVersion}/people")]
[ApiController]
public sealed class PeopleController(
   IPersonReadModel readModel,
   IPersonUseCases useCases,
   IImageUseCases imageUseCases,
   ILogger<PeopleController> logger
) : ControllerBase {

   /// <summary>
   /// Returns all people.
   /// </summary>
   /// <param name="ct">Cancellation token for the HTTP request.</param>
   /// <returns>All people ordered by the read model.</returns>
   /// <response code="200">The people were returned successfully.</response>
   /// <response code="500">The people could not be read.</response>
   [HttpGet(Name = "People_GetAll")]
   [ProducesResponseType(typeof(IReadOnlyList<PersonDto>), StatusCodes.Status200OK)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
   public async Task<ActionResult<IReadOnlyList<PersonDto>>> GetAllAsync(
      CancellationToken ct
   ) {
      // Read queries are delegated to the read model and do not track entities.
      var result = await readModel.SelectAllAsync(ct);

      // Successful Result values become 200 OK responses.
      if (result.IsSuccess)
         return Ok(result.Value);

      // Domain/application errors are translated only at the HTTP boundary.
      return ToError(result.Error.Status, result.Error);
   }

   /// <summary>
   /// Returns one person identified by its UUID.
   /// </summary>
   /// <param name="id">UUID of the requested person.</param>
   /// <param name="ct">Cancellation token for the HTTP request.</param>
   /// <returns>The requested person.</returns>
   /// <response code="200">The person was found.</response>
   /// <response code="404">No person with the supplied UUID exists.</response>
   [HttpGet("{id:guid}", Name = "People_GetById")]
   [ProducesResponseType(typeof(PersonDto), StatusCodes.Status200OK)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
   public async Task<ActionResult<PersonDto>> GetByIdAsync(
      [FromRoute] Guid id,
      CancellationToken ct
   ) {
      // The route constraint already guarantees a syntactically valid Guid.
      var result = await readModel.FindByIdAsync(id, ct);
      if (result.IsSuccess)
         return Ok(result.Value);

      return ToError(result.Error.Status, result.Error);
   }

   /// <summary>
   /// Creates a new person and optionally stores an uploaded profile image.
   /// </summary>
   /// <remarks>
   /// The request uses multipart/form-data. When Image is supplied, the API first
   /// stores the image file, creates its absolute URL and stores that URL with the
   /// person. If creating the person fails, the newly stored image is removed again.
   /// </remarks>
   /// <param name="dto">Person data and optional profile image.</param>
   /// <param name="ct">Cancellation token for the HTTP request.</param>
   /// <returns>The created person including its generated ImageUrl when an image was uploaded.</returns>
   /// <response code="201">The person was created.</response>
   /// <response code="400">Person or image data is invalid.</response>
   /// <response code="409">The supplied person UUID already exists.</response>
   /// <response code="500">The image or person could not be stored.</response>
   [HttpPost(Name = "People_Create")]
   [Consumes("multipart/form-data")]
   [ProducesResponseType(typeof(PersonDto), StatusCodes.Status201Created)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
   public async Task<ActionResult<PersonDto>> CreateAsync(
      [FromForm] PersonCreateDto dto,
      CancellationToken ct
   ) {
      string? newImageFileName = null;
      string? imageUrl = null;

      // Store an optional image before creating the person. The People resource
      // itself still stores only the resulting absolute URL.
      if (dto.Image is not null) {
         var imageResult = await StoreImageAsync(dto.Image, ct);
         if (imageResult.IsFailure)
            return ToError(imageResult.Error.Status, imageResult.Error);

         newImageFileName = imageResult.Value.FileName;
         imageUrl = BuildImageUrl(newImageFileName);
      }

      // Convert the Web DTO into transport-neutral application data. IFormFile
      // does not cross the Web/Core boundary.
      var data = new PersonCreateData(
         dto.FirstName,
         dto.LastName,
         dto.Email,
         dto.Phone,
         imageUrl,
         dto.Id
      );

      // Creation rules and People persistence live in the People use case.
      var result = await useCases.CreateAsync(data, ct);
      if (result.IsSuccess) {
         // 201 Created includes a Location header pointing to the new resource.
         return CreatedAtRoute(
            "People_GetById",
            new { id = result.Value.Id, version = "1" },
            result.Value
         );
      }

      // Compensate the file-system write when the database operation fails.
      if (newImageFileName is not null)
         await DeleteImageQuietlyAsync(newImageFileName, ct);

      return ToError(result.Error.Status, result.Error);
   }

   /// <summary>
   /// Updates an existing person and optionally replaces or removes its profile image.
   /// </summary>
   /// <remarks>
   /// Image == null and RemoveImage == false keeps the existing image.
   /// Supplying Image stores a new file and removes the old local image after the
   /// People update succeeds. RemoveImage == true removes the current image.
   /// Supplying Image and RemoveImage == true at the same time is invalid.
   /// </remarks>
   /// <param name="id">UUID of the person to update.</param>
   /// <param name="dto">New person data plus the optional image operation.</param>
   /// <param name="ct">Cancellation token for the HTTP request.</param>
   /// <returns>The updated person.</returns>
   /// <response code="200">The person was updated.</response>
   /// <response code="400">Person/image data or the requested image operation is invalid.</response>
   /// <response code="404">The person does not exist.</response>
   /// <response code="500">The image or person could not be stored.</response>
   [HttpPut("{id:guid}", Name = "People_Update")]
   [Consumes("multipart/form-data")]
   [ProducesResponseType(typeof(PersonDto), StatusCodes.Status200OK)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
   public async Task<ActionResult<PersonDto>> UpdateAsync(
      [FromRoute] Guid id,
      [FromForm] PersonUpdateDto dto,
      CancellationToken ct
   ) {
      // A request cannot replace and remove the image at the same time.
      if (dto.Image is not null && dto.RemoveImage)
         return InvalidImageOperation();

      // Read the existing representation before any file is written. Besides the
      // NotFound check, this gives us the previous ImageUrl for later cleanup.
      var currentResult = await readModel.FindByIdAsync(id, ct);
      if (currentResult.IsFailure)
         return ToError(currentResult.Error.Status, currentResult.Error);

      var current = currentResult.Value;
      var nextImageUrl = current.ImageUrl;
      string? newImageFileName = null;

      if (dto.Image is not null) {
         // Store the replacement first so the old image remains available if the
         // upload or the following database update fails.
         var imageResult = await StoreImageAsync(dto.Image, ct);
         if (imageResult.IsFailure)
            return ToError(imageResult.Error.Status, imageResult.Error);

         newImageFileName = imageResult.Value.FileName;
         nextImageUrl = BuildImageUrl(newImageFileName);
      }
      else if (dto.RemoveImage) {
         // Explicit removal writes null into the People resource.
         nextImageUrl = null;
      }

      // The application layer receives the fully resolved image state.
      var data = new PersonUpdateData(
         dto.FirstName,
         dto.LastName,
         dto.Email,
         dto.Phone,
         nextImageUrl
      );

      var result = await useCases.UpdateAsync(id, data, ct);
      if (result.IsFailure) {
         // The new image is not referenced when the People update failed.
         if (newImageFileName is not null)
            await DeleteImageQuietlyAsync(newImageFileName, ct);

         return ToError(result.Error.Status, result.Error);
      }

      // Delete the old local image only after the Person now references the new
      // image (or no image). A cleanup failure must not roll back valid People data.
      if (dto.Image is not null || dto.RemoveImage)
         await DeleteReferencedImageQuietlyAsync(current.ImageUrl, ct);

      return Ok(result.Value);
   }

   /// <summary>
   /// Deletes a person and removes its locally managed profile image, if present.
   /// </summary>
   /// <param name="id">UUID of the person to delete.</param>
   /// <param name="ct">Cancellation token for the HTTP request.</param>
   /// <response code="204">The person was deleted.</response>
   /// <response code="404">The person does not exist.</response>
   /// <response code="500">The person could not be deleted.</response>
   [HttpDelete("{id:guid}", Name = "People_Delete")]
   [ProducesResponseType(StatusCodes.Status204NoContent)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
   public async Task<IActionResult> DeleteAsync(
      [FromRoute] Guid id,
      CancellationToken ct
   ) {
      // Read the ImageUrl before deleting the database row. The URL is needed for
      // file cleanup after the People deletion has committed successfully.
      var currentResult = await readModel.FindByIdAsync(id, ct);
      if (currentResult.IsFailure)
         return ToError(currentResult.Error.Status, currentResult.Error);

      // Delete the People resource first. If this fails, the image remains intact.
      var result = await useCases.DeleteAsync(id, ct);
      if (result.IsFailure)
         return ToError(result.Error.Status, result.Error);

      // Remove only images that can be recognized as resources of our /images API.
      await DeleteReferencedImageQuietlyAsync(currentResult.Value.ImageUrl, ct);
      return NoContent();
   }

   // Convert the web upload into the Core-neutral stream DTO used by Image use cases.
   private async Task<PeopleApi._2_BuildingBlocks.Result<ImageDto>> StoreImageAsync(
      IFormFile file,
      CancellationToken ct
   ) {
      await using var stream = file.OpenReadStream();
      var upload = new ImageUpload(
         stream,
         file.FileName,
         file.ContentType,
         file.Length
      );

      return await imageUseCases.CreateAsync(upload, ct);
   }

   // Create the same absolute URL that the standalone ImageController returns.
   private string BuildImageUrl(string fileName) =>
      Url.RouteUrl(
         "Images_GetByFileName",
         new { fileName, version = "1" },
         Request.Scheme
      ) ?? throw new InvalidOperationException("Could not create image URL.");

   // Delete a newly written image during compensation. Cleanup problems are logged
   // because the primary People error is more important to the caller.
   private async Task DeleteImageQuietlyAsync(
      string fileName,
      CancellationToken ct
   ) {
      var deleteResult = await imageUseCases.DeleteAsync(fileName, ct);
      if (deleteResult.IsFailure) {
         logger.LogWarning(
            "Could not clean up image {FileName}: {ErrorCode}",
            fileName,
            deleteResult.Error.Code
         );
      }
   }

   // Extract the file name only from URLs that clearly address our /images route.
   // External image URLs are references only and must never trigger local deletion.
   private async Task DeleteReferencedImageQuietlyAsync(
      string? imageUrl,
      CancellationToken ct
   ) {
      if (!TryGetLocalImageFileName(imageUrl, out var fileName))
         return;

      var deleteResult = await imageUseCases.DeleteAsync(fileName, ct);
      if (deleteResult.IsFailure) {
         logger.LogWarning(
            "Could not delete previously referenced image {FileName}: {ErrorCode}",
            fileName,
            deleteResult.Error.Code
         );
      }
   }

   private static bool TryGetLocalImageFileName(
      string? imageUrl,
      out string fileName
   ) {
      fileName = string.Empty;

      if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
         return false;

      var segments = uri.AbsolutePath
         .Split('/', StringSplitOptions.RemoveEmptyEntries);

      // A local managed image URL always ends with /images/{fileName}.
      if (segments.Length < 2 ||
          !string.Equals(segments[^2], "images", StringComparison.OrdinalIgnoreCase)) {
         return false;
      }

      var candidate = Uri.UnescapeDataString(segments[^1]);

      // Accept only a plain server-generated file name. ImageFileStorageFs creates
      // names as a 32-character Guid plus one supported image extension. This
      // prevents arbitrary external URLs from being treated as local files.
      if (string.IsNullOrWhiteSpace(candidate) ||
          !string.Equals(candidate, Path.GetFileName(candidate), StringComparison.Ordinal)) {
         return false;
      }

      var extension = Path.GetExtension(candidate).ToLowerInvariant();
      var idPart = Path.GetFileNameWithoutExtension(candidate);
      var supportedExtension = extension is ".jpg" or ".jpeg" or ".png" or ".webp";

      if (!supportedExtension || !Guid.TryParseExact(idPart, "N", out _))
         return false;

      fileName = candidate;
      return true;
   }

   private BadRequestObjectResult InvalidImageOperation() {
      var problem = new ProblemDetails {
         Status = StatusCodes.Status400BadRequest,
         Title = "Bad Request",
         Detail = "Image and RemoveImage cannot be used at the same time.",
         Instance = HttpContext.Request.Path
      };
      problem.Extensions["code"] = "person.image_operation_invalid";
      return BadRequest(problem);
   }

   private ObjectResult ToError(
      WebErrorStatus status,
      DomainError error
   ) {
      // Create one consistent ProblemDetails body for all controller methods.
      var problem = DomainProblemDetailsFactory.FromDomainError(error, HttpContext);
      return status switch {
         WebErrorStatus.NotFound => NotFound(problem),
         WebErrorStatus.Conflict => Conflict(problem),
         WebErrorStatus.InternalServerError =>
            StatusCode(StatusCodes.Status500InternalServerError, problem),
         _ => BadRequest(problem)
      };
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Der Client startet weiterhin ausschließlich People-CRUD. Die API orchestriert
 *   einen optionalen Image-Upload beziehungsweise das Entfernen alter Bilddateien.
 * - IFormFile bleibt in der Web-Schicht. Der Core erhält nur normale Daten und die
 *   bereits erzeugte vollständige ImageUrl.
 * - Beim Create wird ein neu gespeichertes Bild wieder gelöscht, falls das
 *   anschließende Speichern der Person fehlschlägt (Kompensation).
 * - Beim Update wird zuerst das neue Bild gespeichert, dann die Person geändert
 *   und erst danach das alte Bild gelöscht. So bleibt das alte Bild bei Fehlern
 *   möglichst lange verfügbar.
 * - Beim Delete wird zuerst die Person aus der Datenbank gelöscht und anschließend
 *   eine von dieser API verwaltete Bilddatei aufgeräumt.
 * - Externe ImageUrls werden niemals als lokale Dateien interpretiert oder gelöscht.
 * - XML-Dokumentationskommentare und ProducesResponseType machen den HTTP-Vertrag
 *   einschließlich Statuscodes direkt in Swagger sichtbar.
 */
