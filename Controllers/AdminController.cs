using Demo1.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Razor.Tokenizer.Symbols;

namespace Demo1.Controllers
{
    public class AdminController : Controller
    {
        BookStoreDBEntities database = new BookStoreDBEntities();
        private void LoadBookDropdowns()
        {
            ViewBag.Authors = new SelectList(database.Authors.ToList(), "AuthorID", "AuthorName");
            ViewBag.Categories = new SelectList(database.Categories.ToList(), "CategoryID", "CategoryName");
            ViewBag.Publishers = new SelectList(database.Publishers.ToList(), "PublisherID", "PublisherName");
        }
        public ActionResult Employee()
        {
            return View();
        }
        //Account
        public ActionResult AccountList(string keyword)
        {
            var query = database.Accounts
                .Where(a => a.Role == "Customer")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(a => a.Username.Contains(keyword)
                                       || a.Customer.FullName.Contains(keyword)
                                       || a.Customer.Email.Contains(keyword));
            }

            ViewBag.Keyword = keyword;
            return View(query.OrderByDescending(a => a.AccountID).ToList());
        }

         [HttpGet]
        public ActionResult AccountDelete(int id)
        {
            var account = database.Accounts.Find(id);
            if (account == null)
                return HttpNotFound();
            return View(account);
        }

        [HttpPost]
        [ActionName("AccountDelete")]
        public ActionResult AccountDeleteConfirmed(int id)
        {
            var account = database.Accounts.Find(id);
            if (account == null)
                return HttpNotFound();

            using (var transaction = database.Database.BeginTransaction())
            {
                try
                {
                    var customer = database.Customers.Find(account.CustomerID);

                    database.Accounts.Remove(account);
                    database.SaveChanges();

                    if (customer != null)
                    {
                        database.Customers.Remove(customer);
                        database.SaveChanges();
                    }

                    transaction.Commit();
                    TempData["Success5"] = "Delete Successful";
                }
                catch (DbUpdateException)
                {
                    transaction.Rollback();
                    TempData["Error"] = "Delete fail";
                }
            }

            return RedirectToAction("AccountList");
        }


