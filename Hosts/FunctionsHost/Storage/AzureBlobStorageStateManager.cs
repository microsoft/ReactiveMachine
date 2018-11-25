// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;

namespace FunctionsHost
{
    public class AzureBlobStorageStateManager
    {
        public const string ApplicationStateContainer = "reactive-machine-state";

        public static string BlobName(uint processId)
        {
            return $"process{processId}";
        }


        public static string LeaseId;

        public static async Task<CloudBlobContainer> GetCloudBlobContainer(String storageConnectionString, ILogger logger, bool createIfNotExists)
        {
            if (CloudStorageAccount.TryParse(storageConnectionString, out var storageAccount))
            {
                // Create the CloudBlobClient that represents the Blob storage endpoint for the storage account.
                var cloudBlobClient = storageAccount.CreateCloudBlobClient();

                // Create a container called 'quickstartblobs' and append a GUID value to it to make the name unique. 
                var cloudBlobContainer = cloudBlobClient.GetContainerReference(ApplicationStateContainer);

                if (createIfNotExists)
                {
                    await cloudBlobContainer.CreateIfNotExistsAsync();

                    // Set the permissions so the blobs are public. 
                    var permissions = new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Blob
                    };
                    await cloudBlobContainer.SetPermissionsAsync(permissions);
                }

                return cloudBlobContainer;
            }
            else
            {
                // Otherwise, let the user know that they need to define the environment variable.
                logger.LogCritical("A connection string for checkpoint storage has not been defined.");

                return null;
            }
        }

        public static bool IsBlobNotFound(StorageException storageException)
        {
            return storageException?.RequestInformation?.HttpStatusCode == (int)HttpStatusCode.NotFound
                && storageException.RequestInformation.ExtendedErrorInformation.ErrorCode == BlobErrorCodeStrings.BlobNotFound;
        }

        public static async Task Save(CloudBlobContainer cloudBlobContainer, String deploymentId, ILogger logger, uint processId, ProcessState cs, string leaseId = null)
        {
            DataContractSerializer ser =
            new DataContractSerializer(typeof(ProcessState));
            MemoryStream stream = new MemoryStream();
            ser.WriteObject(stream, cs);
            stream.Position = 0;
            stream.Flush();

            var accessCondition = new AccessCondition()
            {
                LeaseId = leaseId
            };

            var cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(BlobName(processId));

            if (leaseId != null)
                await cloudBlockBlob.UploadFromStreamAsync(stream, new AccessCondition()
                {
                    LeaseId = leaseId
                },
                new BlobRequestOptions(), new OperationContext());
            else
                await cloudBlockBlob.UploadFromStreamAsync(stream);

            logger.LogInformation($"Saved {cs} to {cloudBlockBlob.Name}!");
        }

        public static async Task<ProcessState> Load(String storageConnectionString, ILogger logger, uint processId)
        {
            var cloudBlobContainer = await GetCloudBlobContainer(storageConnectionString, logger, true);

            // If the connection string is valid, proceed with operations against Blob storage here.
            if (cloudBlobContainer != null)
            {
                DataContractSerializer ser =
                new DataContractSerializer(typeof(ProcessState));
                MemoryStream stream = new MemoryStream();

                try
                {
                    var cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(BlobName(processId));
                    await cloudBlockBlob.DownloadToStreamAsync(stream);
                    stream.Position = 0;
                    // log.Info($"Process {topicName}: Downloaded memory stream: {stream}");
                    var cs = (ProcessState)ser.ReadObject(stream);
                    logger.LogInformation($"Loaded {cs} from {cloudBlockBlob.Name}");
                    return cs;
                }
                catch (StorageException ex) when (IsBlobNotFound(ex))
                {
                    logger.LogCritical($"No checkpoint found");
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogCritical($"Exception: {exception.Message}");
                    throw;
                }
            }
            else
            {
                logger.LogCritical($"No checkpoint container found");
                return null;
            }
        }
    }
}
