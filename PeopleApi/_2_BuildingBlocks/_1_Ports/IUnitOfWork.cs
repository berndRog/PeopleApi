namespace PeopleApi._2_BuildingBlocks._1_Ports;

// Defines the transaction boundary used by write use cases.
public interface IUnitOfWork {
   Task<int> SaveAllChangesAsync(
      string caller,
      CancellationToken ct = default
   );
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Der Port abstrahiert SaveChanges von EF Core.
 * - UseCases kennen dadurch nur die Transaktionsidee, nicht den konkreten DbContext.
 */
