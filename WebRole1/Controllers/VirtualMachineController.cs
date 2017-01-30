using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Azure;
using System.Configuration;
using WebRole1.Models;
using Microsoft.WindowsAzure.Management.Compute;
using Microsoft.WindowsAzure.Management.Compute.Models;
using Hyak.Common;
using System.ComponentModel;

namespace WebRole1.Controllers
{
    public class VirtualMachineController : Controller
    {
        private static string ReturnCertificate()
        {
            return ConfigurationManager.AppSettings["encodedcertificate"].ToString(); 
        }
        private static string ReturnSubscriptionID()
        {
            return ConfigurationManager.AppSettings["subscriptionId"].ToString();
            
        }        

        public static CertificateCloudCredentials cloudCredentials = new CertificateCloudCredentials(ReturnSubscriptionID(), new System.Security.Cryptography.X509Certificates.X509Certificate2(Convert.FromBase64String(ReturnCertificate())));
        public static string StorageAccountName = "docstoragepoc";
        // GET: VirtualMachine
        public ActionResult createVM()
        {
            List<VirtualMachine> lstVirtualMachine = new List<VirtualMachine>();
            ComputeManagementClient client = new ComputeManagementClient(cloudCredentials);
            lstVirtualMachine = ListVM(client);
            return View(lstVirtualMachine);
        }

        [HttpPost]
        public ActionResult createVM(VirtualMachine objVirtualMachine)
        {
            List<VirtualMachine> lstVirtualMachine = new List<VirtualMachine>();
            ComputeManagementClient client = createCloudService(objVirtualMachine.VMName, objVirtualMachine.Location);
            try
            {
                var VmRole = new Role
                {
                    RoleType = VirtualMachineRoleType.PersistentVMRole.ToString(),
                    RoleName = objVirtualMachine.VMName,
                    Label = objVirtualMachine.VMName,
                    RoleSize = VirtualMachineRoleSize.Small,
                    ConfigurationSets = new List<ConfigurationSet>(),
                    OSVirtualHardDisk = new OSVirtualHardDisk()
                    {
                        MediaLink = getVhdUri(string.Format("{0}.blob.core.windows.net/vhds", objVirtualMachine.storageAccount)),
                        SourceImageName = GetSourceImageName("Windows Server 2012 Datacenter", client)
                    }
                };

                ConfigurationSet configSet = new ConfigurationSet
                {
                    ConfigurationSetType = ConfigurationSetTypes.WindowsProvisioningConfiguration,
                    EnableAutomaticUpdates = true,
                    ResetPasswordOnFirstLogon = false,
                    ComputerName = objVirtualMachine.VMName,
                    AdminUserName = objVirtualMachine.UserName,
                    AdminPassword = objVirtualMachine.Password,
                    InputEndpoints = new BindingList<InputEndpoint>
                {
                    new InputEndpoint { LocalPort = 3389, Name = "RDP", Protocol = "tcp" },
                    new InputEndpoint { LocalPort = 80, Port = 80, Name = "web", Protocol = "tcp" }
                }
                };

                VmRole.ConfigurationSets.Add(configSet);
                VmRole.ResourceExtensionReferences = null;
                List<Role> lstRole = new List<Role>() { VmRole };
                VirtualMachineCreateDeploymentParameters createDeploymentParams = new VirtualMachineCreateDeploymentParameters
                {

                    Name = objVirtualMachine.VMName,
                    Label = objVirtualMachine.VMName,
                    Roles = lstRole,
                    DeploymentSlot = DeploymentSlot.Production
                };
                client.VirtualMachines.CreateDeployment(objVirtualMachine.VMName, createDeploymentParams);
                ViewBag.Message = "Saved Successfully.........";
            }
            catch (CloudException e)
            {
                ViewBag.Message = e.Message;
            }
            catch (Exception ex)
            {
                ViewBag.Message = ex.Message;
            }
            lstVirtualMachine=ListVM(client);
            return View(lstVirtualMachine);
        }
        public List<VirtualMachine> ListVM(ComputeManagementClient client)
        {
            List<VirtualMachine> lstVirtualMachine = new List<VirtualMachine>();
            VirtualMachine objVM = new VirtualMachine();       
            var hostedServices = client.HostedServices.List();
            foreach (var service in hostedServices)
            {
                var deployment = GetAzureDeyployment(service.ServiceName, DeploymentSlot.Production);
                if (deployment != null)
                {
                    if (deployment.Roles.Count > 0)
                    {
                        foreach (var role in deployment.Roles)
                        {
                            if (role.RoleType == VirtualMachineRoleType.PersistentVMRole.ToString())
                            {                                
                                objVM = new VirtualMachine();
                                objVM.VMName = role.RoleName;                                
                                lstVirtualMachine.Add(objVM);
                            }

                        }
                    }
                }
            }
            return lstVirtualMachine;
        }

