using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Infrastructure;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.ViewModels;
using Portfolio.Web.Services;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class MessagesController : AdminControllerBase
{
    public MessagesController(Data.AppDbContext db, IMediaService media) : base(db, media) { }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? filter, int page = 1, CancellationToken ct = default)
    {
        var query = Db.ContactMessages.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(m => m.Name.Contains(term) || m.Email.Contains(term)
                || (m.Subject != null && m.Subject.Contains(term)) || m.Message.Contains(term));
        }

        query = filter switch
        {
            "unread" => query.Where(m => !m.IsRead && !m.IsArchived),
            "archived" => query.Where(m => m.IsArchived),
            "read" => query.Where(m => m.IsRead && !m.IsArchived),
            _ => query.Where(m => !m.IsArchived)
        };

        var items = await PagedList<ContactMessage>.CreateAsync(
            query.OrderByDescending(m => m.CreatedAt), page, 15, ct);

        ViewData["Filter"] = filter ?? "all";
        ViewData["Q"] = q;
        ViewData["UnreadCount"] = await Db.ContactMessages.CountAsync(m => !m.IsRead && !m.IsArchived, ct);
        ViewData["ArchivedCount"] = await Db.ContactMessages.CountAsync(m => m.IsArchived, ct);
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var message = await Db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (message is null) return NotFound();
        if (!message.IsRead)
        {
            message.IsRead = true;
            await Db.SaveChangesAsync(ct);
        }
        return View(message);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRead(int id, CancellationToken ct)
    {
        var message = await Db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (message is null) return NotFound();
        message.IsRead = !message.IsRead;
        await Db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleArchive(int id, CancellationToken ct)
    {
        var message = await Db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (message is null) return NotFound();
        message.IsArchived = !message.IsArchived;
        message.IsRead = true;
        await Db.SaveChangesAsync(ct);
        NotifySuccess(message.IsArchived ? "Message archived." : "Message restored to the inbox.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var message = await Db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (message is null) return NotFound();
        Db.ContactMessages.Remove(message);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Message permanently deleted.");
        return RedirectToAction(nameof(Index));
    }
}