        //Category
        // Category
        public ActionResult CategoryList(string keyword)
        {
            var query = database.Categories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x => x.CategoryName.Contains(keyword));
            }

            ViewBag.Keyword = keyword;

            return View(query.OrderBy(x => x.CategoryID).ToList());
        }
        [HttpGet]
        public ActionResult CategoryAdd()
        {
            return View();
        }

        [HttpPost]
        public ActionResult CategoryAdd(Category model)
        {
            if (!ModelState.IsValid)
                return View(model);

            database.Categories.Add(model);
            database.SaveChanges();

            TempData["Success10"] = "ADD GENRE SUCCESSFUL";
            return RedirectToAction("CategoryList");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CategoryDelete(int id)
        {
            var cate = database.Categories.Find(id);

            if (cate == null)
                return HttpNotFound();

            if (database.Books.Any(x => x.CategoryID == id))
            {
                TempData["Error2"] = "Cannot delete beacause still have book in this genre";
                return RedirectToAction("CategoryList");
            }

            database.Categories.Remove(cate);
            database.SaveChanges();

            TempData["Success"] = "Delete successful";

            return RedirectToAction("CategoryList");
        }
        public ActionResult CategoryDetail(int id)
        {
            var category = database.Categories.Find(id);

            if (category == null)
                return HttpNotFound();

            ViewBag.Category = category;

            var books = database.Books
                                .Where(x => x.CategoryID == id)
                                .ToList();

            return View(books);
        }


        //Order
        public ActionResult CancelOrder(int id)
        {
            var order = database.Orders.Find(id);

            if (order == null)
                return HttpNotFound();
            if (order.Status != "Chờ xác nhận")
            {
                TempData["Error"] = "CANNOT CANCEL THIS ORDER";
                return RedirectToAction("OrderList");
            }

            using (var transaction = database.Database.BeginTransaction())
            {
                try
                {
                    // Trả lại số lượng sách
                    var details = database.OrderDetails
                                          .Where(x => x.OrderID == id)
                                          .ToList();

                    foreach (var item in details)
                    {
                        var book = database.Books.Find(item.BookID);

                        if (book != null)
                        {
                            book.Quantity += item.Quantity ?? 0;
                        }
                    }
                    // Xóa chi tiết đơn hàng
                    database.OrderDetails.RemoveRange(details);

                    // Xóa đơn hàng
                    database.Orders.Remove(order);

                    database.SaveChanges();

                    transaction.Commit();

                    TempData["Success9"] = "ORDER CANCEL SUCCESSFUL";
                }
                catch
                {
                    transaction.Rollback();
                    TempData["Error"] = "CANCEL FAIL";
                }
            }

            return RedirectToAction("OrderList");
        }
        public ActionResult OrderList(string keyword)
        {
            var orders = database.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                orders = orders.Where(x =>
                    x.Customer.FullName.Contains(keyword) ||
                    x.OrderID.ToString().Contains(keyword));
            }

            ViewBag.Keyword = keyword;

            return View(orders
                .OrderByDescending(x => x.OrderID)
                .ToList());
        }
        public ActionResult OrderDetail(int id)
        {
            var order = database.Orders.Find(id);

            if (order == null)
                return HttpNotFound();

            ViewBag.Details = database.OrderDetails
                .Where(x => x.OrderID == id)
                .ToList();

            return View(order);
        }
        public ActionResult ConfirmOrder(int id)
        {
            var order = database.Orders.Find(id);

            if (order == null)
                return HttpNotFound();

            order.Status = "Đang giao";

            database.SaveChanges();

            TempData["Success7"] = "CONFIRM SUCCESSFUL";

            return RedirectToAction("OrderList");
        }
        public ActionResult DeliveredOrder(int id)
        {
            var order = database.Orders.Find(id);

            if (order == null)
                return HttpNotFound();

            order.Status = "Đã giao";

            database.SaveChanges();

            TempData["Success8"] = "ORDER DONE";

            return RedirectToAction("OrderList");
        }
        //Book
        [HttpGet]
        public ActionResult BookList(string keyword)
        {
            var query = database.Books.AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(b => b.BookName.Contains(keyword));
            }

            ViewBag.Keyword = keyword;
            return View(query.OrderByDescending(a => a.BookID).ToList());
        }

        [HttpGet]
        public ActionResult BookDelete(int id)
        {
            var book = database.Books.Find(id);
            if (book == null)
                return HttpNotFound();
            return View(book);
        }

        [HttpPost]
        [ActionName("BookDelete")]
        public ActionResult BookDeleteConfirmed(int id)
        {
            var book = database.Books.Find(id);
            if (book == null)
                return HttpNotFound();

            using (var transaction = database.Database.BeginTransaction())
            {
                try
                {
                    var manybook = database.Books.Find(book.BookID);
                    database.Books.Remove(book);
                    database.SaveChanges();
                    transaction.Commit();
                    TempData["Success5"] = "Delete Successful";
                }
                catch (DbUpdateException)
                {
                    transaction.Rollback();
                    TempData["Error"] = "Delete fail";
                }
            }

            return RedirectToAction("BookList");
        }

        [HttpGet]
        public ActionResult BookADD()
        {
            LoadBookDropdowns();
            return View();
        }

        [HttpPost]
        public ActionResult BookADD(Book model, HttpPostedFileBase ImageFile)
        {
            if (!ModelState.IsValid)
            {
                LoadBookDropdowns();
                return View(model);
            }

            if (ImageFile != null && ImageFile.ContentLength > 0)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                string path = Server.MapPath("~/image/" + fileName);
                ImageFile.SaveAs(path);
                model.Image = "/image/" + fileName;
            }

            model.Status = true;
            database.Books.Add(model);
            database.SaveChanges();

            TempData["Success11"] = "Add book succesful";
            return RedirectToAction("BookList");
        }
        [HttpGet]
        public ActionResult BookEdit(int id)
        {
            var book = database.Books.Find(id);
            if (book == null)
                return HttpNotFound();

            LoadBookDropdowns();
            return View(book);
        }

        [HttpPost]
        public ActionResult BookEdit(Book model, HttpPostedFileBase ImageFile)
        {
            var book = database.Books.Find(model.BookID);
            if (book == null)
                return HttpNotFound();

            if (!ModelState.IsValid)
            {
                LoadBookDropdowns();
                return View(model);
            }

            book.BookName = model.BookName;
            book.AuthorID = model.AuthorID;
            book.CategoryID = model.CategoryID;
            book.PublisherID = model.PublisherID;
            book.Price = model.Price;
            book.Quantity = model.Quantity;
            book.Description = model.Description;
            book.PublishYear = model.PublishYear;
            book.Status = model.Status;

            if (ImageFile != null && ImageFile.ContentLength > 0)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                string path = Server.MapPath("~/image/" + fileName);
                ImageFile.SaveAs(path);
                book.Image = "/image/" + fileName;
            }

            database.SaveChanges();

            TempData["Success12"] = "Update book successful";
            return RedirectToAction("BookList");
        }

    }
}