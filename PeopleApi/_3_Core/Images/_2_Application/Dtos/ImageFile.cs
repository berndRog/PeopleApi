namespace PeopleApi._3_Core.Images._2_Application.Dtos;

// Represents an opened image resource without depending on ASP.NET Core FileResult.
public sealed record ImageFile(
   Stream Stream,
   string ContentType
);

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Ein Stream erlaubt das schrittweise Übertragen großer Binärdaten.
 * - Der Controller übernimmt erst später die Übersetzung in eine HTTP-Dateiantwort.
 */
