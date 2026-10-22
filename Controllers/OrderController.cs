using Demo1.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace Demo1.Controllers
{
    public class OrderController : Controller
    {
        BookStoreDBEntities database = new BookStoreDBEntities();

        public ActionResult Index()
        {
            if (Session["Customer"] == null)
                return RedirectToAction("Login", "Login");

            int customerId = (int)Session["Customer"];
            var items = database.Carts.Where(c => c.CustomerID == customerId).ToList();
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateQuantity(int cartId, int quantity)
        {
            if (Session["Customer"] == null)
                return RedirectToAction("Login", "Login");

            var item = database.Carts.Find(cartId);
            if (item != null && item.CustomerID == (int)Session["Customer"] && quantity > 0)
            {
                item.Quantity = quantity;
                database.SaveChanges();
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public JsonResult AddToCart(int bookId, int quantity = 1)
        {
            if (Session["Customer"] == null)
            {
                return Json(new { success = false, needLogin = true, message = "Bạn cần đăng nhập để thêm vào giỏ hàng." });
            }

            int customerId = (int)Session["Customer"];

            var book = database.Books.FirstOrDefault(b => b.BookID == bookId && b.Status == true);
            if (book == null || book.Quantity <= 0)
            {
                return Json(new { success = false, needLogin = false, message = "Sách không tồn tại hoặc đã hết hàng." });
            }

            var cartItem = database.Carts.FirstOrDefault(c => c.CustomerID == customerId && c.BookID == bookId);
            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                database.Carts.Add(new Cart
                {
                    CustomerID = customerId,
                    BookID = bookId,
                    Quantity = quantity
                });
            }

            database.SaveChanges();
            return Json(new { success = true, needLogin = false, message = "" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveFromCart(int cartId)
        {
            var item = database.Carts.Find(cartId);
            if (item != null && Session["Customer"] != null && item.CustomerID == (int)Session["Customer"])
            {
                database.Carts.Remove(item);
                database.SaveChanges();
            }
            return RedirectToAction("Index");
        }
        //Mua ngay
        [HttpGet]
        public ActionResult BuyNow(int bookId)
        {
            if (Session["Customer"] == null)
                return RedirectToAction("Login", "Login");

            var book = database.Books.FirstOrDefault(b => b.BookID == bookId && b.Status == true);
            if (book == null)
                return HttpNotFound();

            var customer = database.Customers.Find((int)Session["Customer"]);

            var model = new CheckoutViewModel
            {
                BookID = book.BookID,
                Quantity = 1,
                FullName = customer?.FullName,
                Phone = customer?.Phone,
                Address = customer?.Address
            };

            ViewBag.Book = book;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BuyNow(CheckoutViewModel model)
        {
            if (Session["Customer"] == null)
                return RedirectToAction("Login", "Login");

            var book = database.Books.FirstOrDefault(b => b.BookID == model.BookID && b.Status == true);
            if (book == null)
                return HttpNotFound();

            if (model.Quantity <= 0) model.Quantity = 1;

            int currentStock = book.Quantity ?? 0;
            if (currentStock < model.Quantity)
            {
                ViewBag.Book = book;
                ViewBag.Err = "Số lượng vượt quá tồn kho hiện có (" + currentStock + ")";
                return View(model);
            }

            int customerId = (int)Session["Customer"];

            using (var transaction = database.Database.BeginTransaction())
            {
                try
                {
                    var order = new Order
                    {
                        CustomerID = customerId,
                        OrderDate = DateTime.Now,
                        TotalMoney = book.Price * model.Quantity,
                        Status = "Chờ xác nhận",
                        ShippingAddress = model.Address
                    };
                    database.Orders.Add(order);
                    database.SaveChanges();

                    database.OrderDetails.Add(new OrderDetail
                    {
                        OrderID = order.OrderID,
                        BookID = book.BookID,
                        Quantity = model.Quantity,
                        Price = book.Price
                    });

                    book.Quantity = currentStock - model.Quantity;

                    database.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    TempData["Error"] = "Đặt hàng thất bại, vui lòng thử lại.";
                    return RedirectToAction("BuyNow", new { bookId = book.BookID });
                }
            }

            TempData["Success"] = "Đặt hàng thành công! Đơn hàng của bạn đang chờ xác nhận.";
            return RedirectToAction("MyOrders");
        }

        //CHECKOUT TỪ GIỎ HÀNG
        [HttpGet]
        public ActionResult Checkout()
        {
            if (Session["Customer"] == null)
                return RedirectToAction("Login", "Login");

            int customerId = (int)Session["Customer"];
            var cartItems = database.Carts.Where(c => c.CustomerID == customerId).ToList();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            var customer = database.Customers.Find(customerId);
            var model = new CheckoutViewModel
            {
                FullName = customer?.FullName,
                Phone = customer?.Phone,
                Address = customer?.Address
            };

            ViewBag.CartItems = cartItems;
            ViewBag.Total = cartItems.Sum(c => c.Book.Price * c.Quantity);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(CheckoutViewModel model)
        {
            if (Session["Customer"] == null)
                return RedirectToAction("Login", "Login");

            int customerId = (int)Session["Customer"];
            var cartItems = database.Carts.Where(c => c.CustomerID == customerId).ToList();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            using (var transaction = database.Database.BeginTransaction())
            {
                try
                {
                    var order = new Order
                    {
                        CustomerID = customerId,
                        OrderDate = DateTime.Now,
                        TotalMoney = cartItems.Sum(c => c.Book.Price * c.Quantity),
                        Status = "Chờ xác nhận",
                        ShippingAddress = model.Address
                    };
                    database.Orders.Add(order);
                    database.SaveChanges();

                    foreach (var item in cartItems)
                    {
                        database.OrderDetails.Add(new OrderDetail
                        {
                            OrderID = order.OrderID,
                            BookID = item.BookID,
                            Quantity = item.Quantity,
                            Price = item.Book.Price
                        });

                        item.Book.Quantity = (item.Book.Quantity ?? 0) - item.Quantity;
                        database.Carts.Remove(item);
                    }

                    database.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    TempData["Error"] = "Đặt hàng thất bại, vui lòng thử lại.";
                    return RedirectToAction("Checkout");
                }
            }

            TempData["Success"] = "Đặt hàng thành công! Đơn hàng của bạn đang chờ xác nhận.";
            return RedirectToAction("MyOrders");
        }

        //THEO DÕI ĐƠN HÀNG
        public ActionResult MyOrders()
        {
            if (Session["Customer"] == null)
                return RedirectToAction("Login", "Login");

            int customerId = (int)Session["Customer"];
            var orders = database.Orders
                .Where(o => o.CustomerID == customerId)
                .OrderByDescending(o => o.OrderID)
                .ToList();

            return View(orders);
        }

        public ActionResult OrderDetails(int id)
        {
            if (Session["Customer"] == null)
                return RedirectToAction("Login", "Login");

            int customerId = (int)Session["Customer"];
            var order = database.Orders.FirstOrDefault(o => o.OrderID == id && o.CustomerID == customerId);
            if (order == null)
                return HttpNotFound();

            ViewBag.Details = database.OrderDetails.Where(d => d.OrderID == id).ToList();
            return View(order);
        }

        protected override void Dispose(bool disposing)
        {
            database.Dispose();
            base.Dispose(disposing);
        }
    }
}