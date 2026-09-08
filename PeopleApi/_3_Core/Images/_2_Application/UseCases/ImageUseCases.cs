using PeopleApi._2_BuildingBlocks;
using PeopleApi._3_Core.Images._1_Ports;
using PeopleApi._3_Core.Images._2_Application.Dtos;

namespace PeopleApi._3_Core.Images._2_Application.UseCases;

public sealed class ImageUseCases(
   ImageUcCreate create,
   ImageUcOpen open,
   ImageUcDelete delete
) : IImageUseCases {
   // The facade presents one compact port to the controller while individual
   // use-case classes remain separately testable and focused.
   public Task<Result<ImageDto>> CreateAsync(ImageUpload upload, CancellationToken ct) =>
      create.ExecuteAsync(upload, ct);

   public Task<Result<ImageFile>> OpenAsync(string fileName, CancellationToken ct) =>
      open.ExecuteAsync(fileName, ct);

   public Task<Result> DeleteAsync(string fileName, CancellationToken ct) =>
      delete.ExecuteAsync(fileName, ct);
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - ImageUseCases bündelt mehrere kleine UseCases hinter einer einzigen
 *   Controller-Abhängigkeit.
 * - Die Fassade enthält selbst keine Geschäftslogik, sondern delegiert nur.
 */
