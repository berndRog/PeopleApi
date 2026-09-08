namespace PeopleApi._3_Core.People._2_Application.Dtos;

// Internal application data used after the Web layer has handled an optional image upload.
public sealed record PersonCreateData(
   string FirstName,
   string LastName,
   string? Email,
   string? Phone,
   string? ImageUrl,
   string? Id
);

/*
 * Lernziele und Didaktik
 * ----------------------
 * - PersonCreateData gehört zur Application-Schicht und enthält deshalb keine
 *   ASP.NET-Core-Typen wie IFormFile.
 * - Die Web-Schicht hat einen optionalen Upload bereits in eine vollständige
 *   ImageUrl übersetzt, bevor der eigentliche People-UseCase aufgerufen wird.
 */
