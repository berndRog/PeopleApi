using PeopleApi._2_BuildingBlocks;
using PeopleApi._3_Core.People._2_Application.Dtos;

namespace PeopleApi._3_Core.People._1_Ports;

// Read-only queries exposed to PeopleController.
public interface IPersonReadModel {
   Task<Result<IReadOnlyList<PersonDto>>> SelectAllAsync(CancellationToken ct);
   Task<Result<int>> CountAsync(CancellationToken ct);
   Task<Result<PersonDto>> FindByIdAsync(Guid id, CancellationToken ct);
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Lesende Abfragen werden vom schreiborientierten Repository getrennt.
 * - Das ReadModel liefert direkt DTOs für die Application/Web-Schicht.
 * - CountAsync ermittelt die Anzahl direkt in der Datenbank, ohne dafür alle
 *   Person-Datensätze laden zu müssen.
 */
