using EcommerceAPI.Data;
using EcommerceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Controllers
{
    /// <summary>Consultation du journal d'audit (qui a modifié quoi) — réservée à l'administrateur.</summary>
    [ApiController]
    [Route("api/audit-logs")]
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : ControllerBase
    {
        private readonly EcommerceContext _db;
        public AuditLogsController(EcommerceContext db) => _db = db;

        public class AuditLogDto
        {
            public long Id { get; set; }
            public DateTime Timestamp { get; set; }
            public int? UserId { get; set; }
            public string UserEmail { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
            public string EntityName { get; set; } = string.Empty;
            public string EntityId { get; set; } = string.Empty;
            /// <summary>JSON : { "Champ": { "old": ..., "new": ... } }</summary>
            public string Changes { get; set; } = "{}";
            public string IpAddress { get; set; } = string.Empty;
        }

        public class AuditLogPageDto
        {
            public IEnumerable<AuditLogDto> Items { get; set; } = Enumerable.Empty<AuditLogDto>();
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalCount { get; set; }
            public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
            /// <summary>Types d'entités présents dans le journal, pour alimenter le filtre.</summary>
            public IEnumerable<string> Entities { get; set; } = Enumerable.Empty<string>();
        }

        [HttpGet]
        public async Task<ActionResult<AuditLogPageDto>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? entity = null,
            [FromQuery] string? action = null,
            [FromQuery] string? user = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            if (page < 1 || pageSize is < 1 or > 100)
                return BadRequest(new { message = "Pagination invalide." });

            IQueryable<AuditLog> query = _db.AuditLogs.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(entity)) query = query.Where(a => a.EntityName == entity);
            if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);
            if (!string.IsNullOrWhiteSpace(user))
            {
                var pattern = $"%{user.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
                query = query.Where(a => EF.Functions.ILike(a.UserEmail, pattern, "\\"));
            }
            if (from.HasValue) query = query.Where(a => a.Timestamp >= from.Value.ToUniversalTime());
            if (to.HasValue) query = query.Where(a => a.Timestamp <= to.Value.ToUniversalTime());

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.Timestamp).ThenByDescending(a => a.Id)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id, Timestamp = a.Timestamp, UserId = a.UserId, UserEmail = a.UserEmail,
                    Action = a.Action, EntityName = a.EntityName, EntityId = a.EntityId,
                    Changes = a.Changes, IpAddress = a.IpAddress
                })
                .ToListAsync();
            var entities = await _db.AuditLogs.AsNoTracking()
                .Select(a => a.EntityName).Distinct().OrderBy(n => n).ToListAsync();

            return Ok(new AuditLogPageDto
            {
                Items = items, Page = page, PageSize = pageSize, TotalCount = total, Entities = entities
            });
        }
    }
}
