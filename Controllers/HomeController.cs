<<<<<<< HEAD
﻿using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using X.PagedList;

namespace WePhone.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HomeController> _logger;
        public HomeController(AppDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? pageNumber = 1, string? sortOrder = "", string? priceGroup = "price-all", string? ramGroup = "ram-all", string? romGroup = "size-all")
        {
            ViewData["pageNumber"] = pageNumber;
            ViewData["sortOrder"] = sortOrder;
            ViewData["priceGroup"] = priceGroup;
            ViewData["romGroup"] = romGroup;
            ViewData["ramGroup"] = ramGroup;


            const int pageSize = 18;

            HttpContext.Session.SetString("UserId", "11");

            var smartphones = from s in _context.Smartphones
                           select s;

            if (!String.IsNullOrEmpty(priceGroup))
            {
                switch (priceGroup)
                {
                    case "price-1":
                        smartphones = smartphones.Where(s => s.Price >= 100 && s.Price <= 500);
                        break;
                    case "price-2":
                        smartphones = smartphones.Where(s => s.Price >= 500 && s.Price <= 900);
                        break;
                    case "price-3":
                        smartphones = smartphones.Where(s => s.Price >= 900 && s.Price <= 1200);
                        break;
                    case "price-4":
                        smartphones = smartphones.Where(s => s.Price >= 1200 && s.Price <= 1600);
                        break;
                    case "price-5":
                        smartphones = smartphones.Where(s => s.Price >= 1600 && s.Price <= 2100);
                        break;

                    default:
                        break;
                }
            }

            if (!String.IsNullOrEmpty(romGroup))
            {
                switch (romGroup)
                {
                    case "size-1":
                        smartphones = smartphones.Where(s => s.Rom >= 1 && s.Rom <= 32);
                        break;
                    case "size-2":
                        smartphones = smartphones.Where(s => s.Rom >= 32 && s.Rom <= 64);
                        break;
                    case "size-3":
                        smartphones = smartphones.Where(s => s.Rom >= 64 && s.Rom <= 128);
                        break;
                    case "size-4":
                        smartphones = smartphones.Where(s => s.Rom >= 128 && s.Rom <= 512);
                        break;
                    case "size-5":
                        smartphones = smartphones.Where(s => s.Rom >= 512 && s.Rom <= 1024);
                        break;

                    default:
                        break;
                }
            }

            if (!String.IsNullOrEmpty(ramGroup))
            {
                switch (ramGroup)
                {
                    case "ram-1":
                        smartphones = smartphones.Where(s => s.Ram >= 1 && s.Ram <= 2);
                        break;
                    case "ram-2":
                        smartphones = smartphones.Where(s => s.Ram >= 2 && s.Ram <= 4);
                        break;
                    case "ram-3":
                        smartphones = smartphones.Where(s => s.Ram >= 4 && s.Ram <= 8);
                        break;
                    case "ram-4":
                        smartphones = smartphones.Where(s => s.Ram >= 8 && s.Ram <= 12);
                        break;
                    case "ram-5":
                        smartphones = smartphones.Where(s => s.Ram >= 12 && s.Ram <= 16);
                        break;

                    default:
                        break;
                }
            }

            if (!String.IsNullOrEmpty(sortOrder))
            {
                if (sortOrder == "name")
                {
                    ViewData["sortOrder"] = "Name";
                    smartphones = smartphones.OrderBy(s => s.Brand);
                }
                else if (sortOrder == "price")
                {
                    ViewData["sortOrder"] = "Price";
                    smartphones = smartphones.OrderBy(s => s.Price);
                }
                else if (sortOrder == "rom")
                {
                    ViewData["sortOrder"] = "Rom";
                    smartphones = smartphones.OrderBy(s => s.Rom);
                }
                else if (sortOrder == "ram")
                {
                    ViewData["sortOrder"] = "Ram";
                    smartphones = smartphones.OrderBy(s => s.Ram);
                }
            }

            var listphones = await PaginatedList<Smartphone>.CreateAsync(smartphones.AsNoTracking(), pageNumber ?? 1, pageSize);

            return View(listphones);
        }

        public IActionResult Success()
        {
            var userId = int.Parse(HttpContext.Session.GetString("UserId"));
            List<Cart> carts = _context.Carts.Include(c => c.Smartphone).Include(c => c.User).Where(c => c.User_Id == userId).ToList();

            var newInvoice = new Invoice
            {
                UserId = userId, 
                TotalAmount = carts.Sum(cart => (Math.Round((cart.Smartphone.Price * (1 - cart.Smartphone.Discount / 100)) * 100, 3)) * cart.Quantity), 
                created_at = DateTime.Now, 
            };
            _context.Invoices.Add(newInvoice);
            _context.SaveChanges();

            foreach (var cart in carts)
            {
                var invoiceItem = new InvoiceItem
                {
                    InvoiceId = newInvoice.Id,
                    PhoneId = cart.Phone_Id,
                    Quantity = cart.Quantity
                };

                _context.InvoiceItems.Add(invoiceItem);
            }

            _context.SaveChanges();

            return View();
        }

        public IActionResult Cart()
        {
            List<Cart> carts = _context.Carts.Include(c => c.Smartphone).Include(c => c.User).Where(c => c.User_Id == 11).ToList();
            _logger.LogInformation("This is cart");
            return View(carts);
        }

        public IActionResult PhoneDetail(int id, string? name, string? brand, int ram, int rom, decimal price, decimal discount, string? color, string? picture)
        {
            var phone = new Smartphone
            {
                Id = id,
                Name = name,
                Brand = brand,
                Ram = ram,
                Rom = rom,
                Price = price,
                Discount = discount,
                Color = color,
                Picture = picture
            };

            return View(phone);
        }

        public IActionResult Contact()
        {
            var userIdString = HttpContext.Session.GetString("UserId");

            return View();
        }

        [HttpPost]
        public IActionResult CreateCheckoutSession(string amount)
        {
            var domain = "https://localhost:7278/";
            var options = new SessionCreateOptions
            {

                SuccessUrl = domain + "Home/Success",
                CancelUrl = domain + "Home/Cart",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };
            List<Cart> carts = _context.Carts.Include(c => c.Smartphone).Include(c => c.User).Where(c => c.User_Id == 11).ToList();

            foreach (var item in carts)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)( Math.Round((item.Smartphone.Price * (1 - item.Smartphone.Discount / 100)) * 100, 3) ),
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Smartphone.Name,
                        }
                    },
                    Quantity = item.Quantity
                };
                options.LineItems.Add(sessionLineItem);
            }
            var service = new SessionService();
            var session = service.Create(options);
            Response.Headers.Add("Location", session.Url);

            return Redirect(session.Url);
=======
﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace WePhone.Controllers
{
    [Authorize]
    public class HomeController : Controller
	{
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Access");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
>>>>>>> 46244835a8587cd96493a7643ed62423f394e15c
        }
    }
}
