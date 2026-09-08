using PeopleApi._3_Core.Images._2_Application.Dtos;

namespace PeopleApi._3_Core.People._2_Application.Dtos;

// Internal application data for the complete People-update use case.
public sealed record PersonUpdateData(
   string FirstName,
   string LastName,
   string? Email,
   string? Phone,
   ImageUpload? Image,
   bool RemoveImage,
   string ImageBaseUrl
);

/*
 * Lernziele und Didaktik
 * ----------------------
 * - PersonUpdateData transportiert die drei möglichen Bildoperationen in die
 *   Application-Schicht: beibehalten, ersetzen oder entfernen.
 * - Die Web-Schicht übersetzt nur IFormFile in ImageUpload; die Interpretation
 *   von Image und RemoveImage findet im Update-UseCase statt.
 * - Dadurch bleibt die fachliche Reihenfolge von Datei- und Datenbankoperationen
 *   außerhalb des Controllers.
 */
