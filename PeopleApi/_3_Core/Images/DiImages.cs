using PeopleApi._3_Core.Images._1_Ports;
using PeopleApi._3_Core.Images._2_Application.UseCases;

namespace PeopleApi.Configure;

public static class DiImages {
   public static IServiceCollection AddImagesModule(
      this IServiceCollection services
   ) {
      // Register the image application facade and its focused operations.
      services.AddScoped<IImageUseCases, ImageUseCases>();
      services.AddScoped<ImageUcCreate>();
      services.AddScoped<ImageUcOpen>();
      services.AddScoped<ImageUcDelete>();
      return services;
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Das Images-Modul kennt nur UseCases und Ports.
 * - Ob Bilder als Dateien, in Cloud Storage oder anders gespeichert werden,
 *   entscheidet erst die Infrastructure-Schicht.
 */
