using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Clinicrest.Api.BookingStress;

/// <summary>
/// Stress: 100 concurrent POST /api/bookings for the same slot (capacity 10), each with a distinct user_id.
/// Idempotency is per (user_id, slot_id), so one user cannot fill multiple seats; distinct users are required.
/// Requires Docker (Testcontainers).
/// </summary>
[CollectionDefinition(nameof(BookingStressCollection))]
public class BookingStressCollection : ICollectionFixture<BookingStressFixture>
{
}

[Collection(nameof(BookingStressCollection))]
public sealed class BookingConcurrencyStressTests(BookingStressFixture fixture)
{
    [Fact]
    public async Task Hundred_concurrent_bookings_on_capacity_10_slot_only_ten_succeed_and_booked_count_stays_at_capacity()
    {
        var client = fixture.Client;
        var slotId = fixture.SlotId;

        var userIds = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToArray();

        var tasks = userIds.Select(userId =>
            client.PostAsJsonAsync("/api/bookings", new { user_id = userId, slot_id = slotId }));

        var responses = await Task.WhenAll(tasks);

        var created = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var okReplay = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var conflict = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);
        var badRequest = responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest);

        foreach (var r in responses.Where(r =>
                     r.StatusCode is not HttpStatusCode.Created
                     and not HttpStatusCode.Conflict
                     and not HttpStatusCode.OK))
        {
            var body = await r.Content.ReadAsStringAsync();
            Assert.Fail($"Unexpected status {r.StatusCode}: {body}");
        }

        Assert.Equal(0, okReplay);
        Assert.Equal(0, badRequest);
        Assert.Equal(10, created);
        Assert.Equal(90, conflict);

        await using var conn = new NpgsqlConnection(fixture.ConnectionString);
        await conn.OpenAsync();

        int bookedCount;
        int capacity;
        await using (var cmd = new NpgsqlCommand(
                           """
                           SELECT booked_count, capacity
                           FROM time_slots
                           WHERE id = @id
                           """,
                           conn))
        {
            cmd.Parameters.AddWithValue("id", slotId);
            await using var reader = await cmd.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            bookedCount = reader.GetInt32(0);
            capacity = reader.GetInt32(1);
        }

        Assert.Equal(10, capacity);
        Assert.Equal(10, bookedCount);
        Assert.True(bookedCount <= capacity);

        await using (var countCmd = new NpgsqlCommand(
                          """
                          SELECT COUNT(*)::int
                          FROM bookings
                          WHERE slot_id = @id AND status = 'active'
                          """,
                          conn))
        {
            countCmd.Parameters.AddWithValue("id", slotId);
            var activeBookings = (int)(await countCmd.ExecuteScalarAsync() ?? 0);
            Assert.Equal(10, activeBookings);
        }
    }
}

public sealed class BookingStressFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private WebApplicationFactory<global::Program>? _factory;

    public HttpClient Client { get; private set; } = null!;
    public Guid SlotId { get; private set; }
    public string ConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();
        await _postgres.StartAsync();
        ConnectionString = _postgres.GetConnectionString();

        const string jwtSecret = "stress-test-jwt-secret-must-be-at-least-32-characters-long!!";

        _factory = new WebApplicationFactory<global::Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
            builder.UseSetting("Jwt:Secret", jwtSecret);
            builder.UseSetting("Jwt:Issuer", "clinicrest-api");
            builder.UseSetting("Jwt:Audience", "clinicrest-web");
            builder.UseSetting("Seed:RootAdminUsername", "rootadmin");
            builder.UseSetting("Seed:RootAdminPassword", "guardianOP");
            builder.UseSetting("Seed:SyncRootAdminPasswordOnStartup", "true");
        });

        Client = _factory.CreateClient();
        Client.DefaultRequestHeaders.Accept.Clear();
        Client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

        await ApplyBookingSchemaAsync(ConnectionString);

        var token = await LoginAsync(Client);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        SlotId = Guid.NewGuid();
        await InsertStressSlotAsync(ConnectionString, SlotId);
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }
    }

    private static async Task<string> LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "rootadmin", password = "guardianOP" });

        response.EnsureSuccessStatusCode();
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        return doc.GetProperty("token").GetString()
               ?? throw new InvalidOperationException("Login response missing token.");
    }

    private static async Task ApplyBookingSchemaAsync(string connectionString)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        foreach (var batch in BookingSchemaBatches)
        {
            await using var cmd = new NpgsqlCommand(batch, conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task InsertStressSlotAsync(string connectionString, Guid slotId)
    {
        var start = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(1).Date.AddHours(12), DateTimeKind.Utc);
        var end = start.AddHours(1);
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO time_slots (id, start_time, end_time, capacity, booked_count, created_at)
            VALUES (@id, @start, @end, 10, 0, NOW())
            """,
            conn);
        cmd.Parameters.AddWithValue("id", slotId);
        cmd.Parameters.AddWithValue("start", start);
        cmd.Parameters.AddWithValue("end", end);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>EF migrations do not include booking tables; mirror SQL migration in batches.</summary>
    private static readonly string[] BookingSchemaBatches =
    [
        """
        CREATE TABLE IF NOT EXISTS time_slots (
            id uuid PRIMARY KEY,
            start_time timestamp with time zone NOT NULL,
            end_time timestamp with time zone NOT NULL,
            capacity integer NOT NULL,
            booked_count integer NOT NULL DEFAULT 0,
            created_at timestamp with time zone NOT NULL DEFAULT NOW(),
            CONSTRAINT chk_time_slots_time_range CHECK (end_time > start_time),
            CONSTRAINT chk_time_slots_capacity_positive CHECK (capacity > 0),
            CONSTRAINT chk_time_slots_booked_count_range CHECK (booked_count >= 0 AND booked_count <= capacity)
        );
        """,
        """
        CREATE TABLE IF NOT EXISTS bookings (
            id uuid PRIMARY KEY,
            user_id uuid NOT NULL,
            slot_id uuid NOT NULL REFERENCES time_slots(id) ON DELETE RESTRICT,
            status text NOT NULL,
            created_at timestamp with time zone NOT NULL DEFAULT NOW(),
            CONSTRAINT chk_bookings_status CHECK (status IN ('active', 'cancelled'))
        );
        """,
        """
        CREATE INDEX IF NOT EXISTS ix_bookings_slot_id ON bookings(slot_id);
        """,
        """
        CREATE UNIQUE INDEX IF NOT EXISTS ux_bookings_user_slot_active
            ON bookings(user_id, slot_id)
            WHERE status = 'active';
        """
    ];
}
