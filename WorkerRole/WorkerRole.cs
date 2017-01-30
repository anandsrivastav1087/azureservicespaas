using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Configuration;
using System;
using Microsoft.WindowsAzure.Storage.Queue;

namespace WorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public CloudBlobContainer BlobReference(string BlobName)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["connectionstring"].ConnectionString);
            CloudBlobClient blobStorage = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobStorage.GetContainerReference(BlobName);
            if (container.CreateIfNotExists())
            {
                // configure container for public access
                var permissions = container.GetPermissions();
                permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                container.SetPermissions(permissions);
            }
            return container;
        }

        private CloudQueue CreateQueueConnection()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["connectionstring"].ConnectionString);
            CloudQueueClient queclient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queclient.GetQueueReference("imagefold");
            queue.CreateIfNotExists();
            return queue;
        }
        public override void Run()
        {
            Trace.TraceInformation("WorkerRole is running");

            try
            {                
                this.RunAsync(this.cancellationTokenSource.Token).Wait();                
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;            
            Trace.WriteLine("The Worker has been started");
            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                CloudBlobContainer container = BlobReference("document");
                CloudBlockBlob clblob = container.GetBlockBlobReference("Queuetxt.txt");
                if (!clblob.Exists())
                {
                    clblob.UploadText("Added Current Date Time " + DateTime.Now.ToString());
                }
                else
                {
                    string txt = clblob.DownloadText();
                    clblob.UploadText(txt + "\n Added Current Date Time " + DateTime.Now.ToString());
                }

                CloudQueue queue = CreateQueueConnection();
                if (queue.Exists())
                {
                    CloudQueueMessage msg = queue.PeekMessage();
                    if (msg != null)
                    {
                        if (!clblob.Exists())
                        {
                            clblob.UploadText("Added Current Date Time " + DateTime.Now.ToString() + Environment.NewLine + msg.AsString);
                        }
                        else
                        {
                            string txt = clblob.DownloadText();
                            clblob.UploadText(txt + "\n Added Current Date Time " + DateTime.Now.ToString() + Environment.NewLine + msg.AsString);
                        }

                    }
                    //queue.DeleteMessage(msg);
                }
                await Task.Delay(1000);
            }
        }
    }
}
