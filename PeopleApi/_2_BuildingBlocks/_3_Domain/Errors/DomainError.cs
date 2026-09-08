using PeopleApi._2_BuildingBlocks._3_Domain.Enums;

namespace PeopleApi._2_BuildingBlocks._3_Domain.Errors;

// A domain/application error carries a stable code, readable message and
// a transport-neutral error category.
public sealed record DomainError(
   string Code,
   string Message,
   WebErrorStatus Status
) {
   // Success Results use one shared sentinel instead of null.
   public static readonly DomainError None =
      new(string.Empty, string.Empty, WebErrorStatus.None);
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Fehler werden als Werte modelliert und können durch mehrere Schichten laufen.
 * - Der Code ist für Clients stabiler als ein frei formulierter Meldungstext.
 */
