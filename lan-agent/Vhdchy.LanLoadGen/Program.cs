using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;

var baseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://127.0.0.1:17891";
var clients = args.Length > 1 && int.TryParse(args[1], out var c) ? Math.Clamp(c, 1, 500) : 10;
var requestsPerClient = args.Length > 2 && int.TryParse(args[2], out var r) ? Math.Clamp(r, 1, 10000) : 100;

Console.WriteLine("VHDCHY LAN LoadGen BETA");
Console.WriteLine($"Target: {baseUrl}");
Console.WriteLine($"Logical clients: {clients}; requests/client: {requestsPerClient}; total: {clients * requestsPerClient}");
Console.WriteLine("Synthetic load measures Agent capacity only; it is NOT evidence for equivalent physical PDA/Wi-Fi capacity.\n");

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
var latencies = new ConcurrentBag<double>();
long ok = 0, failed = 0;
var all = Stopwatch.StartNew();

await Parallel.ForEachAsync(Enumerable.Range(1, clients), new ParallelOptions { MaxDegreeOfParallelism = clients }, async (client, ct) =>
{
    var deviceId = $"synthetic-{client:D3}";
    for (var i = 1; i <= requestsPerClient; i++)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var response = await http.PostAsJsonAsync(baseUrl + "/api/pilot/echo", new { deviceId, payload = "load-test" }, ct);
            if (response.IsSuccessStatusCode) Interlocked.Increment(ref ok); else Interlocked.Increment(ref failed);
        }
        catch
        {
            Interlocked.Increment(ref failed);
        }
        finally
        {
            sw.Stop();
            latencies.Add(sw.Elapsed.TotalMilliseconds);
        }
    }
});

all.Stop();
var ordered = latencies.Order().ToArray();
double Percentile(double p) => ordered.Length == 0 ? 0 : ordered[Math.Min(ordered.Length - 1, (int)Math.Ceiling((ordered.Length - 1) * p))];
var total = ok + failed;
var rate = all.Elapsed.TotalSeconds <= 0 ? 0 : total / all.Elapsed.TotalSeconds;

Console.WriteLine($"Elapsed: {all.Elapsed}");
Console.WriteLine($"Success: {ok}/{total} ({(total == 0 ? 0 : ok * 100.0 / total):F3}%)");
Console.WriteLine($"Failed: {failed}");
Console.WriteLine($"Throughput: {rate:F1} req/s");
Console.WriteLine($"Latency p50/p95/p99: {Percentile(.50):F1} / {Percentile(.95):F1} / {Percentile(.99):F1} ms");

Environment.ExitCode = failed == 0 ? 0 : 2;
