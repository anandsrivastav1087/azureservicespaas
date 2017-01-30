using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using WebRole1.Models;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Microsoft.WindowsAzure.Storage.Table;

namespace WebRole1.Controllers
{
    public class UploadFileController : Controller
    {
        #region SAS & Public Storage blob

        // GET: UploadFile
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult documentupload()
        {
            CloudBlobContainer container = BlobReference("document");
            List<Download> lstDownload = new List<Download>();
            lstDownload = FIllList(container);
            return View(lstDownload);
        }
        private List<Download> FIllList(CloudBlobContainer container)
        {
            List<Download> lstDownload = new List<Download>();

            Download objDownload;
            foreach (IListBlobItem item in container.ListBlobs(null, false))
            {
                CloudBlockBlob blob = (CloudBlockBlob)item;
                objDownload = new Download();
                objDownload.Name = blob.Name;
                objDownload.Url = blob.Uri.AbsoluteUri.ToString();
                lstDownload.Add(objDownload);
            }
            if (lstDownload.Count == 0)
            {
                objDownload = new Download();
                objDownload.Name = "No Record Found......!";
                objDownload.Url = "";
                lstDownload.Add(objDownload);
            }



            return lstDownload;
        }
        [HttpPost]
        public ActionResult documentupload(HttpPostedFileBase file,string json)
        {
            List<Download> lstDownload = new List<Download>();
            try
            {
                if (file != null)
                {
                    CloudBlobContainer container = BlobReference("document");
                    CloudBlockBlob blockblob = container.GetBlockBlobReference(file.FileName);
                    blockblob.UploadFromStream(file.InputStream);
                    lstDownload = FIllList(container);
                    ViewBag.message = "Uploaded Successfully.......!";
                }
            }
            catch (Exception ex)
            {
                ViewBag.message = ex.Message;
            }
            if (json=="Y")
            {
                return Json(ViewBag.message);
            }
            else
            {
                return View("documentupload", lstDownload);
            }
            
        }

        public ActionResult download(string url)
        {
            List<Download> lstDownload = new List<Download>();
            CloudBlobContainer container = BlobReference("document");
            lstDownload = FIllList(container);
            Response.Redirect(url);

            //foreach (IListBlobItem item in container.ListBlobs(null, false))
            //{
            //    CloudBlockBlob blob = (CloudBlockBlob)item;
            //    Response.Redirect(item.Uri.AbsoluteUri.ToString());
            //    //((Microsoft.WindowsAzure.Storage.Blob.CloudBlob)item).Name
            //}
            return View("documentupload", lstDownload);
        }

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

        public ActionResult delete(string name, string url)
        {
            List<Download> lstDownload = new List<Download>();
            CloudBlobContainer container = BlobReference("document");
            CloudBlockBlob clblob = container.GetBlockBlobReference(name);
            clblob.DeleteIfExists();
            lstDownload = FIllList(container);
            return View("documentupload", lstDownload);
        }


        public static List<Download> GetBlobSasUri()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["connectionstring"].ConnectionString);
            CloudBlobClient blobStorage = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobStorage.GetContainerReference("documentsas");
            container.CreateIfNotExists();

            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
            sasConstraints.SharedAccessStartTime = DateTime.UtcNow.AddMinutes(0);
            sasConstraints.SharedAccessExpiryTime = DateTime.UtcNow.AddSeconds(10);
            sasConstraints.Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write;

            List<Download> lstDownload = new List<Download>();
            Download objDownload;
            foreach (IListBlobItem item in container.ListBlobs(null, true))
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;
                    string sasBlobToken = blob.GetSharedAccessSignature(sasConstraints);

