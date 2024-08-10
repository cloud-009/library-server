using System.Text;
using library_server.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace library_server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LibraryController : ControllerBase
    {
        public LibraryController(Context context, EmailService emailService, JwtService jwtService)
        {
            Context = context;
            EmailService = emailService;
            JwtService = jwtService;
        }

        public Context Context { get; }
        public EmailService EmailService { get; }
        public JwtService JwtService { get; }

        [HttpPost("Register")]
        public ActionResult Register(User user)
        {
            user.AccountStatus = AccountStatus.UNAPPROVED;
            user.UserType = UserType.STUDENT;
            user.createdOn = DateTime.Now;

            Context.Users.Add(user);
            Context.SaveChanges();

            const string emailSubject = "Account Created";

            var sb = new StringBuilder();

            sb.AppendLine("<html>");
            sb.AppendLine("    <body>");
            sb.AppendLine($"        <h1>Hello, {user.FirstName} {user.LastName}</h1>");
            sb.AppendLine("        <h2>");
            sb.AppendLine("            Your account has been created and we have sent an approval request to the admin.");
            sb.AppendLine("            Once the request is approved by the admin, you will receive an email, and you will be");
            sb.AppendLine("            able to log in to your account.");
            sb.AppendLine("        </h2>");
            sb.AppendLine("        <h3>Thanks</h3>");
            sb.AppendLine("    </body>");
            sb.AppendLine("</html>");

            string body = sb.ToString();

            EmailService.SendEmail(user.Email, emailSubject, body);

            return Ok("Thankyou for registering. Your account has been sent for approval.");
        }

        [HttpGet("Login")]
        public ActionResult Login(string email, string password)
        {
            if(Context.Users.Any(o => o.Email.Equals(email) && o.Password.Equals(password)))
            {
                var user = Context.Users.Single(user => user.Email.Equals(email) && user.Password.Equals(password));
                if(user.AccountStatus == AccountStatus.UNAPPROVED)
                {
                    return Ok("unapproved");
                }
                if(user.AccountStatus == AccountStatus.BLOCKED)
                {
                    return Ok("blocked");
                }
                return Ok(JwtService.GenerateToken(user));
            }

            return Ok("not found");
        }

        [HttpGet("GetBooks")]
        public ActionResult GetBooks()
        {
            if (Context.Books.Any())
            {
                return Ok(Context.Books.Include(b => b.BookCategory).ToList());
            }
            return NotFound();
        }

        [HttpPost("OrderBooks")]
        public ActionResult OrderBook(int userId, int bookId)
        {
            var canOrder = Context.Orders.Count(o => o.UserId == userId && !o.Returned) < 3;
            if(canOrder)
            {
                Context.Orders.Add(new()
                {
                    UserId = userId,
                    BookId = bookId,
                    OrderDate = DateTime.Now,
                    RetrunDate = null,
                    Returned = false,
                    FinePaid = 0
                });

                var book = Context.Books.Find(bookId);
                if(book is not null)
                {
                    book.Ordered = true;
                }
                Context.SaveChanges();
                return Ok("ordered");
            }
            return Ok("cannot order");
        }

        [HttpGet("GetOrdersOfUser")]
        public ActionResult GetOrdersOrUser(int userId)
        {
            var orders = Context.Orders
                .Include(o => o.Book)
                .Include(o => o.User)
                .Where(o => o.UserId == userId)
                .ToList();
            if(orders.Any())
            {
                return Ok(orders);
            } else
            {
                return Ok("no_orders_found");
            }
        }

        [HttpPost("AddCategory")]
        public ActionResult AddCategory(BookCategory bookCategory)
        {
            var exists = Context.BookCategories.Any(b => b.Category == bookCategory.Category && b.SubCategory == bookCategory.SubCategory);
            if(exists)
            {
                return Ok("cannot insert");
            } else
            {
                Context.BookCategories.Add(bookCategory);
                Context.SaveChanges();
                return Ok("added new category");
            }
        }

        [HttpGet("GetCategories")]
        public ActionResult GetCategories()
        {
            var categories = Context.BookCategories.ToList();
            if(categories.Any())
            {
                return Ok(categories);
            }
            return NotFound();
        }

        [HttpPost("AddBook")]
        public ActionResult AddBook(Book book)
        {
            book.BookCategory = null;
            Context.Books.Add(book);
            Context.SaveChanges();
            return Ok("inserted");
        }

        [HttpDelete("DeleteBook")]
        public ActionResult DeleteBook(int id) 
        {
            var exists = Context.Books.Any(o =>  o.Id == id);
            if(exists)
            {
                var book = Context.Books.Find(id);
                Context.Books.Remove(book!);
                Context.SaveChanges();
                return Ok("deleted");
            }
            return NotFound();
        }

        [HttpGet("ReturnBook")]
        public ActionResult ReturnBook(int userId, int bookId, int fine)
        {
            var order = Context.Orders.SingleOrDefault(o => o.UserId == userId && o.BookId == bookId);
            if(order is not null)
            {
                order.Returned = true;
                order.RetrunDate = DateTime.Now;
                order.FinePaid = fine;

                var book = Context.Books.Single(o => o.Id == order.BookId);
                book.Ordered = false;

                Context.SaveChanges();

                return Ok("returned");
            }
            return Ok();
        }

        [HttpGet("GetUsers")]
        public ActionResult GetUsers()
        {
            return Ok(Context.Users.ToList());
        }

        [HttpGet("ApproveRequest")]
        public ActionResult ApproveRequest(int userId)
        {
            var user = Context.Users.Find(userId);
            if(user is not null)
            {
                if(user.AccountStatus == AccountStatus.UNAPPROVED)
                {
                    user.AccountStatus = AccountStatus.ACTIVE;
                    Context.SaveChanges();

                    var sb = new StringBuilder();

                    sb.AppendLine("<html>");
                    sb.AppendLine("<body>");
                    sb.AppendLine($"<h1>Hello, {user.FirstName} {user.LastName}</h1>");
                    sb.AppendLine("<h2>");
                    sb.AppendLine("Your account has been approved by the admin.");
                    sb.AppendLine("Now you can login to your account");
                    sb.AppendLine("</h2>");
                    sb.AppendLine("<h3>Thanks</h3>");
                    sb.AppendLine("</body>");
                    sb.AppendLine("</html>");

                    string body = sb.ToString();

                    EmailService.SendEmail(user.Email, "Account Approved", body);

                    return Ok("approved");
                }
            }
            return Ok("not approved");
        }

        [HttpGet("GetOrders")]
        public ActionResult GetOrders()
        {
            var orders = Context.Orders.Include(o => o.User).Include(o => o.Book).ToList();
            if(orders.Any())
            {
                return Ok(orders);
            } else
            {
                return NotFound();
            }
        }

        [HttpGet("SendEmailForPendingReturns")]
        public ActionResult SendEmailForPendingReturns()
        {
            var orders = Context.Orders
                .Include(o => o.Book)
                .Include(o => o.User)
                .Where(o => !o.Returned)
                .ToList();
            var emailsWithFine = orders.Where(o => DateTime.Now > o.OrderDate.AddDays(10)).ToList();
            emailsWithFine.ForEach(o => o.FinePaid = (DateTime.Now - o.OrderDate.AddDays(10)).Days * 50);

            var firstFineEmails = emailsWithFine.Where(o => o.FinePaid == 50).ToList();
            firstFineEmails.ForEach(x =>
            {
                var sb = new StringBuilder();

                sb.AppendLine("<html>");
                sb.AppendLine("<body>");
                sb.AppendLine($"<h1>Hello, {x.User?.FirstName} {x.User?.LastName}</h1>");
                sb.AppendLine("<h4>");
                sb.AppendLine($"Yesterday was your last day to return Book: {x.Book?.Title}");
                sb.AppendLine("From today, every day a fine of 50Rs will be added");
                sb.AppendLine("Return the book as soon as possible");
                sb.AppendLine("</h4>");
                sb.AppendLine("<h4>Thanks</h4>");
                sb.AppendLine("</body>");
                sb.AppendLine("</html>");

                string body = sb.ToString();

                EmailService.SendEmail(x.User!.Email, "Return Overdue", body);
            });

            var regularFineEmails = emailsWithFine.Where(o => o.FinePaid > 50 && o.FinePaid <= 500).ToList();
            regularFineEmails.ForEach(x =>
            {

                var sb = new StringBuilder();

                sb.AppendLine("<html>");
                sb.AppendLine("<body>");
                sb.AppendLine($"<h1>Hello, {x.User?.FirstName} {x.User?.LastName}</h1>");
                sb.AppendLine("<h4>");
                sb.AppendLine($"You have {x.FinePaid}Rs fine on Book: {x.Book?.Title}");
                sb.AppendLine("Return the book, and pay the fine as soon as possible");
                sb.AppendLine("</h4>");
                sb.AppendLine("<h4>Thanks</h4>");
                sb.AppendLine("</body>");
                sb.AppendLine("</html>");

                string body = sb.ToString();

                EmailService.SendEmail(x.User!.Email, "Fine To Pay", body);
            });

            var overdueFineEmails = emailsWithFine.Where(o => o.FinePaid > 500).ToList();
            overdueFineEmails.ForEach(x =>
            {
                var sb = new StringBuilder();

                sb.AppendLine("<html>");
                sb.AppendLine("<body>");
                sb.AppendLine($"<h1>Hello, {x.User?.FirstName} {x.User?.LastName}</h1>");
                sb.AppendLine("<h4>");
                sb.AppendLine($"You have {x.FinePaid}Rs fine on Book: {x.Book?.Title}.");
                sb.AppendLine("Your account is BLOCKED!!.");
                sb.AppendLine("Please pay it as soon as possible to UNBLOCK your account.");
                sb.AppendLine("</h4>");
                sb.AppendLine("<h4>Thanks</h4>");
                sb.AppendLine("</body>");
                sb.AppendLine("</html>");

                string body = sb.ToString();

                EmailService.SendEmail(x.User!.Email, "Account Blocked", body);
            });

            return Ok("sent");
        }

        [HttpGet("BlockFineOverdueUsers")]
        public ActionResult BlockFineOverduesUsers()
        {
            var orders = Context.Orders
               .Include(o => o.Book)
               .Include(o => o.User)
               .Where(o => !o.Returned)
               .ToList();
            var emailsWithFine = orders.Where(o => DateTime.Now > o.OrderDate.AddDays(10)).ToList();
            emailsWithFine.ForEach(o => o.FinePaid = (DateTime.Now - o.OrderDate.AddDays(10)).Days * 50);

            var users = emailsWithFine.Where(o => o.FinePaid > 500).Select(x => x.User).Distinct().ToList();

            if (users is not null && users.Any())
            {
                foreach (var user in users)
                {
                    user!.AccountStatus = AccountStatus.BLOCKED;
                }
                Context.SaveChanges();
                return Ok("blocked");
            }
            else
            {
                return Ok("not blocked");
            }
        }

        [HttpGet("Unblock")]
        public ActionResult Unblock(int userId)
        {
            var user = Context.Users.Find(userId);
            if(user is not null)
            {
                user.AccountStatus = AccountStatus.ACTIVE;
                Context.SaveChanges();
                return Ok("unblocked");
            }
            return Ok("not unblocked");

        }
    }
}
