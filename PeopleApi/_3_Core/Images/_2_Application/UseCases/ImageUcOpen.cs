using PeopleApi._2_BuildingBlocks;
using PeopleApi._3_Core.Images._1_Ports;
using PeopleApi._3_Core.Images._2_Application.Dtos;

namespace PeopleApi._3_Core.Images._2_Application.UseCases;

public sealed class ImageUcOpen(
   IImageFileStorage fileStorage
) {
   public Task<Result<ImageFile>> ExecuteAsync(
      string fileName,
      CancellationToken ct
   ) {
      // The storage adapter returns a readable stream plus its MIME type.
      // The Web layer decides how that stream becomes an HTTP response.
      return fileStorage.OpenReadAsync(fileName, ct);
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Lesen bleibt ein eigener Anwendungsfall, obwohl die Implementierung klein ist.
 * - Der Core liefert einen Stream, aber keinen ASP.NET-Core FileResult.
 * - Dadurch bleibt die HTTP-Darstellung Aufgabe des Controllers.
 */
