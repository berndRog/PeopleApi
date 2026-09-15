namespace PeopleApi._2_BuildingBlocks._3_Domain.Enums;

// Describes transport-oriented error categories without referencing ASP.NET Core.
public enum WebErrorStatus {
   None = 0,
   BadRequest,
   Unauthorized,
   Forbidden,
   NotFound,
   Conflict,
   InternalServerError
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Der Core kennt Fehlerkategorien, aber keine konkreten IActionResult-Typen.
 * - Erst die Web-Schicht übersetzt diese Kategorien in HTTP-Statuscodes.
 */
