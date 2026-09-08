using PeopleApi._2_BuildingBlocks;
using PeopleApi._3_Core.People._2_Application.Dtos;

namespace PeopleApi._3_Core.People._1_Ports;

// Application facade for complete People write operations.
public interface IPersonUseCases {
   Task<Result<PersonDto>> CreateAsync(PersonCreateData dto, CancellationToken ct);
   Task<Result<PersonDto>> UpdateAsync(Guid id, PersonUpdateData dto, CancellationToken ct);
   Task<Result> DeleteAsync(Guid id, string imageBaseUrl, CancellationToken ct);
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Der Controller kennt nur den Vertrag der vollständigen People-Anwendungsfälle.
 * - Create/Update/Delete dürfen intern mehrere technische Ressourcen koordinieren,
 *   solange konkrete EF-Core- oder File-System-Klassen hinter Ports verborgen bleiben.
 * - HTTP-spezifische Typen wie IFormFile gehören weiterhin nicht in diesen Vertrag.
 */
