using PeopleApi._2_BuildingBlocks._1_Ports;

namespace PeopleApi._4_Infrastructure.Persistence.Database;

internal sealed class UnitOfWorkEf(
   AppDbContext dbContext,
   ILogger<UnitOfWorkEf> logger
) : IUnitOfWork {
   public async Task<int> SaveAllChangesAsync(
      string caller,
      CancellationToken ct = default
   ) {
      // Commit all tracked changes in the current scoped DbContext together.
      var rows = await dbContext.SaveChangesAsync(ct);

      // Keep persistence diagnostics at Debug level to avoid noisy normal output.
      logger.LogDebug("{Caller}: SaveChanges rows={Rows}", caller, rows);
      return rows;
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - UnitOfWork abstrahiert den Transaktionsabschluss eines Anwendungsfalls.
 * - Repository-Methoden bereiten Änderungen vor; SaveChanges schreibt sie gemeinsam.
 */
