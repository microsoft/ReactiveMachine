// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FunctionsHost
{
    internal class LeaseManager : IDisposable
    {
        public string LeaseId { get; private set; }

        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private CloudBlockBlob stateBlob;

        public CancellationToken LeaseIsGone => cts.Token;

        public LeaseManager(CloudBlockBlob stateBlob)
        {
            this.stateBlob = stateBlob;
        }

        private Task ignoredTask;

        public async Task<bool> TryGetLease(ILogger logger)
        {
            try
            {
                // first, try to get the lease
                LeaseId = await stateBlob.AcquireLeaseAsync(TimeSpan.FromSeconds(15), null);

                // start a background lease renewal process
                ignoredTask = Task.Run(LeaseRenewalLoop);

                return true;
            }
            catch (StorageException ex)
            {
                var information = ex.RequestInformation.ExtendedErrorInformation;
                if (information != null && information.ErrorCode == "LeaseAlreadyPresent")
                {
                    return false;
                }
                if ((ex.RequestInformation.HttpStatusCode == 404) && (information.ErrorCode.Equals(BlobErrorCodeStrings.ContainerNotFound)))
                {
                    logger.LogWarning("Invalid application state (Container not found). Ignoring Doorbell.");
                    return false;
                }
                if ((ex.RequestInformation.HttpStatusCode == 404) && (information.ErrorCode.Equals(BlobErrorCodeStrings.BlobNotFound)))
                {
                    logger.LogWarning("Invalid application state (Blob not found). Ignoring Doorbell.");
                    return false;
                }

                throw;
            }
        }

        public async Task Release()
        {
            AccessCondition acc = new AccessCondition() { LeaseId = LeaseId };
            await stateBlob.ReleaseLeaseAsync(acc);
        }

        public async Task LeaseRenewalLoop()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(7), cts.Token);

                if (cts.IsCancellationRequested)
                    break;

                AccessCondition acc = new AccessCondition() { LeaseId = LeaseId };

                try
                {
                    await stateBlob.RenewLeaseAsync(acc);
                }
                catch
                {
                    // we lost the lease, cancel if not already
                    cts.Cancel();
                    break;
                }
            }
        }

        public void Dispose()
        {
            cts.Cancel();
        }
    }
}
