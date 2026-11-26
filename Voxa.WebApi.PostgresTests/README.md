# PostgreSQL tests

Tests that need the real database provider: migrations, unique indexes, case rules and time zones. They are not part
of `Voxa.sln`, so `dotnet test Voxa.sln` needs no Docker. CI runs them in their own job.

```bash
dotnet test Voxa.WebApi.PostgresTests
```

Without Docker, point them at an existing server; each run creates and drops its own database:

```bash
VOXA_TEST_POSTGRES="Host=localhost;Port=5432;Username=root;Password=root" dotnet test Voxa.WebApi.PostgresTests
```
