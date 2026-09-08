using PeopleApi._2_BuildingBlocks;
using PeopleApi._3_Core.Images._2_Application.Dtos;

namespace PeopleApi._3_Core.Images._1_Ports;

// Facade used by both ImageController and server-side People image orchestration.
public interface IImageUseCases {
   Task<Result<ImageDto>> CreateAsync(ImageUpload upload, CancellationToken ct);
   Task<Result<ImageFile>> OpenAsync(string fileName, CancellationToken ct);
   Task<Result> DeleteAsync(string fileName, CancellationToken ct);
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Web-Controller hängen von einem Port statt von konkreten UseCase-Klassen ab.
 * - Dieselben Image-Operationen können direkt über /images oder intern während
 *   eines People-Create/Update/Delete verwendet werden.
 */
