namespace PeopleApi._3_Core.People._2_Application.Dtos;

// Internal application data used after the Web layer has resolved the image state.
public sealed record PersonUpdateData(
   string FirstName,
   string LastName,
   string? Email,
   string? Phone,
   string? ImageUrl
);

/*
 * Lernziele und Didaktik
 * ----------------------
 * - PersonUpdateData beschreibt den vollständigen neuen fachlichen Zustand.
 * - Ob ein Bild beibehalten, ersetzt oder entfernt wurde, ist an dieser Stelle
 *   bereits entschieden; der Core arbeitet nur noch mit der resultierenden URL.
 */
