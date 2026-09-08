using Microsoft.EntityFrameworkCore;
using PeopleApi._2_BuildingBlocks._1_Ports;
using PeopleApi._3_Core.Images._1_Ports;
using PeopleApi._3_Core.People._1_Ports;
using PeopleApi._4_Infrastructure.Persistence.Database;
using PeopleApi._4_Infrastructure.Persistence.People;
using PeopleApi._4_Infrastructure.Storage;

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

      // Bind image storage configuration from appsettings.json.
      services.Configure<ImageStorageOptions>(
         configuration.GetSection("ImageStorage")
      );

      // Images are stored only as files. The storage service is stateless after
      // construction and can therefore be shared as a singleton.
      services.AddSingleton<IImageFileStorage, ImageFileStorageFs>();

      return services;
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Dependency Injection verbindet abstrakte Ports aus dem Core mit konkreten
 *   Infrastrukturklassen.
 * - People und Images verwenden absichtlich unterschiedliche Persistenzformen:
 *   SQLite/EF Core für Personen, Dateisystem für Bilder.
 * - Der Core kennt weder SQLite noch FileStream; diese Details bleiben in der
 *   Infrastructure-Schicht austauschbar.
 */
