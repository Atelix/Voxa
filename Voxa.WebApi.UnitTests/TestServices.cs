using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Voxa.Abstractions.Services;
using Voxa.Logic;
using Voxa.Services;

namespace Voxa.WebApi.UnitTests;

/// <summary>A fixed client for the rate limiter.</summary>
internal sealed class TestClient : IClientIdentity
{
    public string Address { get; set; } = "192.0.2.1";

    public string Device { get; set; } = "device:test";
}

/// <summary>Every logic class on one in-memory database, with mail in the test mailbox and a fake clock.</summary>
internal sealed class TestServices : IDisposable
{
    public TestServices(DateTimeOffset now)
    {
        Time = new FakeTimeProvider(now);
        Context = TestDatabase.Create();
        Club = new ClubSettingsStore(Context);
        Codes = new CodeService(Options.Create(new CodeOptions { Pepper = "test-pepper" }));
        Outbox = new OutboxMailSender(Time);
        Limiter = new AttemptLimiter(Cache, Time);
        Meetings = new MeetingLogic(Context, Club, Time);
        Agenda = new AgendaLogic(Context);
        Results = new ResultLogic(Context, Time);
        Voters = new VoterLogic(Context, Club, Codes, Outbox, [], Options.Create(new AppOptions { PublicUrl = "https://stem.example.org" }));
        Ballot = new BallotLogic(Context, Codes, Limiter, Client, Time);
    }

    public FakeTimeProvider Time { get; }

    public VoxaContext Context { get; }

    public MemoryCache Cache { get; } = new(new MemoryCacheOptions());

    public ClubSettingsStore Club { get; }

    public CodeService Codes { get; }

    public OutboxMailSender Outbox { get; }

    public AttemptLimiter Limiter { get; }

    public TestClient Client { get; } = new();

    public MeetingLogic Meetings { get; }

    public AgendaLogic Agenda { get; }

    public ResultLogic Results { get; }

    public VoterLogic Voters { get; }

    public BallotLogic Ballot { get; }

    /// <summary>The codes in the test mailbox, read from the mails as a member would.</summary>
    public IReadOnlyList<string> MailedCodes() =>
        [.. Outbox.Messages().Select(static m => System.Text.RegularExpressions.Regex.Match(m.Text, @"Jouw stemcode: (\S+)").Groups[1].Value)];

    public void Dispose()
    {
        Context.Dispose();
        Cache.Dispose();
    }
}
