using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebRole1.Models
{
    public class VirtualMachine
    {
        public string storageAccount { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Location { get; set; }
        public string VMName { get; set; }
    }
    public class Download
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public Uri SASUrl { get; set; }
    }
        
    public class tblStorage : TableEntity
    {
        public tblStorage(string id, string name)
        {
            this.PartitionKey = id;
            this.RowKey = name;
        }
        
        public tblStorage() { }
        public string address { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class EmployeeContext : DbContext
    {
        public EmployeeContext()
            : base("EmployeeContext")
        {

        }

        public DbSet<Employee> Employees { get; set; }
    }

    [Table("Employee")]
    public class Employee
    {
        [Key,DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        
        public int EmpID { get; set; }
        public string EmpName { get; set; }
        public string Address { get; set; }
        public string MobileNo { get; set; }
        public string EmployeeCode { get; set; }
    }

    public class CommonMessage
    {
        public string Message { get; set; }
        public object[] Data { get; set; }
    }

    public class ServiceBusQueue
    {
        public string QueueName { get; set; }
        public string ID { get; set; }
        public string Message { get; set; }
        public int MsgNumber { get; set; }
        public string TestProperty { get; set; }
    }
    public class ServiceBusTopic
    {
        public string Topic { get; set; }
        public string Subcription { get; set; }
        public string TPMessage { get; set; }
        public int MsgNumber { get; set; }
        public string TestProperty { get; set; }
    }
}