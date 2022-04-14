using System.Net;

namespace Cli;

internal class Mains {
    internal static void ServerMain(string[] args) {
        var server = new Lib.Server(new IPEndPoint(IPAddress.Parse("127.0.0.1"), Lib.Defines.Constants.DEFAULT_PORT));
        server.ListenLoop().Wait();
        //Console.ReadKey();
    }

    internal static void ClientMain(string[] args) {
        var cancelTokenSource = new CancellationTokenSource();
        var connectionTask = Lib.Connection.CreateTo(new IPEndPoint(IPAddress.Parse("127.0.0.1"), Lib.Defines.Constants.DEFAULT_PORT), cancelTokenSource.Token);
        connectionTask.Wait();
        var connection = connectionTask.Result;
        if (connection != null) {
            connection.CommunicationLoop().Wait();
        } else {
            Console.WriteLine("Client failed connecting");
            Console.ReadKey();
        }
    }
}