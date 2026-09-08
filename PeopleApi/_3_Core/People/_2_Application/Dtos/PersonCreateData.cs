using PeopleApi._3_Core.Images._2_Application.Dtos;

namespace PeopleApi._3_Core.People._2_Application.Dtos;

// Internal application data for the complete People-create use case.
public sealed record PersonCreateData(
   string FirstName,
   string LastName,
   string? Email,
   string? Phone,
   ImageUpload? Image,
   string ImageBaseUrl,
   string? Id
);

/*
 * Lernziele und Didaktik
 * ----------------------
 * - PersonCreateData gehört zur Application-Schicht und enthält deshalb keine
 *   ASP.NET-Core-Typen wie IFormFile.
 * - Ein optionaler Upload wird als ImageUpload (Stream + Metadaten) übergeben.
 * - Die Web-Schicht liefert nur die öffentliche Image-Basis-URL; der Create-
 *   UseCase entscheidet selbst, ob ein Bild gespeichert und welche ImageUrl
 *   anschließend in der Person abgelegt wird.
 */
