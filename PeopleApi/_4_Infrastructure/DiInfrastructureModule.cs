using Microsoft.EntityFrameworkCore;
using PeopleApi._2_BuildingBlocks._1_Ports;
using PeopleApi._3_Core.People._1_Ports;
using PeopleApi._4_Infrastructure.Persistence.Database;
using PeopleApi._4_Infrastructure.Persistence.People;

namespace PeopleApi._4_Infrastructure;

public static class DiInfrastructureModule {
   public static IServiceCollection AddInfrastructureModule(
      this IServiceCollection services,
      IConfiguration configuration
   ) {
      // Read the SQLite connection string from configuration and keep a useful
      // local fallback for the teaching project.
      var connectionString = configuration.GetConnectionString("PeopleDb")
         ?? "Data Source=PeopleApi.db";

      // EF Core is used only for People. The DbContext lifetime is scoped to one
      // HTTP request by ASP.NET Core's default AddDbContext registration.
      services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

      // Bind the People ports from Core to their EF Core implementations.
      services.AddScoped<IPersonDbContext, PersonDbContextEf>();
      services.AddScoped<IPersonRepository, PersonRepositoryEf>();
      services.AddScoped<IPersonReadModel, PersonReadModelEf>();
      services.AddScoped<IUnitOfWork, UnitOfWorkEf>();

      return services;
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Dependency Injection verbindet abstrakte Ports aus dem Core mit konkreten
 *   Infrastrukturklassen.
 * - People werden mit SQLite/EF Core gespeichert.
 * - Der Core kennt SQLite nicht; dieses Detail bleibt in der Infrastructure-
 *   Schicht austauschbar.
 */
