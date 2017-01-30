using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebRole1.Models
{
    public class ServiceBusController : Controller
    {
        static public int cnt = 0;
        // GET: ServiceBus
        public ActionResult ServiceBus()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ServiceBus(ServiceBusQueue objServiceBusQueue)
        {
            QueueClient client=CreateBusConnection(objServiceBusQueue);
            BrokeredMessage objbrokeredmsg = new BrokeredMessage(objServiceBusQueue);
            objbrokeredmsg.Properties["TestProperty"] = objServiceBusQueue.TestProperty;
            cnt = cnt+1;
            objbrokeredmsg.Properties["Message number"] = cnt;

            client.Send(objbrokeredmsg);
            return View();
        }

        private QueueClient CreateBusConnection(ServiceBusQueue objServiceBusQueue)
        {
            string connectionstring = ConfigurationManager.ConnectionStrings["servicebuscon"].ConnectionString;            
            var namespacemanager = NamespaceManager.CreateFromConnectionString(connectionstring);
            QueueDescription qd = new QueueDescription(objServiceBusQueue.QueueName);
            qd.MaxSizeInMegabytes = 5120;
            qd.DefaultMessageTimeToLive = new TimeSpan(0, 10, 0);

            if (!namespacemanager.QueueExists(objServiceBusQueue.QueueName))
            {
                namespacemanager.CreateQueue(qd);
            }
            QueueClient client = QueueClient.CreateFromConnectionString(connectionstring, objServiceBusQueue.QueueName);
            return client;
        }
        string msg = "";
        public ActionResult Receive(ServiceBusQueue objServiceBusQueue)
        {            
            string connectionstring = ConfigurationManager.ConnectionStrings["servicebuscon"].ConnectionString;
            QueueClient client = QueueClient.CreateFromConnectionString(connectionstring, objServiceBusQueue.QueueName);
            List<ServiceBusQueue> lstQueue = new List<ServiceBusQueue>();
            OnMessageOptions options = new OnMessageOptions();
            options.AutoComplete = false;
            options.AutoRenewTimeout = TimeSpan.FromMinutes(1);
            BrokeredMessage BM = new BrokeredMessage();
            
            for (int i = 0; i < cnt; i++)
            {
                try
                {
                    BM = new BrokeredMessage();
                    BM = client.Receive(TimeSpan.FromMinutes(1));
                    if (BM != null)
                    {
                        objServiceBusQueue = new ServiceBusQueue();
                        objServiceBusQueue = BM.GetBody<ServiceBusQueue>();
                        lstQueue.Add(objServiceBusQueue);
                    }
                }
                catch (Exception)
                {

                }
            }
            
           
            cnt = 0;

            //BM = client.Receive(TimeSpan.FromMinutes(1));

            //if (BM!=null)
            //{
            //    //objServiceBusQueue.Message = "Body: " + BM.GetBody<string>();
            //    //objServiceBusQueue.ID = "MessageID: " + BM.MessageId;

            //    lstQueue = BM.GetBody<List<ServiceBusQueue>>();
            //}
            //client.OnMessage(message =>
            //{
            //    msg = "Body: " + message.GetBody<string>();
            //    msg += "MessageID: " + message.MessageId;

            //    message.Complete();
            //}, options);
            return Json(lstQueue);
        }
        public static int topiccnt = 0;
        [HttpPost]
        public ActionResult CreateTopic(ServiceBusTopic objServiceBusTopic)
        {
            string connectionstring = ConfigurationManager.ConnectionStrings["servicebuscon"].ConnectionString;
            
            TopicDescription td = new TopicDescription("AnandTopic");
            td.MaxSizeInMegabytes = 5130;
            td.DefaultMessageTimeToLive = new TimeSpan(0, 50, 0);
            var namespacemanager = NamespaceManager.CreateFromConnectionString(connectionstring);
            if (!namespacemanager.TopicExists("AnandTopic"))
            {
                namespacemanager.CreateTopic("AnandTopic");
            }
            if (!namespacemanager.SubscriptionExists("AnandTopic", "SUB1"))
            {
                SqlFilter sqlfilter = new SqlFilter("MessageNumber > 2");
                namespacemanager.CreateSubscription("AnandTopic", "SUB1", sqlfilter);
            }
            if (!namespacemanager.SubscriptionExists("AnandTopic", "SUB2"))
            {
                SqlFilter sqlfilter = new SqlFilter("MessageNumber <= 2");
                namespacemanager.CreateSubscription("AnandTopic", "SUB2", sqlfilter);
            }
            TopicClient cl = TopicClient.CreateFromConnectionString(connectionstring, "AnandTopic");
            BrokeredMessage BM = new BrokeredMessage(objServiceBusTopic.TPMessage);
            topiccnt += 1;
            BM.Properties["MessageNumber"] = topiccnt;
            cl.Send(BM);
            return View("ServiceBus");
        }

        public ActionResult ReceiveTopic()
        {
            string connectionstring = ConfigurationManager.ConnectionStrings["servicebuscon"].ConnectionString;
            ServiceBusTopic objServiceBusTopic = new ServiceBusTopic();
            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionstring);

            var ss = namespaceManager.GetSubscriptions("AnandTopic");
            List<ServiceBusTopic> lstTopic = new List<ServiceBusTopic>();
            foreach (var item in ss)
            {
                SubscriptionClient client = SubscriptionClient.CreateFromConnectionString(connectionstring, "AnandTopic", item.Name);
                BrokeredMessage bm = new BrokeredMessage();
                for (int i = 0; i < topiccnt; i++)
                {
                    bm = new BrokeredMessage();
                    bm = client.Receive(TimeSpan.FromMinutes(1));
                    if (bm != null)
                    {
                        objServiceBusTopic = new ServiceBusTopic();
                        objServiceBusTopic.TPMessage = bm.GetBody<string>();
                        objServiceBusTopic.Subcription = item.Name;
                        lstTopic.Add(objServiceBusTopic);
                    }
                }
            }

            return Json(lstTopic);
        }
    }
}