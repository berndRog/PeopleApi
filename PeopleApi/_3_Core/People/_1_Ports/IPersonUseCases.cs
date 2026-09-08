using PeopleApi._2_BuildingBlocks;
using PeopleApi._3_Core.People._2_Application.Dtos;

namespace PeopleApi._3_Core.People._1_Ports;

// Application facade for People write operations.
public interface IPersonUseCases {
   Task<Result<PersonDto>> CreateAsync(PersonCreateData dto, CancellationToken ct);
   Task<Result<PersonDto>> UpdateAsync(Guid id, PersonUpdateData dto, CancellationToken ct);
   Task<Result> DeleteAsync(Guid id, CancellationToken ct);
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Der Controller kennt nur den Vertrag der Anwendungsfälle.
 * - Implementierungsdetails wie EF Core bleiben hinter Ports verborgen.
 */
