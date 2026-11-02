using System.Security.Claims;
using System.Text.Json;
using EcommerceAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EcommerceAPI.Data
{
    /// <summary>
    /// Enregistre dans AuditLogs chaque création, modification et suppression des entités suivies
    /// (produits, catégories, commandes, coupons, utilisateurs, avis), avec l'utilisateur connecté,
    /// son adresse IP et, pour une modification, l'ancienne et la nouvelle valeur de chaque champ.
    /// </summary>
    public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private static readonly HashSet<Type> AuditedTypes = new()
        {
            typeof(Product), typeof(Category), typeof(Order), typeof(Coupon), typeof(User), typeof(Review)
        };

        /// <summary>Champs jamais écrits dans le journal.</summary>
        private static readonly HashSet<string> SensitiveProperties = new() { "Password" };

        private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

        private readonly IHttpContextAccessor _http;
        private readonly List<PendingAudit> _pending = new();
        private bool _flushing;

        public AuditSaveChangesInterceptor(IHttpContextAccessor http) => _http = http;

        private sealed record PendingAudit(EntityEntry Entry, string Action, string? KnownId, Dictionary<string, object?> Changes);

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Capture(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Capture(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            Flush(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
            return base.SavedChanges(eventData, result);
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            await Flush(eventData.Context, cancellationToken);
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData) => _pending.Clear();

        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            _pending.Clear();
            return Task.CompletedTask;
        }

        private void Capture(DbContext? context)
        {
            if (context == null || _flushing) return;
            _pending.Clear();

            foreach (var entry in context.ChangeTracker.Entries().ToList())
            {
                if (!AuditedTypes.Contains(entry.Entity.GetType())) continue;

                switch (entry.State)
                {
                    case EntityState.Added:
                        _pending.Add(new PendingAudit(entry, "Create", null, Snapshot(entry, useOriginal: false)));
                        break;

                    case EntityState.Modified:
                        var changes = Diff(entry);
                        if (changes.Count > 0)
                            _pending.Add(new PendingAudit(entry, "Update", KeyOf(entry), changes));
                        break;

                    case EntityState.Deleted:
                        _pending.Add(new PendingAudit(entry, "Delete", KeyOf(entry), Snapshot(entry, useOriginal: true)));
                        break;
                }
            }
        }

        private async Task Flush(DbContext? context, CancellationToken cancellationToken)
        {
            if (context == null || _flushing || _pending.Count == 0) return;

            var (userId, email, ip) = CurrentActor();
            var logs = _pending.Select(p => new AuditLog
            {
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                UserEmail = email,
                Action = p.Action,
                EntityName = p.Entry.Entity.GetType().Name,
                // Pour une création, l'Id n'existe qu'après l'enregistrement
                EntityId = p.KnownId ?? KeyOf(p.Entry),
                Changes = JsonSerializer.Serialize(p.Changes, Json),
                IpAddress = ip
            }).ToList();
            _pending.Clear();

            _flushing = true;
            try
            {
                context.Set<AuditLog>().AddRange(logs);
                await context.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                _flushing = false;
            }
        }

        private static string KeyOf(EntityEntry entry)
            => string.Join("-", entry.Properties
                .Where(p => p.Metadata.IsPrimaryKey())
                .Select(p => p.CurrentValue?.ToString() ?? string.Empty));

        private static Dictionary<string, object?> Snapshot(EntityEntry entry, bool useOriginal)
        {
            var values = new Dictionary<string, object?>();
            foreach (var property in entry.Properties)
            {
                var name = property.Metadata.Name;
                if (SensitiveProperties.Contains(name) || property.Metadata.IsPrimaryKey()) continue;
                values[name] = Format(useOriginal ? property.OriginalValue : property.CurrentValue);
            }
            return values;
        }

        private static Dictionary<string, object?> Diff(EntityEntry entry)
        {
            var changes = new Dictionary<string, object?>();
            foreach (var property in entry.Properties.Where(p => p.IsModified))
            {
                var name = property.Metadata.Name;
                if (SensitiveProperties.Contains(name))
                {
                    // On note qu'un mot de passe a changé sans jamais enregistrer sa valeur
                    if (!Equals(property.OriginalValue, property.CurrentValue)) changes[name] = "modifié";
                    continue;
                }
                if (Equals(property.OriginalValue, property.CurrentValue)) continue;
                changes[name] = new { old = Format(property.OriginalValue), @new = Format(property.CurrentValue) };
            }
            return changes;
        }

        private static object? Format(object? value) => value switch
        {
            null => null,
            Enum e => e.ToString(),
            DateTime d => d.ToString("o"),
            _ => value
        };

        private (int? UserId, string Email, string Ip) CurrentActor()
        {
            var http = _http.HttpContext;
            var user = http?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return (null, "anonyme", http?.Connection.RemoteIpAddress?.ToString() ?? string.Empty);

            int? id = int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : null;
            return (id, user.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
                http!.Connection.RemoteIpAddress?.ToString() ?? string.Empty);
        }
    }
}
