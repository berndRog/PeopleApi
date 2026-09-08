namespace PeopleApi._1_Web.ReadModels;

// The Web layer enriches stored image metadata with the absolute HTTP URL
// generated from the current request host.
public sealed record ImageReadModel(
   string FileName,
   string ImageUrl,
   string ContentType,
   long Length,
   string OriginalFileName
);

/*
 * Lernziele und Didaktik
 * ----------------------
 * - ReadModels beschreiben die Darstellung einer Ressource an der HTTP-Grenze.
 * - ImageUrl entsteht erst im Controller, weil nur dort Scheme und Host bekannt sind.
 */
