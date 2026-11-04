using Shekar.Application.Abstractions;
using Shekar.Domain.Entities;
using Shekar.Domain.Enums;

namespace Shekar.Mock.Repositories;

public sealed class MockPaymentRepository : IPaymentRepository
{
    private readonly MockDataStore _store;
    public MockPaymentRepository(MockDataStore store) => _store = store;

    public Task<Payment?> GetAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(_store.Payments.FirstOrDefault(p => p.Id == id));

    public Task<Payment?> GetPendingForUserAsync(int userId, CancellationToken ct = default) =>
        Task.FromResult(_store.Payments
            .FirstOrDefault(p => p.UserId == userId && p.Status == PaymentReviewStatus.Pending));

    public Task<IReadOnlyList<Payment>> ListPendingAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Payment>>(_store.Payments
            .Where(p => p.Status == PaymentReviewStatus.Pending)
            .OrderBy(p => p.SubmittedAt)
            .ToList());

    public Task<Payment> AddAsync(Payment payment, CancellationToken ct = default)
    {
        lock (_store.Gate)
        {
            payment.Id = _store.NextId(_store.Payments, p => p.Id);
            _store.Payments.Add(payment);
        }
        return Task.FromResult(payment);
    }

    // پرداخت‌ها روی اشیای درون‌حافظه نگه داشته می‌شوند؛ تغییر همان شیء کافی است.
    public Task UpdateAsync(Payment payment, CancellationToken ct = default) => Task.CompletedTask;
}
