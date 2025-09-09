using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Azure.Identity;
using Azure.Core;
using Microsoft.AnalysisServices.Tabular;

class SerializableServer
{
    public required string ServerName { get; set; }
    public List<SerializableDatabase> Databases { get; set; } = new();
}

class SerializableDatabase
{
    public required string DatabaseName { get; set; }
    public List<SerializableTable> Tables { get; set; } = new();
}

class SerializableTable
{
    public required string TableName { get; set; }
    public List<SerializablePartition> Partitions { get; set; } = new();
}

class SerializablePartition
{
    public required string PartitionName { get; set; }
    public string? SourceType { get; set; }
    public string? Expression { get; set; }
}

namespace PowerBI_Metadata_Extractor
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Set up environment variables
            string? workspaceName = Environment.GetEnvironmentVariable("WORKSPACE_NAME");
            string? artifactName = Environment.GetEnvironmentVariable("ARTIFACT_NAME");
            string? tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
            string? clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
            string? clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");

            // Construct the workspace connection string
            string workspaceConnection = $"powerbi://api.powerbi.com/v1.0/myorg/{workspaceName}";

            // Acquire token using Azure.Identity
            Console.WriteLine($"🔐 Authenticating with Azure AD...");
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var tokenRequestContext = new TokenRequestContext(new[] { "https://analysis.windows.net/powerbi/api/.default" });
            AccessToken azureToken = credential.GetToken(tokenRequestContext);
            // Build AnalysisServices AccessToken from AzureToken
            Microsoft.AnalysisServices.AccessToken accessToken = new Microsoft.AnalysisServices.AccessToken(azureToken.Token, azureToken.ExpiresOn);

            // Connect to the Power BI workspace
            Server server = new Server();
            server.AccessToken = accessToken;
            Console.WriteLine($"🔍 Connecting to workspace: {workspaceName}");
            server.Connect(workspaceConnection);
            string serverName = server.Name;

            Console.WriteLine($"📦 Extracting metadata...");
            // Build the serializable metadata structure
            var serializableServer = new SerializableServer
            {
                ServerName = serverName
	        };

	        foreach (Database db in server.Databases)
	        {
                // Some databases may not be accessible; handle exceptions gracefully
                try
                {
                    var _ = db.Model;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Warning: Skipping database \"{db.Name}\": {ex.Message}");
                    continue;
                }

                var serializableDb = new SerializableDatabase
                {
                    DatabaseName = db.Name
                };

                foreach (Table table in db.Model.Tables)
                {
                    var serializableTable = new SerializableTable
                    {
                        TableName = table.Name
                    };

                    foreach (Partition partition in table.Partitions)
                    {
                        string? expression = null;

                        if (partition.Source is QueryPartitionSource querySource)
                            expression = querySource.Query;
                        else if (partition.Source is CalculatedPartitionSource calcSource)
                            expression = calcSource.Expression;
                        else if (partition.Source is MPartitionSource mSource)
                            expression = mSource.Expression;

                        serializableTable.Partitions.Add(new SerializablePartition
                        {
                            PartitionName = partition.Name,
                            SourceType = partition.Source?.GetType().Name,
                            Expression = expression
                        });
                    }
                    serializableDb.Tables.Add(serializableTable);
                }
            serializableServer.Databases.Add(serializableDb);
            }
            // Serialize to JSON and write to file
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            string json = System.Text.Json.JsonSerializer.Serialize(serializableServer, options);
            File.WriteAllText($"{artifactName}.json", json);
            Console.WriteLine($"✅ Metadata written to {artifactName}.json");
            server.Disconnect();
        }
    }
}