using System.Net;
using Klarfakt.Validation;

namespace Klarfakt.Tests;

/// <summary>
/// Restoring is the only network call Klarfakt makes, and the first thing a caller runs. A single
/// 504 from a GitHub release download used to end it. These pin the retry: a transient answer is
/// tried again, a 404 is not, and a cancelled token stops immediately rather than backing off.
/// </summary>
public class RestoreRetryTests
{
    [Fact]
    public async Task Retries_a_transient_answer_and_uses_the_one_that_arrives()
    {
        var handler = new ScriptedHandler(
            Respond(HttpStatusCode.GatewayTimeout),
            Respond(HttpStatusCode.OK, EmptyZip()));

        var exception = await Assert.ThrowsAsync<RulePackException>(() => Restore(handler));

        Assert.Equal(2, handler.Calls);
        Assert.Contains("is not in", exception.Message);
    }

    [Fact]
    public async Task Gives_up_after_four_attempts()
    {
        var handler = new ScriptedHandler(Respond(HttpStatusCode.GatewayTimeout));

        var exception = await Assert.ThrowsAsync<RulePackException>(() => Restore(handler));

        Assert.Equal(4, handler.Calls);
        Assert.Contains("504", exception.Message);
    }

    [Fact]
    public async Task Does_not_retry_a_tag_that_is_not_there()
    {
        var handler = new ScriptedHandler(Respond(HttpStatusCode.NotFound));

        var exception = await Assert.ThrowsAsync<RulePackException>(() => Restore(handler));

        Assert.Equal(1, handler.Calls);
        Assert.Contains("404", exception.Message);
    }

    [Fact]
    public async Task Does_not_retry_a_network_failure_forever()
    {
        var handler = new ScriptedHandler(() => throw new HttpRequestException("connection reset"));

        var exception = await Assert.ThrowsAsync<RulePackException>(() => Restore(handler));

        Assert.Equal(4, handler.Calls);
        Assert.Contains("connection reset", exception.Message);
    }

    [Fact]
    public async Task Stops_when_the_caller_cancels()
    {
        var handler = new ScriptedHandler(Respond(HttpStatusCode.GatewayTimeout));
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Restore(handler, source.Token));

        Assert.True(handler.Calls <= 1, $"a cancelled restore should not retry, but it made {handler.Calls} requests");
    }

    private static async Task Restore(ScriptedHandler handler, CancellationToken cancellationToken = default)
    {
        var root = Path.Combine(Path.GetTempPath(), "klarfakt-restore-" + Guid.NewGuid().ToString("n"));

        try
        {
            using var client = new HttpClient(handler);
            await RulePackCatalog.Load(root).RestoreAsync(client: client, cancellationToken: cancellationToken);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static Func<HttpResponseMessage> Respond(HttpStatusCode status, byte[]? content = null) =>
        () => new HttpResponseMessage(status) { Content = new ByteArrayContent(content ?? []) };

    private static byte[] EmptyZip()
    {
        using var buffer = new MemoryStream();
        using (new System.IO.Compression.ZipArchive(buffer, System.IO.Compression.ZipArchiveMode.Create, true))
        {
        }

        return buffer.ToArray();
    }

    private sealed class ScriptedHandler(params Func<HttpResponseMessage>[] responses) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = responses[Math.Min(Calls, responses.Length - 1)];
            Calls++;

            return Task.FromResult(response());
        }
    }
}
