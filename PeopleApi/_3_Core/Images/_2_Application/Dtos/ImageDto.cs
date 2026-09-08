namespace PeopleApi._3_Core.Images._2_Application.Dtos;

// Metadata returned by the file-storage/application layer after a successful upload.
public sealed record ImageDto(
   string FileName,
   string ContentType,
   long Length,
   string OriginalFileName
);

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Der Core kennt Dateimetadaten, aber noch keine HTTP-URL.
 * - Die absolute URL wird erst in der Web-Schicht aus dem aktuellen Request erzeugt.
 */
