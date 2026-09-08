namespace PeopleApiTest.TestInfrastructure;

public abstract class TestBaseEndToEnd : IDisposable {
   protected PeopleApiFactory Factory { get; }
   protected HttpClient Client { get; }

   protected TestBaseEndToEnd() {
      // A fresh factory means a fresh SQLite database and image directory for
      // every xUnit test instance.
      Factory = new PeopleApiFactory();

      // CreateClient starts the application lazily and routes HTTP calls through
      // ASP.NET Core TestServer instead of opening a real TCP port.
      Client = Factory.CreateClient();
   }

   public void Dispose() {
      // Dispose the client before the factory removes its temporary resources.
      Client.Dispose();
      Factory.Dispose();
   }
}

/*
 * Lernziele und Didaktik
 * ----------------------
 * - Gemeinsamer Testaufbau wird einmal gekapselt und nicht in jedem Test kopiert.
 * - Die Tests kommunizieren über HttpClient mit der API und bleiben damit echte
 *   End-to-End-Tests der serverseitigen Schichten.
 */
