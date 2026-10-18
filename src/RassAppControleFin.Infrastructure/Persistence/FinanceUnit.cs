using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RassAppControleFin.Application.Abstractions.Persistence;
using RassAppControleFin.Application.Features.Finance;
using RassAppControleFin.Domain.Entities.Finance;
using RassAppControleFin.MultiTenancy;
using RassAppControleFin.Security;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Infrastructure.Persistence;

public sealed class FinanceUnitFactory(AppDbContext db, ITenantTransactionFactory transactions,
    ITenantContext tenant, ICurrentUser user) : IFinanceUnitFactory
{
    public Task<Result<IFinanceUnit>> BeginAsync(CancellationToken ct) => BeginAsync(ct, false);
    internal async Task<Result<IFinanceUnit>> BeginAsync(CancellationToken ct, bool snapshot)
    {
        var started = await transactions.BeginAsync(ct, snapshot);
        if (!started.IsSuccess) return Result<IFinanceUnit>.Failure(started.Error!);
        try
        {
            var roles = await db.Database.SqlQuery<string>($"""
                SELECT "Role" AS "Value" FROM app."TenantMemberships"
                WHERE "TenantId"={tenant.TenantId!.Value} AND "UserId"={user.Subject!} AND "IsActive"
                """).ToListAsync(ct);
            if (roles.Count == 1 && roles[0] is "User" or "TenantAdmin" or "FinancialAdmin")
                return Result<IFinanceUnit>.Success(new FinanceUnit(db, started.Value, tenant.TenantId.Value, user.Subject!));
            await started.Value.DisposeAsync();
            return Result<IFinanceUnit>.Failure(new("FINANCE_FORBIDDEN", "Vinculo financeiro ativo necessario.", ErrorType.Forbidden));
        }
        catch
        {
            await started.Value.DisposeAsync();
            throw;
        }
    }
}

internal sealed class FinanceUnit(AppDbContext db, ITenantTransaction transaction, Guid tenantId, string userId) : IFinanceUnit
{
    public async Task<Result<ReceiptLookup>> ClaimAsync(Guid operationId, string payloadHash, CancellationToken ct)
    {
        // Transaction-scoped locks serialize duplicates without leaking another user's receipt.
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(tenantId.ToString("N") + operationId.ToString("N")));
        var key = BinaryPrimitives.ReadInt64BigEndian(bytes);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})", ct);
        var receipt = await db.Set<SyncOperationReceipt>().SingleOrDefaultAsync(x => x.OperationId == operationId, ct);
        if (receipt is null) return Result<ReceiptLookup>.Success(new(false, null));
        return receipt.PayloadHash == payloadHash
            ? Result<ReceiptLookup>.Success(new(true, receipt.ResponseJson))
            : Result<ReceiptLookup>.Failure(new("OPERATION_REUSED", "OperationId ja utilizado com outros dados.", ErrorType.Conflict));
    }
    public Task<Account?> AccountAsync(Guid id, CancellationToken ct)
        => db.Accounts.AsTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Category?> CategoryAsync(Guid id, CancellationToken ct)
        => db.Categories.AsTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<CreditCard?> CardAsync(Guid id, CancellationToken ct)
        => db.CreditCards.AsTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<FinancialTransaction?> TransactionAsync(Guid id, CancellationToken ct)
        => db.Transactions.AsTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Budget?> BudgetAsync(Guid id, CancellationToken ct)
        => db.Budgets.AsTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Goal?> GoalAsync(Guid id, CancellationToken ct)
        => db.Goals.AsTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<ExpenseReductionGoal?> ExpenseReductionGoalAsync(Guid id, CancellationToken ct)
        => db.ExpenseReductionGoals.AsTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<FinancialNotification?> NotificationAsync(Guid id, CancellationToken ct)
        => db.FinancialNotifications.AsTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyList<BudgetAlert>> BudgetAlertsAsync(Guid categoryId, string currency,
        DateTimeOffset occurredAt, decimal pendingExpense, CancellationToken ct)
    {
        var budgets = await db.Budgets.AsNoTracking().Where(x => !x.IsArchived && x.Currency == currency
            && x.PeriodStart <= occurredAt && occurredAt < x.PeriodEnd
            && (!x.CategoryId.HasValue || x.CategoryId == categoryId)).ToListAsync(ct);
        var alerts = new List<BudgetAlert>();
        foreach (var budget in budgets)
        {
            var spent = await db.Transactions.Where(x => x.Status == TransactionStatus.Posted
                && x.Flow == FinancialFlow.Expense && x.Currency == currency
                && x.OccurredAt >= budget.PeriodStart && x.OccurredAt < budget.PeriodEnd
                && (!budget.CategoryId.HasValue || x.CategoryId == budget.CategoryId))
                .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
            var projected = spent + pendingExpense;
            var threshold = projected >= budget.Amount ? 100 : projected >= budget.Amount * 0.8m ? 80 : 0;
            if (threshold > 0) alerts.Add(new BudgetAlert(budget.Id, budget.Amount, projected, threshold));
        }
        return alerts;
    }
    public Task<bool> NotificationExistsAsync(string type, CancellationToken ct)
        => db.FinancialNotifications.AnyAsync(x => x.Type == type, ct);
    public void Add(FinancialAggregate resource) => db.Add(resource);
    public void Ledger(Guid accountId, decimal delta, string kind, Guid resourceId, Guid? transferId = null)
        => db.Add(new LedgerEntry { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, AccountId = accountId,
            Delta = delta, Kind = kind, ResourceId = resourceId, TransferId = transferId, CreatedAt = DateTimeOffset.UtcNow });
    public void CardMovement(Guid cardId, Guid? accountId, Guid? categoryId, decimal amount, string currency,
        string kind, DateTimeOffset occurredAt, string description, Guid id)
        => db.Add(new CardMovementRow { Id = id, TenantId = tenantId, UserId = userId, CardId = cardId,
            AccountId = accountId, CategoryId = categoryId, Amount = amount, Currency = currency, Kind = kind,
            OccurredAt = occurredAt, Description = description, CreatedAt = DateTimeOffset.UtcNow });
    public async Task<Result> CommitAsync(Guid operationId, string payloadHash, MutationReceipt receipt, string action, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        db.Add(new SyncOperationReceipt { TenantId = tenantId, UserId = userId, OperationId = operationId,
            PayloadHash = payloadHash, ResponseJson = JsonSerializer.Serialize(receipt), CreatedAt = now });
        db.Add(new FinancialOutboxRow { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId,
            OperationId = operationId, ResourceId = receipt.ResourceId, EventType = action, CreatedAt = now });
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO app."AuditEntries" ("Id","TenantId","UserId","Action","ResourceId","Outcome","CorrelationId","CreatedAt")
            VALUES ({Guid.NewGuid()},{tenantId},{userId},{action},{receipt.ResourceId},'Success',
                {Activity.Current?.TraceId.ToString() ?? operationId.ToString()},{now})
            """, ct);
        return await transaction.CommitAsync(ct);
    }
    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
