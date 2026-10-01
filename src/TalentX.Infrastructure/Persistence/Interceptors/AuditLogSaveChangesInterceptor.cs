using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Interceptors;

public sealed class AuditLogSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;

    public AuditLogSaveChangesInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CreateAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CreateAuditLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CreateAuditLogs(DbContext? dbContext)
    {
        if (dbContext is not ApplicationDbContext context)
        {
            return;
        }

        var entries = context.ChangeTracker.Entries().ToList();
        var auditLogs = new List<AuditLog>();

        foreach (var entry in entries)
        {
            if (entry.Entity is AuditLog auditLog)
            {
                if (entry.State == EntityState.Added && string.IsNullOrWhiteSpace(auditLog.IpAddress) && !string.IsNullOrWhiteSpace(_currentUserService.IpAddress))
                {
                    auditLog.SetIpAddress(_currentUserService.IpAddress);
                }
                continue;
            }

            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var clrType = entry.Metadata.ClrType;
            if (clrType.Namespace != null && clrType.Namespace.StartsWith("Microsoft.AspNetCore.Identity", StringComparison.Ordinal))
            {
                continue;
            }

            var entityName = clrType.Name;
            var genericMarkerIndex = entityName.IndexOf('`');
            if (genericMarkerIndex >= 0)
            {
                entityName = entityName[..genericMarkerIndex];
            }

            var actionPrefix = entry.State switch
            {
                EntityState.Added => "Insert",
                EntityState.Modified => "Update",
                EntityState.Deleted => "Delete",
                _ => null
            };

            if (actionPrefix == null)
            {
                continue;
            }

            var action = $"{actionPrefix} · {entityName}";
            var currentUserEmail = _currentUserService.Email;
            var actor = string.IsNullOrWhiteSpace(currentUserEmail) ? "System" : currentUserEmail;
            var isSystem = string.IsNullOrWhiteSpace(currentUserEmail) || currentUserEmail == "System";

            auditLogs.Add(new AuditLog(actor, action, isSystem, _currentUserService.IpAddress));
        }

        if (auditLogs.Count > 0)
        {
            context.AuditLogs.AddRange(auditLogs);
        }
    }
}
