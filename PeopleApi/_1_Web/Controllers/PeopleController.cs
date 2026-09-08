using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PeopleApi._1_Web.Common;
using PeopleApi._1_Web.Dtos;
using PeopleApi._2_BuildingBlocks._3_Domain.Enums;
using PeopleApi._2_BuildingBlocks._3_Domain.Errors;
using PeopleApi._3_Core.Images._2_Application.Dtos;
using PeopleApi._3_Core.People._1_Ports;
using PeopleApi._3_Core.People._2_Application.Dtos;

namespace PeopleApi._1_Web.Controllers;

/// <summary>
/// Provides CRUD operations for people. Optional profile images are accepted as
/// part of the People request, while their orchestration is delegated to the
/// application use cases.
/// </summary>
[ApiVersion("1.0")]
[Route("peopleapi/v{version:apiVersion}/people")]
[ApiController]
public sealed class PeopleController(
   IPersonReadModel readModel,
   IPersonUseCases useCases
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

      // Translate the application Result only at the HTTP boundary.
      if (result.IsSuccess)
         return Ok(result.Value);

      return ToError(result.Error.Status, result.Error);
   }

   /// <summary>
   /// Returns the number of stored people.
   /// </summary>
   /// <param name="ct">Cancellation token for the HTTP request.</param>
   /// <returns>The number of stored people.</returns>
   /// <response code="200">The count was returned successfully.</response>
   /// <response code="500">The count could not be read.</response>
   [HttpGet("count", Name = "People_Count")]
   [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
   public async Task<ActionResult<int>> CountAsync(
      CancellationToken ct
   ) {
      var result = await readModel.CountAsync(ct);
      if (result.IsSuccess)
         return Ok(result.Value);

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
   /// The request uses multipart/form-data. The controller converts an optional
   /// IFormFile into a web-neutral ImageUpload. The People use case stores the image,
   /// creates its absolute URL and compensates the file write if People creation fails.
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
      // IFormFile is a Web concern. Keep the stream alive until the awaited use case
      // has finished, then dispose it together with the HTTP action scope.
      using var imageStream = dto.Image?.OpenReadStream();
      var image = ToImageUpload(dto.Image, imageStream);

      // Pass only transport-neutral application data into the Core. The base URL is
      // derived from the current request because the stored Person needs an absolute URL.
      var data = new PersonCreateData(
         dto.FirstName,
         dto.LastName,
         dto.Email,
         dto.Phone,
         image,
         BuildImageBaseUrl(),
         dto.Id
      );

      var result = await useCases.CreateAsync(data, ct);
      if (result.IsFailure)
         return ToError(result.Error.Status, result.Error);

      // 201 Created includes a Location header pointing to the new resource.
      return CreatedAtRoute(
         "People_GetById",
         new { id = result.Value.Id, version = "1" },
         result.Value
      );
   }

   /// <summary>
   /// Updates an existing person and optionally replaces or removes its profile image.
   /// </summary>
   /// <remarks>
   /// Image == null and RemoveImage == false keeps the existing image.
   /// Supplying Image replaces the old image after the People update succeeds.
   /// RemoveImage == true removes the current image. Supplying Image and
   /// RemoveImage == true at the same time is rejected by the People use case.
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
      // Convert only the HTTP upload type. The three image states are interpreted by
      // PersonUcUpdate, not by the controller.
      using var imageStream = dto.Image?.OpenReadStream();
      var image = ToImageUpload(dto.Image, imageStream);

      var data = new PersonUpdateData(
         dto.FirstName,
         dto.LastName,
         dto.Email,
         dto.Phone,
         image,
         dto.RemoveImage,
         BuildImageBaseUrl()
      );

      var result = await useCases.UpdateAsync(id, data, ct);
      if (result.IsFailure)
         return ToError(result.Error.Status, result.Error);

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
      // The use case performs the complete delete flow including optional image cleanup.
      var result = await useCases.DeleteAsync(id, BuildImageBaseUrl(), ct);
      if (result.IsFailure)
         return ToError(result.Error.Status, result.Error);

      return NoContent();
   }

   // Translate ASP.NET Core's upload abstraction into the Core-neutral stream DTO.
   private static ImageUpload? ToImageUpload(
      IFormFile? file,
      Stream? stream
   ) {
      if (file is null || stream is null)
         return null;

      return new ImageUpload(
         stream,
         file.FileName,
         file.ContentType,
         file.Length
      );
   }

   // Build only the public base URL. The use case appends the generated file name.
   // The host may therefore be localhost, a WLAN address or a real server name.
   private string BuildImageBaseUrl() {
      var version = RouteData.Values["version"]?.ToString() ?? "1";
      return $"{Request.Scheme}://{Request.Host}{Request.PathBase}/peopleapi/v{version}/images";
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
 * - Der PeopleController ist ein HTTP-Adapter: Er bindet Route/Form-Daten,
 *   übersetzt IFormFile in ImageUpload und erzeugt HTTP-Responses.
 * - Lesende Operationen wie GetAll, Count und GetById werden direkt an das
 *   IPersonReadModel delegiert.
 * - Der Controller entscheidet nicht über die fachliche Reihenfolge von People-
 *   und Image-Operationen. Diese Orchestrierung liegt in den People-UseCases.
 * - IFormFile bleibt damit vollständig in der Web-Schicht; die Application kennt
 *   nur Stream + Metadaten sowie die öffentliche Image-Basis-URL.
 * - Create, Update und Delete werden jeweils als ein Anwendungsfall ausgeführt,
 *   obwohl intern Datenbank- und Dateioperationen kombiniert werden.
 * - XML-Dokumentationskommentare und ProducesResponseType machen den HTTP-Vertrag
 *   einschließlich Statuscodes direkt in Swagger sichtbar.
 */