                    objDownload = new Download();
                    objDownload.Name = blob.Name;
                    objDownload.Url = blob.Uri + sasBlobToken;
                    objDownload.SASUrl = blob.Uri;
                    lstDownload.Add(objDownload);
                }
            }

            return lstDownload;
        }

        [HttpGet]
        public ActionResult uploadwithSAS()
        {
            //CloudBlobContainer container = getcontainer("documentsas");
            List<Download> lstDownload = new List<Download>();
            lstDownload = GetBlobSasUri();
            //ViewBag.list = lstDownload;
            return View(lstDownload);
        }
        private CloudBlobContainer getcontainer(string containerName)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["connectionstring"].ConnectionString);
            CloudBlobClient blobStorage = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobStorage.GetContainerReference(containerName);
            container.CreateIfNotExists();

            SharedAccessBlobPolicy policies = new SharedAccessBlobPolicy();

            policies.SharedAccessStartTime = DateTime.Now;
            policies.SharedAccessExpiryTime = DateTime.Now.AddHours(1);
            policies.Permissions = SharedAccessBlobPermissions.Read;
            policies.Permissions = SharedAccessBlobPermissions.Write;
            string sassignature = container.GetSharedAccessSignature(policies);

            ServiceProperties sp = blobStorage.GetServiceProperties();
            sp.DefaultServiceVersion = "2013-08-15";
            CorsRule cr = new CorsRule();
            cr.AllowedHeaders.Add("*");
            cr.AllowedMethods = CorsHttpMethods.Get | CorsHttpMethods.Put | CorsHttpMethods.Post;
            cr.AllowedOrigins.Add("http://localhost:63093");
            cr.ExposedHeaders.Add("x-ms-*");
            cr.MaxAgeInSeconds = 500;
            sp.Cors.CorsRules.Clear();
            sp.Cors.CorsRules.Add(cr);
            blobStorage.SetServiceProperties(sp);

            return container;
        }

        [HttpPost]
        public ActionResult uploadwithSAS(HttpPostedFileBase file)
        {
            CloudBlobContainer container = getcontainer("documentsas");
            CloudBlockBlob blockblob = container.GetBlockBlobReference(file.FileName);

            blockblob.UploadFromStream(file.InputStream);
            List<Download> lstDownload = new List<Download>();
            lstDownload = GetBlobSasUri();
            return View(lstDownload);
        }
        public ActionResult downloadsas(Uri url, string name)
        {
            List<Download> lstDownload = new List<Download>();

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["connectionstring"].ConnectionString);
            CloudBlobClient blobStorage = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobStorage.GetContainerReference("documentsas");
            container.CreateIfNotExists();
            CloudBlockBlob blockblob = container.GetBlockBlobReference(name);

            SharedAccessBlobPolicy policies = new SharedAccessBlobPolicy();

            policies.SharedAccessStartTime = DateTime.UtcNow.AddSeconds(-4);
            policies.SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(10);
            policies.Permissions = SharedAccessBlobPermissions.Read;
            policies.Permissions = SharedAccessBlobPermissions.Write;

            string sassignature = blockblob.GetSharedAccessSignature(policies);
            //ServiceProperties sp = blobStorage.GetServiceProperties();
            //sp.DefaultServiceVersion = "2013-08-15";
            //CorsRule cr = new CorsRule();
            //cr.AllowedHeaders.Add("*");
            //cr.AllowedMethods = CorsHttpMethods.Get | CorsHttpMethods.Put | CorsHttpMethods.Post;
            //cr.AllowedOrigins.Add("http://localhost:63093");
            //cr.ExposedHeaders.Add("x-ms-*");
            //cr.MaxAgeInSeconds = 500;
            //sp.Cors.CorsRules.Clear();
            //sp.Cors.CorsRules.Add(cr);
            //blobStorage.SetServiceProperties(sp);


            lstDownload = FIllList(container);
            //Response.Redirect(blockblob.SnapshotQualifiedStorageUri.PrimaryUri.ToString() + sassignature);
            Response.Redirect(url.OriginalString + sassignature);

            return View("uploadwithSAS", lstDownload);
        }
        public ActionResult deletesas(string name, string url)
        {
            List<Download> lstDownload = new List<Download>();
            CloudBlobContainer container = getcontainer("documentsas");
            CloudBlockBlob clblob = container.GetBlockBlobReference(name);
            clblob.DeleteIfExists();
            lstDownload = GetBlobSasUri();
            return View("uploadwithSAS", lstDownload);
        }
        #endregion

        #region Table Storage

        private CloudTable CreateStorageConnection()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["connectionstring"].ConnectionString);
            CloudTableClient tblclient = storageAccount.CreateCloudTableClient();
            CloudTable tbl = tblclient.GetTableReference("employeenew");
            tbl.CreateIfNotExists();
            return tbl;
        }
        public ActionResult tablestorage()
        {
            List<tblStorage> lst = new List<tblStorage>();
            CloudTable tbl = CreateStorageConnection();
            lst = FillTableList(tbl);
            return View(lst);
        }
        [HttpPost]
        public ActionResult tablestorage(tblStorage tbldata)
        {
            string msg = "Saved Successfully";
            List<tblStorage> lst = new List<tblStorage>();
            try
            {
                CloudTable tbl = CreateStorageConnection();
                tblStorage objtblStorage = new tblStorage();
                objtblStorage.PhoneNumber = tbldata.PhoneNumber;
                objtblStorage.address = tbldata.address;
                objtblStorage.RowKey = tbldata.RowKey;
                objtblStorage.PartitionKey = tbldata.PartitionKey;
                objtblStorage.Email = tbldata.Email;

                TableOperation operation;
                operation = TableOperation.InsertOrReplace(objtblStorage);

                tbl.Execute(operation);
                lst = FillTableList(tbl);
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }

            return View(lst);
        }

        private List<tblStorage> FillTableList(CloudTable tbl)
        {
            List<tblStorage> lst = new List<tblStorage>();
            tblStorage objtblStorage = new tblStorage();
            TableQuery<tblStorage> query = new TableQuery<tblStorage>();
            foreach (var item in tbl.ExecuteQuery(query))
            {
                objtblStorage = new tblStorage();
                objtblStorage.Email = item.Email;
                objtblStorage.address = item.address;
                objtblStorage.PartitionKey = item.PartitionKey;
                objtblStorage.RowKey = item.RowKey;
                objtblStorage.Timestamp = item.Timestamp;
                lst.Add(objtblStorage);
            }
            return lst;
        }

        public ActionResult editupdate(string command, tblStorage tbldata)
        {
            string msg = "Deleted Successfully";
            List<tblStorage> lst = new List<tblStorage>();
            TableOperation operation;
            CloudTable tbl = CreateStorageConnection();
            //TableQuery<tblStorage> query = new TableQuery<tblStorage>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, parition));
            //TableQuery<tblStorage> rangeQuery = new TableQuery<tblStorage>().Where(
            //TableQuery.CombineFilters(
            //TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, parition),
            //TableOperators.And,
            //TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, key)));
            operation = TableOperation.Retrieve<tblStorage>(tbldata.PartitionKey, tbldata.RowKey);
            TableResult tblResult = tbl.Execute(operation);
            tbldata = (tblStorage)tblResult.Result;
            if (command == "d")
            {
                operation = TableOperation.Delete(tbldata);
                tbl.Execute(operation);
                lst = FillTableList(tbl);
                return View("tablestorage", lst);
            }
            else
            {
                return Json(tbldata);
            }
        }
        #endregion

        #region Queue Storage

        private CloudQueue CreateQueueConnection()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["connectionstring"].ConnectionString);
            CloudQueueClient queclient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queclient.GetQueueReference("imagefold");
            queue.CreateIfNotExists();
            return queue;
        } 
        [HttpGet]
        public ActionResult QueueStorage()
        {
            CloudQueue queue = CreateQueueConnection();
            return View();
        }
        [HttpPost]
        public ActionResult QueueStorage(HttpPostedFileBase file, Download objdownlaod)
        {
            CloudQueue queue = CreateQueueConnection();
            CloudQueueMessage message = new CloudQueueMessage(objdownlaod.Name);            
            queue.AddMessage(message);
            return View();
        }

        #endregion

    }
}
