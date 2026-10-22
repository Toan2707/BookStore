using Demo1.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Demo1.Controllers
{
    public class HomePageController : Controller
    {
        BookStoreDBEntities database = new BookStoreDBEntities();
        // GET: HomePage

        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Contact()
        {
            return View();
        }
        [HttpGet]
        public ActionResult AccountEdit(int id)
        {
            var account = database.Accounts.Find(id);
            if (account == null)
                return HttpNotFound();

            var model = new AccountEditViewModel
            {
                AccountID = account.AccountID,
                Username = account.Username,
                Status = account.Status ?? true,
                FullName = account.Customer.FullName,
                Gender = account.Customer.Gender,
                Birthday = account.Customer.Birthday,
                Phone = account.Customer.Phone,
                Email = account.Customer.Email,
                Address = account.Customer.Address
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult AccountEdit(AccountEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var account = database.Accounts.Find(model.AccountID);
            if (account == null)
                return HttpNotFound();

            account.Username = model.Username;
            account.Status = model.Status;
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                account.Password = model.Password;
            }

            var customer = database.Customers.Find(account.CustomerID);
            customer.FullName = model.FullName;
            customer.Gender = model.Gender;
            customer.Birthday = model.Birthday;
            customer.Phone = model.Phone;
            customer.Email = model.Email;
            customer.Address = model.Address;

            database.SaveChanges();

            TempData["Success4"] = "Edit Successful";
            return RedirectToAction("BS");
        }
        public ActionResult BS()
        {
            var books = database.Books
                .Where(b => b.Status == true)
                .OrderByDescending(b => b.BookID)
                .Take(12)
            .ToList();

            ViewBag.Categories = database.Categories.ToList();

            return View(books);
            
        }
   
        public ActionResult About()
        {
            return View();
        }
        public ActionResult BookDetail(int id)
        {
            var book = database.Books
                               .Include(b => b.Author)
                               .Include(b => b.Category)
                               .Include(b => b.Publisher)
                               .FirstOrDefault(b => b.BookID == id && b.Status == true);

            if (book == null)
                return HttpNotFound();

            return View(book);
        }

        public ActionResult Book(string keyword)
        {
            var books = database.Books.Where(b => b.Status == true);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                books = books.Where(b =>
                    b.BookName.Contains(keyword) ||
                    b.Author.AuthorName.Contains(keyword) ||
                    b.Category.CategoryName.Contains(keyword));
            }

            ViewBag.Keyword = keyword;
            ViewBag.Categories = database.Categories.ToList();

            return View(books
                .OrderByDescending(b => b.BookID)
                .ToList());
        }
        public ActionResult Category(int id)
        {
            var books = database.Books.Where(b => b.CategoryID == id).ToList();

            return View("Book", books);
        }

    }
}