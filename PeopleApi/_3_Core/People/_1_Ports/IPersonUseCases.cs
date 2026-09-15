using PeopleApi._2_BuildingBlocks;
using PeopleApi._3_Core.People._2_Application.Dtos;

namespace PeopleApi._3_Core.People._1_Ports;

public interface IPersonUseCases {
   Task<Result<PersonDto>> CreateAsync(PersonDto dto, CancellationToken ct);
   Task<Result<PersonDto>> UpdateAsync(Guid id, PersonDto dto, CancellationToken ct);
   Task<Result> DeleteAsync(Guid id, CancellationToken ct);
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Der Controller kennt nur den Port der People-Anwendungsfälle.
 * - Der Vertrag enthält weder ASP.NET-Core-Typen noch Dateioperationen.
 * - PersonDto transportiert die JSON-Daten; die Domain-Entität bleibt intern.
 */
