using Demo1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;


namespace Demo1.Controllers
{
    public class LoginController : Controller
    {
        BookStoreDBEntities database = new BookStoreDBEntities();
        
        public ActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            var account = database.Accounts.FirstOrDefault(m => m.Username.Equals(model.Username) && m.Password.Equals(model.Password) && m.Status == true);
            if (account == null)
            {
                ViewBag.Err = "UserName or Password is not true";
                return View(model);
            }
            TempData["Success3"] = "Login Successful";
            TempData["Success"] = "Logout Successful";
            Session["Account ID"] = account.AccountID;
            Session["UserName"] = account.Username;
            Session["Password"] = account.Password;
            Session["Customer"] = account.CustomerID;
            Session["Role"] = account.Role;
            Session["Image"] = account.Image;
            if (account.Role == "Admin" || account.Role == "Employee")
            {
                return RedirectToAction("AccountList", "Admin");
            }
            return RedirectToAction("BS", "HomePage");
        }
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }
        [HttpGet]
        public ActionResult Register()
        {
            RegisterViewModel model = new RegisterViewModel();
            model.Image = "/image/matbiec.jpg";
            return View(model);
        }
        [HttpPost]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Image = "/image/jpg";
                return View(model);
            }
            if (database.Accounts.Any(m => m.Username.Equals(model.Username)))
            {
                ViewBag.ErrUser = "UserName is exited";
                model.Image = "/image/jpg";
                return View(model);
            }

            string image = "/image/matbiec.jpg";
            if (model.ImageFile != null && model.ImageFile.ContentLength > 0)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(model.ImageFile.FileName);
                string path = Server.MapPath("~/image/" + fileName);
                model.ImageFile.SaveAs(path);
                image = "/image/" + fileName;
            }
            Customer customer = new Customer();

            customer.FullName = model.FullName;
            customer.Gender = model.Gender;
            customer.Birthday = model.Birthday;
            customer.Phone = model.Phone;
            customer.Email = model.Email;
            customer.Address = model.Address;

            database.Customers.Add(customer);
            database.SaveChanges();    

 
            Account newAccount = new Account();

            newAccount.Username = model.Username;
            newAccount.Password = model.Password;
            newAccount.Role = "Customer";
            newAccount.CustomerID = customer.CustomerID; 
            newAccount.Image = image;
            newAccount.Status = true;
            newAccount.CreatedDate = DateTime.Now;

            database.Accounts.Add(newAccount);
            database.SaveChanges();
            TempData["Success2"] = "Register Successful";
            return RedirectToAction("Login");

        }
    }
}