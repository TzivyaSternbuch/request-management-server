using Requests.Domain.Entities;

namespace Requests.Infrastructure.Persistence;

public static class DbSeeder
{
    private const int SeedCount = 100_000;
    private const int BatchSize = 10_000;
    private const int UserCount = 50;
    private const int CustomerCount = 1_000;
    private const int HistoryDays = 3 * 365;
    private const int UnassignedEvery = 7;
    private const int MinutesPerDay = 24 * 60;
    private const int RandomSeed = 42;

    private static readonly RequestStatus[] Statuses = Enum.GetValues<RequestStatus>();
    private static readonly RequestType[] Types = Enum.GetValues<RequestType>();

    public static void Seed(RequestsDbContext db)
    {
        if (db.Requests.Any())
            return;

        var random = new Random(RandomSeed);
        var now = DateTime.UtcNow;

    
        using var transaction = db.Database.BeginTransaction();

        // Saving in batches and clearing the change tracker keeps memory low:
        // the 100k entities are never tracked at the same time.
        for (var batchStart = 1; batchStart <= SeedCount; batchStart += BatchSize)
        {
            var batchCount = Math.Min(BatchSize, SeedCount - batchStart + 1);
            var requests = Enumerable.Range(batchStart, batchCount)
                .Select(number => CreateRequest(number, random, now));

            db.Requests.AddRange(requests);
            db.SaveChanges();
            db.ChangeTracker.Clear();
        }

        transaction.Commit();
    }

    private static Request CreateRequest(int number, Random random, DateTime now)
    {
        var createdAt = now.AddMinutes(-random.Next(HistoryDays * MinutesPerDay));
        var minutesSinceCreated = (int)(now - createdAt).TotalMinutes;

        return new Request
        {
            RequestNumber = $"REQ-{number:000000}",
            CustomerId = random.Next(1, CustomerCount + 1),
            OwnerId = random.Next(1, UserCount + 1),
            AssignedToUserId = number % UnassignedEvery == 0 ? null : random.Next(1, UserCount + 1),
            Status = Statuses[random.Next(Statuses.Length)],
            RequestType = Types[random.Next(Types.Length)],
            CreatedAt = createdAt,
            UpdatedAt = createdAt.AddMinutes(random.Next(minutesSinceCreated + 1))
        };
    }
}