        public ActionResult RestartVM(VirtualMachine objVirtualMachine)
        {
            List<VirtualMachine> lstVirtualMachine = new List<VirtualMachine>();
            ComputeManagementClient client = new ComputeManagementClient(cloudCredentials);
            lstVirtualMachine = ListVM(client);
            try
            {
                client.VirtualMachines.Start("anandDN", "anandSN", objVirtualMachine.VMName);
            }
            catch (Exception)
            {
                client.VirtualMachines.Restart("anandDN", "anandSN", objVirtualMachine.VMName);
            }            
            

            return View("createVM", lstVirtualMachine);
        }

        public ActionResult ConnectVM(VirtualMachine objVirtualMachine)
        {
            List<VirtualMachine> lstVirtualMachine = new List<VirtualMachine>();
            ComputeManagementClient client = new ComputeManagementClient(cloudCredentials);
            lstVirtualMachine = ListVM(client);
            client.VirtualMachines.GetRemoteDesktopFile("anandDN", "anandSN", objVirtualMachine.VMName);
            return View("createVM", lstVirtualMachine);
        }

        public ActionResult stopVM(VirtualMachine objVirtualMachine)
        {
            List<VirtualMachine> lstVirtualMachine = new List<VirtualMachine>();
            ComputeManagementClient client = new ComputeManagementClient(cloudCredentials);
            lstVirtualMachine = ListVM(client);

            //client.VirtualMachines.Shutdown("anandDN", "anandSN", objVirtualMachine.VMName,);

            return View("createVM", lstVirtualMachine);
        }
        private DeploymentGetResponse GetAzureDeyployment(string serviceName, DeploymentSlot slot)
        {
            ComputeManagementClient client = new ComputeManagementClient(cloudCredentials);
            try
            {
                return client.Deployments.GetBySlot(serviceName, slot);
            }
            catch (CloudException ex)
            {

                if (ex.Error.Code == "ResourceNotFound")
                {
                    return null;
                }
                else
                {
                    throw ex;
                }
            }
        }
        private static Uri getVhdUri(string blobcontainerAddress)
        {
            var now = DateTime.UtcNow;
            string dateString = now.Year + "-" + now.Month + "-" + now.Day + now.Hour + now.Minute + now.Second + now.Millisecond;

            var address = string.Format("http://{0}/{1}-650.vhd", blobcontainerAddress, dateString);
            return new Uri(address);
        }
        private static string GetSourceImageName(string name, ComputeManagementClient client)
        {
            var result= client.VirtualMachineOSImages.List();
            var disk = result.Where(m => m.ImageFamily == name).FirstOrDefault();
            if (disk!=null)
            {
                return disk.Name;
            }
            else
            {
                throw new CloudException(string.Format("Can't find {0} OS image in current subscription"));
            }
        }
        private static string EncodeToBase64(string toEncode)
        {
            byte[] toEncodeAsBytes
            = System.Text.ASCIIEncoding.ASCII.GetBytes(toEncode);
            string returnValue
                  = System.Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }
        private ComputeManagementClient createCloudService(string VMName, string location, string affinityGroupName = null)
        {
            ComputeManagementClient client = new ComputeManagementClient(cloudCredentials);
            HostedServiceCreateParameters hostedServiceCreateParams = new HostedServiceCreateParameters();           
            try
            {
                if (location != null)
                {
                    hostedServiceCreateParams = new HostedServiceCreateParameters
                    {
                        ServiceName = VMName,
                        Location = location,
                        Label = EncodeToBase64(VMName),
                    };
                }
                else if (affinityGroupName != null)
                {
                    hostedServiceCreateParams = new HostedServiceCreateParameters
                    {
                        ServiceName = VMName,
                        AffinityGroup = affinityGroupName,
                        Label = EncodeToBase64(VMName),
                    };
                }
                client.HostedServices.Create(hostedServiceCreateParams);
            }
            catch (CloudException e)
            {
                throw e;
            }
            return client;

        }

    }
}