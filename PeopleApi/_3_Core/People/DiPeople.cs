using PeopleApi._3_Core.People._1_Ports;
using PeopleApi._3_Core.People._2_Application.UseCases;

namespace PeopleApi.Configure;

public static class DiPeople {
   public static IServiceCollection AddPeopleModule(
      this IServiceCollection services
   ) {
      // Expose one facade to the controller and compose it from focused use cases.
      services.AddScoped<IPersonUseCases, PersonUseCases>();
      services.AddScoped<PersonUcCreate>();
      services.AddScoped<PersonUcUpdate>();
      services.AddScoped<PersonUcDelete>();
      return services;
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Das People-Modul registriert Application-Services, aber keine EF-Core-Klassen.
 * - Konkrete Persistenzadapter werden getrennt im Infrastructure-Modul gebunden.
 */
