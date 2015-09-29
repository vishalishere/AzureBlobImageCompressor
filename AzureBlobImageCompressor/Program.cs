namespace AzureBlobImageCompressor
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

    internal class Program
    {
        private static void Main(string[] args)
        {
            var connectionString = ConfigurationManager.AppSettings["AzureConnectionString"];
            var containerName = ConfigurationManager.AppSettings["ContainerName"];

            var container = CloudStorageAccount
                .Parse(connectionString)
                .CreateCloudBlobClient()
                .GetContainerReference(containerName);

            var dir = Environment.CurrentDirectory;

            foreach (var listBlobItem in container.ListBlobs())
            {
                var blob = container.GetBlockBlobReference(((CloudBlockBlob) listBlobItem).Name);
                blob.FetchAttributes();
                if (blob.Metadata.ContainsKey("optimized") && blob.Metadata["optimized"] == "true")
                {
                    continue;
                }

                var path = Path.Combine(dir, blob.Name);

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                blob.DownloadToFile(path, FileMode.CreateNew);

                if (path.EndsWith(".png"))
                {
                    Console.WriteLine("Processing file: " + path);
                    Process.Start(Path.Combine(dir, "optipng.exe"), path);
                    blob.Metadata["optimized"] = "true";
                    Console.WriteLine("File optimized");
                    Console.WriteLine();
                }
                else if (path.EndsWith(".jpg") || path.EndsWith(".jpeg"))
                {
                    Console.WriteLine("Processing file: " + path);
                    Process.Start(Path.Combine(dir, "jpegtran.exe"), "-optimize -verbose" + path + " " + path);
                    blob.Metadata["optimized"] = "true";
                    Console.WriteLine("File optimized");
                    Console.WriteLine();
                }
                else
                {
                    continue;
                }

                if (blob.Metadata["optimized"] == "true")
                {
                    blob.UploadFromFile(path, FileMode.Open);
                }

                File.Delete(path);
            }
        }
    }
}
