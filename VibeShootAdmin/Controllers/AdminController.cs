using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using VibeShootAdmin.Data;
using VibeShootAdmin.Models;
using VibeShootAdmin.Models.Admin;
using VibeShootAdmin.Models.Entities;
using VibeShootAdmin.Services;
using AdminAccount = VibeShootAdmin.Models.Entities.Admin;

namespace VibeShootAdmin.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private const int PageSize = 15;

        private readonly ApplicationDbContext _context;
        private readonly ImageStorage _storage;

        public AdminController(ApplicationDbContext context, ImageStorage storage)
        {
            _context = context;
            _storage = storage;
        }

        /// <summary>Data every admin page needs: sidebar badges and the photographer switcher.</summary>
        public override async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var scope = User.ScopedPhotographerId();
                ViewBag.IsSuperAdmin = User.IsSuperAdmin();
                ViewBag.AdminName = User.Identity.Name;
                ViewBag.Photographers = await _context.Photographers
                    .Where(p => scope == null || p.Id == scope)
                    .OrderBy(p => p.SortOrder)
                    .Select(p => new PhotographerOption(p.Id, p.Name, p.LogoPath))
                    .ToListAsync();
                ViewBag.PendingPaymentCount = await _context.Payments.ForPhotographer(scope)
                    .CountAsync(p => p.Status == PaymentStatus.ForVerification);
                ViewBag.PendingBookingCount = await _context.Bookings.ForPhotographer(scope)
                    .CountAsync(b => b.Status == BookingStatus.Pending);
            }
            await next();
        }

        // ------------------------------------------------------------------ Auth

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(Dashboard));
            SetLoginArt();
            return View("~/Views/Admin/Login.cshtml", new AdminLoginViewModel());
        }

        /// <summary>Backdrop photo for the sign-in screen.</summary>
        private void SetLoginArt()
        {
            ViewBag.LoginArt = _context.GalleryImages.Where(g => g.IsFeatured)
                .OrderBy(g => EF.Functions.Random()).Select(g => g.FilePath).FirstOrDefault();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AdminLoginViewModel model)
        {
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == model.Username);

            if (admin != null && !string.IsNullOrEmpty(model.Password) &&
                new PasswordHasher<AdminAccount>().VerifyHashedPassword(admin, admin.PasswordHash, model.Password) != PasswordVerificationResult.Failed)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, admin.Username),
                    new Claim("Role", admin.Role),
                    new Claim("PhotographerId", admin.PhotographerId?.ToString() ?? "0")
                };

                var identity = new ClaimsIdentity(claims, "VibeShootAdminCookie");
                await HttpContext.SignInAsync("VibeShootAdminCookie", new ClaimsPrincipal(identity));
                return RedirectToAction(nameof(Dashboard));
            }

            model.ErrorMessage = "Invalid username or password.";
            model.Password = "";
            SetLoginArt();
            return View("~/Views/Admin/Login.cshtml", model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("VibeShootAdminCookie");
            return RedirectToAction(nameof(Login));
        }

        // ------------------------------------------------------------------ Overview

        [HttpGet]
        public async Task<IActionResult> Dashboard(int? photographerId)
        {
            var pid = User.EffectivePhotographerId(photographerId);
            ViewBag.SelectedPhotographerId = pid;

            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var chartStart = monthStart.AddMonths(-5);

            var bookings = _context.Bookings.ForPhotographer(pid);
            var payments = _context.Payments.ForPhotographer(pid);

            var verified = await payments
                .Where(p => p.Status == PaymentStatus.Verified && p.VerifiedAt >= chartStart.AddMonths(-1))
                .Select(p => new { p.Amount, p.VerifiedAt })
                .ToListAsync();

            var createdPerMonth = await bookings
                .Where(b => b.CreatedAt >= chartStart)
                .Select(b => b.CreatedAt)
                .ToListAsync();

            var model = new DashboardViewModel
            {
                TotalBookings = await bookings.CountAsync(),
                PendingBookings = await bookings.CountAsync(b => b.Status == BookingStatus.Pending),
                ConfirmedUpcoming = await bookings.CountAsync(b => b.Status == BookingStatus.Confirmed && b.TargetDate >= today),
                PaymentsForVerification = await payments.CountAsync(p => p.Status == PaymentStatus.ForVerification),
                AmountForVerification = await payments.Where(p => p.Status == PaymentStatus.ForVerification).SumAsync(p => (decimal?)p.Amount) ?? 0,
                RevenueThisMonth = verified.Where(p => p.VerifiedAt >= monthStart).Sum(p => p.Amount),
                RevenueLastMonth = verified.Where(p => p.VerifiedAt >= monthStart.AddMonths(-1) && p.VerifiedAt < monthStart).Sum(p => p.Amount),
                StatusCounts = await bookings.GroupBy(b => b.Status).ToDictionaryAsync(g => g.Key, g => g.Count()),
            };

            for (int i = 0; i < 6; i++)
            {
                var m = chartStart.AddMonths(i);
                model.Monthly.Add(new MonthlyRevenue
                {
                    Label = m.ToString("MMM yyyy"),
                    Amount = verified.Where(p => p.VerifiedAt >= m && p.VerifiedAt < m.AddMonths(1)).Sum(p => p.Amount),
                    Bookings = createdPerMonth.Count(c => c >= m && c < m.AddMonths(1)),
                });
            }

            var active = await bookings
                .Include(b => b.Payments)
                .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending)
                .ToListAsync();
            model.OutstandingBalance = active.Where(b => b.Status == BookingStatus.Confirmed).Sum(b => b.Balance);

            model.Upcoming = active
                .Where(b => b.TargetDate >= today)
                .OrderBy(b => b.TargetDate).ThenBy(b => b.StartTime)
                .Take(6).ToList();
            foreach (var b in model.Upcoming)
            {
                b.Photographer = await _context.Photographers.FindAsync(b.PhotographerId);
            }

            model.RecentPayments = await payments
                .Include(p => p.Booking)
                .OrderByDescending(p => p.CreatedAt)
                .Take(6)
                .ToListAsync();

            return View("~/Views/Admin/Dashboard.cshtml", model);
        }

        // ------------------------------------------------------------------ Schedule

        [HttpGet]
        public IActionResult Schedule(int? photographerId)
        {
            var pid = User.EffectivePhotographerId(photographerId);
            ViewBag.SelectedPhotographerId = pid;
            return View("~/Views/Admin/Schedule.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetScheduleData(int? photographerId, int year, int month)
        {
            var pid = User.EffectivePhotographerId(photographerId);
            var from = new DateTime(year, month, 1).AddDays(-7);
            var to = from.AddDays(50);

            var bookings = await _context.Bookings.ForPhotographer(pid)
                .Include(b => b.Photographer)
                .Where(b => b.TargetDate >= from && b.TargetDate <= to && b.Status != BookingStatus.Declined && b.Status != BookingStatus.Cancelled)
                .OrderBy(b => b.TargetDate).ThenBy(b => b.StartTime)
                .ToListAsync();

            var blocked = await _context.BlockedDates
                .Where(b => (pid == null || b.PhotographerId == pid) && b.Date >= from && b.Date <= to)
                .Select(b => new { date = b.Date.ToString("yyyy-MM-dd"), b.PhotographerId, b.Reason })
                .ToListAsync();

            return Json(new
            {
                bookings = bookings.Select(b => new
                {
                    id = b.TransactionId,
                    date = b.TargetDate.ToString("yyyy-MM-dd"),
                    time = b.TimeRange,
                    client = b.ClientName,
                    status = b.Status,
                    package = $"{b.Category} · {b.PackageName}",
                    venue = b.Venue,
                    photographer = b.Photographer?.Name,
                }),
                blocked
            });
        }

        public class BlockDateBody
        {
            public string Date { get; set; } = "";
            public int? PhotographerId { get; set; }
            public string? Reason { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlockDate([FromBody] BlockDateBody request)
        {
            var pid = User.EffectivePhotographerId(request.PhotographerId);
            if (pid == null) return BadRequest(new { error = "Choose a photographer first, then block dates for them." });

            if (!DateTime.TryParseExact(request.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return BadRequest(new { error = "Invalid date." });

            var existing = await _context.BlockedDates.FirstOrDefaultAsync(b => b.PhotographerId == pid && b.Date == date.Date);
            if (existing != null)
            {
                _context.BlockedDates.Remove(existing);
            }
            else
            {
                _context.BlockedDates.Add(new BlockedDate { PhotographerId = pid.Value, Date = date.Date, Reason = request.Reason });
            }
            await _context.SaveChangesAsync();
            return Ok(new { blocked = existing == null });
        }

        // ------------------------------------------------------------------ Bookings

        [HttpGet]
        public async Task<IActionResult> Bookings([FromQuery] PagedFilter filter)
        {
            var pid = User.EffectivePhotographerId(filter.PhotographerId);
            filter.PhotographerId = pid;
            ViewBag.SelectedPhotographerId = pid;

            var baseQuery = _context.Bookings.ForPhotographer(pid);
            if (!string.IsNullOrWhiteSpace(filter.Q))
            {
                var q = filter.Q.Trim();
                baseQuery = baseQuery.Where(b => b.TransactionId.Contains(q) || b.ClientName.Contains(q) || b.ContactNumber.Contains(q) || b.Venue.Contains(q));
            }
            if (filter.From != null) baseQuery = baseQuery.Where(b => b.TargetDate >= filter.From);
            if (filter.To != null) baseQuery = baseQuery.Where(b => b.TargetDate <= filter.To);

            var counts = await baseQuery.GroupBy(b => b.Status).ToDictionaryAsync(g => g.Key, g => g.Count());

            var query = baseQuery;
            if (!string.IsNullOrEmpty(filter.Status)) query = query.Where(b => b.Status == filter.Status);

            var total = await query.CountAsync();
            var model = new BookingsViewModel
            {
                Filter = filter,
                StatusCounts = counts,
                TotalItems = total,
                TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize)),
                Items = await query
                    .Include(b => b.Photographer)
                    .Include(b => b.Payments)
                    .OrderByDescending(b => b.CreatedAt)
                    .Skip((Math.Max(1, filter.Page) - 1) * PageSize).Take(PageSize)
                    .ToListAsync()
            };

            return View("~/Views/Admin/Bookings.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> BookingDetails(string id)
        {
            var booking = await LoadBookingAsync(id);
            if (booking == null) return NotFound();
            return View("~/Views/Admin/BookingDetails.cshtml", booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string transactionId, string status, string? remarks, string? returnUrl)
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.TransactionId == transactionId);
            if (booking == null || !User.CanAccess(booking.PhotographerId)) return NotFound();
            if (!BookingStatus.All.Contains(status)) return BadRequest();

            booking.Status = status;
            if (!string.IsNullOrWhiteSpace(remarks)) booking.AdminRemarks = remarks.Trim();
            booking.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Toast"] = $"Booking {booking.TransactionId} marked as {status}.";
            return LocalRedirect(returnUrl ?? Url.Action(nameof(BookingDetails), new { id = transactionId })!);
        }

        // ------------------------------------------------------------------ Transactions

        [HttpGet]
        public async Task<IActionResult> Transactions([FromQuery] PagedFilter filter)
        {
            var pid = User.EffectivePhotographerId(filter.PhotographerId);
            filter.PhotographerId = pid;
            ViewBag.SelectedPhotographerId = pid;

            var query = FilterPayments(filter, pid);

            var totals = await query.GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Sum = g.Sum(p => p.Amount), Count = g.Count() })
                .ToListAsync();

            if (!string.IsNullOrEmpty(filter.Status)) query = query.Where(p => p.Status == filter.Status);

            var total = await query.CountAsync();
            var model = new TransactionsViewModel
            {
                Filter = filter,
                TotalItems = total,
                TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize)),
                VerifiedTotal = totals.FirstOrDefault(t => t.Status == PaymentStatus.Verified)?.Sum ?? 0,
                ForVerificationTotal = totals.FirstOrDefault(t => t.Status == PaymentStatus.ForVerification)?.Sum ?? 0,
                ForVerificationCount = totals.FirstOrDefault(t => t.Status == PaymentStatus.ForVerification)?.Count ?? 0,
                RejectedTotal = totals.FirstOrDefault(t => t.Status == PaymentStatus.Rejected)?.Sum ?? 0,
                Items = await query
                    .Include(p => p.Booking).ThenInclude(b => b!.Photographer)
                    .OrderBy(p => p.Status == PaymentStatus.ForVerification ? 0 : 1)
                    .ThenByDescending(p => p.CreatedAt)
                    .Skip((Math.Max(1, filter.Page) - 1) * PageSize).Take(PageSize)
                    .ToListAsync()
            };

            return View("~/Views/Admin/Transactions.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPayment(int id, string? returnUrl)
        {
            var payment = await _context.Payments.Include(p => p.Booking).ThenInclude(b => b!.Payments).FirstOrDefaultAsync(p => p.Id == id);
            if (payment?.Booking == null || !User.CanAccess(payment.Booking.PhotographerId)) return NotFound();

            payment.Status = PaymentStatus.Verified;
            payment.VerifiedAt = DateTime.UtcNow;
            payment.VerifiedBy = User.Identity?.Name;

            // A verified down payment secures the slot.
            if (payment.Booking.Status == BookingStatus.Pending && payment.Booking.AmountPaid >= payment.Booking.DownPaymentRequired)
            {
                payment.Booking.Status = BookingStatus.Confirmed;
            }
            payment.Booking.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Toast"] = $"Payment {payment.ReceiptNumber} verified (₱{payment.Amount:N2}).";
            return LocalRedirect(returnUrl ?? Url.Action(nameof(Transactions))!);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPayment(int id, string? remarks, string? returnUrl)
        {
            var payment = await _context.Payments.Include(p => p.Booking).FirstOrDefaultAsync(p => p.Id == id);
            if (payment?.Booking == null || !User.CanAccess(payment.Booking.PhotographerId)) return NotFound();

            payment.Status = PaymentStatus.Rejected;
            payment.Remarks = string.IsNullOrWhiteSpace(remarks) ? "Payment could not be verified." : remarks.Trim();
            payment.VerifiedAt = DateTime.UtcNow;
            payment.VerifiedBy = User.Identity?.Name;
            await _context.SaveChangesAsync();

            TempData["Toast"] = $"Payment {payment.ReceiptNumber} rejected.";
            return LocalRedirect(returnUrl ?? Url.Action(nameof(Transactions))!);
        }

        /// <summary>Records a payment received outside the website (e.g. cash on the event day).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordPayment(string transactionId, decimal amount, string method, string? referenceNumber)
        {
            var booking = await _context.Bookings.Include(b => b.Payments).FirstOrDefaultAsync(b => b.TransactionId == transactionId);
            if (booking == null || !User.CanAccess(booking.PhotographerId)) return NotFound();

            if (amount <= 0)
            {
                TempData["ToastError"] = "Enter an amount greater than zero.";
                return RedirectToAction(nameof(BookingDetails), new { id = transactionId });
            }

            booking.Payments.Add(new Payment
            {
                ReceiptNumber = IdGenerator.ReceiptNumber(),
                Amount = Math.Round(amount, 2),
                Method = method == PaymentMethod.GCash ? PaymentMethod.GCash : PaymentMethod.Cash,
                Type = booking.AmountPaid > 0 || booking.Payments.Any(p => p.Type == PaymentType.DownPayment) ? PaymentType.Balance : PaymentType.DownPayment,
                ReferenceNumber = string.IsNullOrWhiteSpace(referenceNumber) ? null : referenceNumber.Trim(),
                Status = PaymentStatus.Verified,
                VerifiedAt = DateTime.UtcNow,
                VerifiedBy = User.Identity?.Name,
                Remarks = "Recorded by admin",
            });
            if (booking.Status == BookingStatus.Pending) booking.Status = BookingStatus.Confirmed;
            booking.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Toast"] = $"₱{amount:N2} {method} payment recorded.";
            return RedirectToAction(nameof(BookingDetails), new { id = transactionId });
        }

        [HttpGet]
        public async Task<IActionResult> ExportTransactions([FromQuery] PagedFilter filter)
        {
            var pid = User.EffectivePhotographerId(filter.PhotographerId);
            var query = FilterPayments(filter, pid);
            if (!string.IsNullOrEmpty(filter.Status)) query = query.Where(p => p.Status == filter.Status);

            var rows = await query.Include(p => p.Booking).ThenInclude(b => b!.Photographer)
                .OrderByDescending(p => p.CreatedAt).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Receipt No,Date,Transaction ID,Client,Photographer,Type,Method,Reference No,Amount,Status,Verified By,Verified At");
            foreach (var p in rows)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    p.ReceiptNumber, p.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"), p.BookingTransactionId,
                    p.Booking?.ClientName, p.Booking?.Photographer?.Name, p.Type, p.Method, p.ReferenceNumber,
                    p.Amount.ToString("0.00", CultureInfo.InvariantCulture), p.Status, p.VerifiedBy,
                    p.VerifiedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
                }.Select(Csv)));
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"VibeShoot-Transactions-{DateTime.Now:yyyyMMdd-HHmm}.csv");
        }

        // ------------------------------------------------------------------ Receipts

        [HttpGet]
        public async Task<IActionResult> Receipts([FromQuery] PagedFilter filter)
        {
            filter.Status ??= PaymentStatus.Verified;
            var pid = User.EffectivePhotographerId(filter.PhotographerId);
            filter.PhotographerId = pid;
            ViewBag.SelectedPhotographerId = pid;

            var query = FilterPayments(filter, pid).Where(p => p.Status == filter.Status);
            var total = await query.CountAsync();

            var model = new TransactionsViewModel
            {
                Filter = filter,
                TotalItems = total,
                TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize)),
                VerifiedTotal = await query.SumAsync(p => (decimal?)p.Amount) ?? 0,
                Items = await query
                    .Include(p => p.Booking).ThenInclude(b => b!.Photographer)
                    .OrderByDescending(p => p.VerifiedAt ?? p.CreatedAt)
                    .Skip((Math.Max(1, filter.Page) - 1) * PageSize).Take(PageSize)
                    .ToListAsync()
            };
            return View("~/Views/Admin/Receipts.cshtml", model);
        }

        /// <summary>Printable booking statement (all payments) for a booking.</summary>
        [HttpGet]
        public async Task<IActionResult> PrintBooking(string id)
        {
            var booking = await LoadBookingAsync(id);
            if (booking == null) return NotFound();
            return View("~/Views/Admin/Receipt.cshtml", new ReceiptViewModel { Booking = booking });
        }

        /// <summary>Printable official receipt for one payment.</summary>
        [HttpGet]
        public async Task<IActionResult> PrintReceipt(int id)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == id);
            if (payment == null) return NotFound();

            var booking = await LoadBookingAsync(payment.BookingTransactionId);
            if (booking == null) return NotFound();

            return View("~/Views/Admin/Receipt.cshtml", new ReceiptViewModel
            {
                Booking = booking,
                Payment = booking.Payments.First(p => p.Id == id)
            });
        }

        /// <summary>Printable collection report for a date range.</summary>
        [HttpGet]
        public async Task<IActionResult> PrintReport(int? photographerId, DateTime? from, DateTime? to)
        {
            var pid = User.EffectivePhotographerId(photographerId);
            var start = (from ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
            var end = (to ?? DateTime.Today).Date;

            var payments = await FilterPayments(new PagedFilter { From = start, To = end }, pid)
                .Where(p => p.Status == PaymentStatus.Verified)
                .Include(p => p.Booking).ThenInclude(b => b!.Photographer)
                .OrderBy(p => p.VerifiedAt)
                .ToListAsync();

            var model = new ReportViewModel
            {
                From = start,
                To = end,
                Payments = payments,
                PreparedBy = User.Identity?.Name ?? "",
                PhotographerName = pid == null ? "All photographers" : (await _context.Photographers.FindAsync(pid))?.Name ?? ""
            };
            return View("~/Views/Admin/PrintReport.cshtml", model);
        }

        // ------------------------------------------------------------------ Gallery

        [HttpGet]
        public async Task<IActionResult> Gallery(int? photographerId, string? category)
        {
            var pid = User.EffectivePhotographerId(photographerId)
                      ?? await _context.Photographers.OrderBy(p => p.SortOrder).Select(p => (int?)p.Id).FirstOrDefaultAsync();
            ViewBag.SelectedPhotographerId = pid;

            var all = _context.GalleryImages.Where(g => g.PhotographerId == pid);
            var model = new GalleryAdminViewModel
            {
                PhotographerId = pid,
                Category = category,
                CategoryCounts = await all.GroupBy(g => g.Category).ToDictionaryAsync(g => g.Key, g => g.Count()),
                Images = await all
                    .Where(g => string.IsNullOrEmpty(category) || g.Category == category)
                    .OrderBy(g => g.Category).ThenBy(g => g.SortOrder)
                    .ToListAsync()
            };
            return View("~/Views/Admin/Gallery.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(200 * 1024 * 1024)]
        public async Task<IActionResult> UploadGallery(int photographerId, string category, List<Microsoft.AspNetCore.Http.IFormFile> files)
        {
            if (!User.CanAccess(photographerId)) return Forbid();
            var photog = await _context.Photographers.FindAsync(photographerId);
            if (photog == null || !GalleryImage.Categories.Contains(category)) return BadRequest();

            int order = await _context.GalleryImages.Where(g => g.PhotographerId == photographerId && g.Category == category)
                .MaxAsync(g => (int?)g.SortOrder) ?? 0;
            int saved = 0;
            var errors = new List<string>();

            foreach (var file in files ?? new List<Microsoft.AspNetCore.Http.IFormFile>())
            {
                var error = ImageStorage.Validate(file);
                if (error != null) { errors.Add($"{file.FileName}: {error}"); continue; }

                var folder = $"Uploads/Album/{(string.IsNullOrEmpty(photog.MediaFolder) ? photog.Slug : photog.MediaFolder)}/{category}";
                var path = await _storage.SaveAsync(file, folder, System.IO.Path.GetFileNameWithoutExtension(file.FileName));
                _context.GalleryImages.Add(new GalleryImage
                {
                    PhotographerId = photographerId,
                    Category = category,
                    FilePath = path,
                    FileSizeBytes = file.Length,
                    SortOrder = ++order,
                });
                saved++;
            }
            await _context.SaveChangesAsync();

            if (saved > 0) TempData["Toast"] = $"{saved} photo{(saved == 1 ? "" : "s")} added to {category}.";
            if (errors.Count > 0) TempData["ToastError"] = string.Join(" ", errors);
            return RedirectToAction(nameof(Gallery), new { photographerId, category });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGalleryImage(int id)
        {
            var image = await _context.GalleryImages.FindAsync(id);
            if (image == null || !User.CanAccess(image.PhotographerId)) return NotFound();

            _context.GalleryImages.Remove(image);
            await _context.SaveChangesAsync();
            _storage.Delete(image.FilePath);

            TempData["Toast"] = "Photo deleted.";
            return RedirectToAction(nameof(Gallery), new { photographerId = image.PhotographerId, category = Request.Query["category"].ToString() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFeatured(int id)
        {
            var image = await _context.GalleryImages.FindAsync(id);
            if (image == null || !User.CanAccess(image.PhotographerId)) return NotFound();

            image.IsFeatured = !image.IsFeatured;
            await _context.SaveChangesAsync();
            return Ok(new { featured = image.IsFeatured });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RescanGallery([FromServices] GalleryImporter importer)
        {
            if (!User.IsSuperAdmin()) return Forbid();
            var added = await importer.ImportAsync();
            TempData["Toast"] = added == 0 ? "Gallery is already in sync with the Uploads folder." : $"Imported {added} new photo(s) from the Uploads folder.";
            return RedirectToAction(nameof(Gallery));
        }

        // ------------------------------------------------------------------ Studio settings

        [HttpGet]
        public async Task<IActionResult> Studio(int? photographerId)
        {
            var pid = User.EffectivePhotographerId(photographerId)
                      ?? await _context.Photographers.OrderBy(p => p.SortOrder).Select(p => (int?)p.Id).FirstOrDefaultAsync();
            ViewBag.SelectedPhotographerId = pid;

            var photog = await _context.Photographers.FindAsync(pid);
            if (photog == null) return NotFound();

            var model = new StudioViewModel
            {
                Photographer = photog,
                Packages = await _context.Packages.Where(p => p.PhotographerId == pid && p.IsActive)
                    .OrderBy(p => p.Category).ThenBy(p => p.SortOrder).ToListAsync(),
                Accounts = User.IsSuperAdmin()
                    ? await _context.Admins.Include(a => a.Photographer).OrderBy(a => a.Username).ToListAsync()
                    : new List<AdminAccount>()
            };
            return View("~/Views/Admin/Studio.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileForm form)
        {
            if (!User.CanAccess(form.Id)) return Forbid();
            var p = await _context.Photographers.FindAsync(form.Id);
            if (p == null) return NotFound();

            if (string.IsNullOrWhiteSpace(form.Name))
            {
                TempData["ToastError"] = "Studio name is required.";
                return RedirectToAction(nameof(Studio), new { photographerId = form.Id });
            }

            p.Name = form.Name.Trim();
            p.Tagline = form.Tagline?.Trim() ?? "";
            p.Bio = form.Bio?.Trim() ?? "";
            p.GCashAccountName = form.GCashAccountName?.Trim();
            p.GCashNumber = form.GCashNumber?.Trim();
            p.FacebookUrl = form.FacebookUrl?.Trim();
            p.InstagramUrl = form.InstagramUrl?.Trim();
            p.TikTokUrl = form.TikTokUrl?.Trim();
            p.XUrl = form.XUrl?.Trim();
            if (User.IsSuperAdmin()) p.IsActive = form.IsActive;

            var folder = string.IsNullOrEmpty(p.MediaFolder) ? p.Slug : p.MediaFolder;
            if (form.Logo != null)
            {
                var err = ImageStorage.Validate(form.Logo);
                if (err == null) p.LogoPath = await _storage.SaveAsync(form.Logo, $"Uploads/Logos/{folder}", folder);
                else TempData["ToastError"] = "Logo: " + err;
            }
            if (form.GCashQr != null)
            {
                var err = ImageStorage.Validate(form.GCashQr);
                if (err == null) p.GCashQrPath = await _storage.SaveAsync(form.GCashQr, $"Uploads/QRCodes/{folder}", folder + "GCash");
                else TempData["ToastError"] = "GCash QR: " + err;
            }

            await _context.SaveChangesAsync();
            TempData["Toast"] = "Studio profile saved.";
            return RedirectToAction(nameof(Studio), new { photographerId = form.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePackage(PackageForm form)
        {
            if (!User.CanAccess(form.PhotographerId)) return Forbid();
            if (string.IsNullOrWhiteSpace(form.Name) || !ServicePackage.Categories.Contains(form.Category) || form.Price < 0 || form.DurationHours is < 1 or > 12)
            {
                TempData["ToastError"] = "Please complete the package details (name, category, price ≥ 0, 1-12 hours).";
                return RedirectToAction(nameof(Studio), new { photographerId = form.PhotographerId });
            }

            ServicePackage? pkg = form.Id > 0
                ? await _context.Packages.FirstOrDefaultAsync(p => p.Id == form.Id && p.PhotographerId == form.PhotographerId)
                : null;

            if (pkg == null)
            {
                pkg = new ServicePackage
                {
                    PhotographerId = form.PhotographerId,
                    SortOrder = (await _context.Packages.Where(p => p.PhotographerId == form.PhotographerId && p.Category == form.Category).MaxAsync(p => (int?)p.SortOrder) ?? 0) + 1
                };
                _context.Packages.Add(pkg);
            }

            pkg.Category = form.Category;
            pkg.Name = form.Name.Trim();
            pkg.Price = Math.Round(form.Price, 2);
            pkg.DurationHours = form.DurationHours;
            pkg.Inclusions = form.Inclusions?.Trim() ?? "";
            await _context.SaveChangesAsync();

            TempData["Toast"] = $"Package \"{pkg.Name}\" saved.";
            return RedirectToAction(nameof(Studio), new { photographerId = form.PhotographerId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePackage(int id)
        {
            var pkg = await _context.Packages.FindAsync(id);
            if (pkg == null || !User.CanAccess(pkg.PhotographerId)) return NotFound();

            pkg.IsActive = false; // keep it so old bookings still show what was booked
            await _context.SaveChangesAsync();

            TempData["Toast"] = $"Package \"{pkg.Name}\" removed.";
            return RedirectToAction(nameof(Studio), new { photographerId = pkg.PhotographerId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == User.Identity!.Name);
            var hasher = new PasswordHasher<AdminAccount>();

            if (admin == null || hasher.VerifyHashedPassword(admin, admin.PasswordHash, currentPassword ?? "") == PasswordVerificationResult.Failed)
                TempData["ToastError"] = "Current password is incorrect.";
            else if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 8)
                TempData["ToastError"] = "New password must be at least 8 characters.";
            else if (newPassword != confirmPassword)
                TempData["ToastError"] = "New passwords do not match.";
            else
            {
                admin.PasswordHash = hasher.HashPassword(admin, newPassword);
                await _context.SaveChangesAsync();
                TempData["Toast"] = "Password updated.";
            }
            return RedirectToAction(nameof(Studio));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAccount(string username, string password, int? photographerId)
        {
            if (!User.IsSuperAdmin()) return Forbid();

            username = (username ?? "").Trim();
            if (username.Length < 3 || string.IsNullOrEmpty(password) || password.Length < 8)
                TempData["ToastError"] = "Username needs 3+ characters and password 8+ characters.";
            else if (await _context.Admins.AnyAsync(a => a.Username == username))
                TempData["ToastError"] = "That username is already taken.";
            else
            {
                var account = new AdminAccount
                {
                    Username = username,
                    Role = photographerId > 0 ? AdminRoles.Photographer : AdminRoles.SuperAdmin,
                    PhotographerId = photographerId > 0 ? photographerId : null,
                };
                account.PasswordHash = new PasswordHasher<AdminAccount>().HashPassword(account, password);
                _context.Admins.Add(account);
                await _context.SaveChangesAsync();
                TempData["Toast"] = $"Account \"{username}\" created.";
            }
            return RedirectToAction(nameof(Studio));
        }

        // ------------------------------------------------------------------ Helpers

        private IQueryable<Payment> FilterPayments(PagedFilter filter, int? pid)
        {
            var query = _context.Payments.ForPhotographer(pid);
            if (!string.IsNullOrWhiteSpace(filter.Q))
            {
                var q = filter.Q.Trim();
                query = query.Where(p => p.ReceiptNumber.Contains(q) || p.BookingTransactionId.Contains(q)
                                         || (p.ReferenceNumber != null && p.ReferenceNumber.Contains(q))
                                         || p.Booking!.ClientName.Contains(q));
            }
            if (!string.IsNullOrEmpty(filter.Method)) query = query.Where(p => p.Method == filter.Method);
            if (filter.From != null)
            {
                var from = filter.From.Value.Date;
                query = query.Where(p => (p.VerifiedAt ?? p.CreatedAt) >= from);
            }
            if (filter.To != null)
            {
                var to = filter.To.Value.Date.AddDays(1);
                query = query.Where(p => (p.VerifiedAt ?? p.CreatedAt) < to);
            }
            return query;
        }

        private async Task<Booking?> LoadBookingAsync(string id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Photographer)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.TransactionId == id);

            if (booking == null || !User.CanAccess(booking.PhotographerId)) return null;
            booking.Payments = booking.Payments.OrderBy(p => p.CreatedAt).ToList();
            return booking;
        }

        private static string Csv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            // Neutralise spreadsheet formula injection and quote when needed.
            if ("=+-@".Contains(value[0])) value = "'" + value;
            return value.IndexOfAny(new[] { ',', '"', '\n' }) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
        }
    }
}
