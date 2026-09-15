using Microsoft.AspNetCore.Mvc;
using PeopleApi._2_BuildingBlocks._3_Domain.Enums;
using PeopleApi._2_BuildingBlocks._3_Domain.Errors;

namespace PeopleApi._1_Web.Common;

public static class DomainProblemDetailsFactory {
   public static ProblemDetails FromDomainError(
      DomainError error,
      HttpContext httpContext
   ) {
      // Translate the domain-oriented error into the standardized HTTP
      // ProblemDetails representation used by the Web layer.
      var problem = new ProblemDetails {
         Status = ToStatusCode(error.Status),
         Title = ToTitle(error.Status),
         Detail = error.Message,
         Instance = httpContext.Request.Path
      };

      // Preserve the stable application error code for machine-readable clients.
      problem.Extensions["code"] = error.Code;
      return problem;
   }

   // Map application status categories to concrete HTTP status codes.
   private static int ToStatusCode(WebErrorStatus status) => status switch {
      WebErrorStatus.BadRequest => StatusCodes.Status400BadRequest,
      WebErrorStatus.Unauthorized => StatusCodes.Status401Unauthorized,
      WebErrorStatus.Forbidden => StatusCodes.Status403Forbidden,
      WebErrorStatus.NotFound => StatusCodes.Status404NotFound,
      WebErrorStatus.Conflict => StatusCodes.Status409Conflict,
      WebErrorStatus.InternalServerError => StatusCodes.Status500InternalServerError,
      _ => StatusCodes.Status400BadRequest
   };

   // Provide a short human-readable title for the ProblemDetails response.
   private static string ToTitle(WebErrorStatus status) => status switch {
      WebErrorStatus.BadRequest => "Bad Request",
      WebErrorStatus.Unauthorized => "Unauthorized",
      WebErrorStatus.Forbidden => "Forbidden",
      WebErrorStatus.NotFound => "Not Found",
      WebErrorStatus.Conflict => "Conflict",
      WebErrorStatus.InternalServerError => "Internal Server Error",
      _ => "Request failed"
   };
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Domänenfehler enthalten keine ASP.NET-Core-Typen und bleiben dadurch vom
 *   Transportprotokoll unabhängig.
 * - Erst die Web-Schicht übersetzt einen DomainError in HTTP ProblemDetails.
 * - Der zusätzliche Fehlercode erlaubt Clients eine stabile technische
 *   Auswertung, während Message und Title für Menschen lesbar bleiben.
 */
