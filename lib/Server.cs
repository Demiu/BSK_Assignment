﻿using System.Net;
using System.Net.Sockets;
using Lib.Fs;

namespace Lib;

public class Server : IDisposable
{
    TcpListener listener;
    List<Connection> connections;
    CancellationTokenSource cancelTokenSource;
    FileSystemAgent fsAgent;
    public Action<Connection>? OnNewConnection {get; set;}

    public Server(IPEndPoint endPoint) {
        listener = new TcpListener(endPoint);
        connections = new();
        cancelTokenSource = new CancellationTokenSource();
        fsAgent = new();
    }

    ~Server() {
        Dispose();
    }

    public void Dispose()
    {
        cancelTokenSource.Cancel();
    }

    public async Task ListenLoop() {
        Console.WriteLine("Server::ListenLoop start");
        try {
            var token = cancelTokenSource.Token;
            listener.Start();
            while (!token.IsCancellationRequested) {
                var client = await listener.AcceptTcpClientAsync(cancelTokenSource.Token);
                HandleNewClient(client);
            }
        } finally {
            listener.Stop();
        }
    }

    public void PingAll() {
        Console.WriteLine($"Pinging all {connections.Count} connections");
        foreach (var c in connections) {
            c.SendPing();
        }
    }

    public List<byte[]?> GetAllAesKeys() {
        var ret = new List<byte[]?>(connections.Count);
        foreach (var c in connections) {
            ret.Add(c.GetAesKey());
        }
        return ret;
    }

    private void HandleNewClient(TcpClient client) {
        Console.WriteLine($"New client connected");
        var connection = new Connection(client, fsAgent, cancelTokenSource.Token);
        connections.Add(connection);
        if (OnNewConnection != null) {
            OnNewConnection(connection);
        }
        Util.TaskRunSafe(() => connection.CommunicationLoop()); // TODO handleconnectionclosed, remove connection on completion
    }

}
