using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PeopleApi._1_Web.Common;
using PeopleApi._2_BuildingBlocks._3_Domain.Enums;
using PeopleApi._2_BuildingBlocks._3_Domain.Errors;
using PeopleApi._3_Core.People._1_Ports;
using PeopleApi._3_Core.People._2_Application.Dtos;

namespace PeopleApi._1_Web.Controllers;

/// <summary>
/// Provides JSON-based CRUD operations for people.
/// </summary>
[ApiVersion("1.0")]
[Route("peopleapi/v{version:apiVersion}/people")]
[ApiController]
public sealed class PeopleController(
   IPersonReadModel readModel,
   IPersonUseCases useCases
) : ControllerBase {

   /// <summary>Returns all people.</summary>
   [HttpGet(Name = "People_GetAll")]
   [ProducesResponseType(typeof(IReadOnlyList<PersonDto>), StatusCodes.Status200OK)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
   public async Task<ActionResult<IReadOnlyList<PersonDto>>> GetAllAsync(
      CancellationToken ct
   ) {
      var result = await readModel.SelectAllAsync(ct);
      if (result.IsSuccess)
         return Ok(result.Value);

      return ToError(result.Error.Status, result.Error);
   }

   /// <summary>Returns the number of stored people.</summary>
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

   /// <summary>Returns one person identified by its UUID.</summary>
   [HttpGet("{id:guid}", Name = "People_GetById")]
   [ProducesResponseType(typeof(PersonDto), StatusCodes.Status200OK)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
   public async Task<ActionResult<PersonDto>> GetByIdAsync(
      [FromRoute] Guid id,
      CancellationToken ct
   ) {
      var result = await readModel.FindByIdAsync(id, ct);
      if (result.IsSuccess)
         return Ok(result.Value);

      return ToError(result.Error.Status, result.Error);
   }

   /// <summary>Creates a person from one JSON object.</summary>
   /// <remarks>
   /// ImageUrl is stored as an optional string. This endpoint does not read,
   /// upload or otherwise process an image file.
   /// </remarks>
   [HttpPost(Name = "People_Create")]
   [Consumes("application/json")]
   [ProducesResponseType(typeof(PersonDto), StatusCodes.Status201Created)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
   public async Task<ActionResult<PersonDto>> CreateAsync(
      [FromBody] PersonDto dto,
      CancellationToken ct
   ) {
      var result = await useCases.CreateAsync(dto, ct);
      if (result.IsFailure)
         return ToError(result.Error.Status, result.Error);

      return CreatedAtRoute(
         "People_GetById",
         new { id = result.Value.Id, version = "1" },
         result.Value
      );
   }

   /// <summary>Replaces the editable properties of an existing person.</summary>
   /// <remarks>
   /// The UUID in the route identifies the resource. ImageUrl is handled like
   /// Email or Phone: it is stored as a nullable string.
   /// </remarks>
   [HttpPut("{id:guid}", Name = "People_Update")]
   [Consumes("application/json")]
   [ProducesResponseType(typeof(PersonDto), StatusCodes.Status200OK)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
   public async Task<ActionResult<PersonDto>> UpdateAsync(
      [FromRoute] Guid id,
      [FromBody] PersonDto dto,
      CancellationToken ct
   ) {
      var result = await useCases.UpdateAsync(id, dto, ct);
      if (result.IsFailure)
         return ToError(result.Error.Status, result.Error);

      return Ok(result.Value);
   }

   /// <summary>Deletes one person.</summary>
   [HttpDelete("{id:guid}", Name = "People_Delete")]
   [ProducesResponseType(StatusCodes.Status204NoContent)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
   public async Task<IActionResult> DeleteAsync(
      [FromRoute] Guid id,
      CancellationToken ct
   ) {
      var result = await useCases.DeleteAsync(id, ct);
      if (result.IsFailure)
         return ToError(result.Error.Status, result.Error);

      return NoContent();
   }

   private ObjectResult ToError(
      WebErrorStatus status,
      DomainError error
   ) {
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
 * - Der Controller beschreibt einen einfachen JSON-Vertrag mit @Body-ähnlicher
 *   Modellbindung über [FromBody].
 * - PersonDto ist das Transportmodell für Requests und Responses. Die Domain-
 *   Entität Person bleibt davon getrennt.
 * - ImageUrl ist lediglich ein optionales String-Attribut. Die API kennt weder
 *   IFormFile noch MultipartBody und greift auf keine Bilddatei zu.
 * - Die Route bestimmt beim Update die zu ändernde Person; die Id im JSON-Objekt
 *   wird nicht als zweite Ressourcenadresse verwendet.
 */
