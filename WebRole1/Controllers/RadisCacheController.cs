using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using WebRole1.Models;

namespace WebRole1.Controllers
{
    public class RadisCacheController : Controller
    {
        public EmployeeContext db = new EmployeeContext();
        ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(ConfigurationManager.AppSettings["RadisConnection"].ToString());
        // GET: RadisCache
        public ActionResult Radis()
        {
            return RadisCacheDisplay();
        }

        public ActionResult RadisCacheCreate()
        {
            //List<Employee> lstEmployee = db.Employees.ToList();
            //IDatabase idb = connection.GetDatabase();
            //idb.StringSet("empdetails", JsonConvert.SerializeObject(lstEmployee));
            return PartialView("_Create");
        }

        [HttpPost]
        public ActionResult RadisCacheCreate(Employee objEmployee)
        {
            CommonMessage CM = new CommonMessage();
            CM.Message = "Saved Successfully..!";
            object[] obj = new object[2];
            Employee emp = new Employee();
            if (objEmployee !=null)
            {
                db.Employees.Add(objEmployee);
                db.SaveChanges();
            }
            CM.Data = obj;
            List<Employee> lstEmployee = db.Employees.ToList();
            IDatabase idb = connection.GetDatabase();
            idb.StringSet("empdetails", JsonConvert.SerializeObject(lstEmployee));
            return Json(CM);
        }

        public ActionResult RadisCacheDisplay()
        {
            List<Employee> lstEmployee;
            IDatabase idb = connection.GetDatabase();
            if (idb.KeyExists("empdetails"))
            {
                lstEmployee = JsonConvert.DeserializeObject<List<Employee>>(idb.StringGet("empdetails"));
            }
            else
            {
                lstEmployee = db.Employees.ToList();
            }
            return View("Radis", lstEmployee);
        }
        public string RenderPartialToStringMethod(ControllerContext context, string partialViewName, ViewDataDictionary viewData, TempDataDictionary tempData)
        {
            ViewEngineResult result = ViewEngines.Engines.FindPartialView(context, partialViewName);

            if (result.View != null)
            {
                StringBuilder sb = new StringBuilder();
                using (StringWriter sw = new StringWriter(sb))
                {
                    using (HtmlTextWriter output = new HtmlTextWriter(sw))
                    {
                        ViewContext viewContext = new ViewContext(context, result.View, viewData, tempData, output);
                        result.View.Render(viewContext, output);
                    }
                }

                return sb.ToString();
            }
            return String.Empty;
        }
    }
}