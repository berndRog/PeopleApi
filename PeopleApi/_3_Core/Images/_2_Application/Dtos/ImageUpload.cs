namespace PeopleApi._3_Core.Images._2_Application.Dtos;

// Web-neutral representation of a multipart upload passed into the Core.
public sealed record ImageUpload(
   Stream Content,
   string FileName,
   string ContentType,
   long Length
);

/*
 * Lernziele und Didaktik
 * ----------------------
 * - IFormFile bleibt im Controller; der Core arbeitet nur mit Stream und Metadaten.
 * - Dadurch entsteht keine Abhängigkeit des Application-Cores von ASP.NET Core.
 */
