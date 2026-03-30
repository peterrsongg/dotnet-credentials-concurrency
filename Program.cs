using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using AWS.Logger;
using AWS.Logger.SeriLog;
using Serilog;
using var s3Client = new AmazonS3Client(RegionEndpoint.USEast1);
var logName = "dotnet-8554";
var bucketName = "dotnet-8554--use1-az4--x-s3";
Log.Logger = new LoggerConfiguration()
    .WriteTo.AWSSeriLog(new AWSLoggerConfig(logName))
    .CreateLogger();
var concurrency = args.Length > 0 ? int.Parse(args[0]) : 64;
var semaphore = new SemaphoreSlim(concurrency);
var tasks = new Task[concurrency];
var errors = new List<Exception>();
var errorLock = new object();


Console.WriteLine("=== EC2 IMDS Concurrent Credential Resolution Bug Reproduction ===");
Console.WriteLine($"  Bucket:      {bucketName}");
Console.WriteLine($"  Concurrency: {concurrency}");
Console.WriteLine($"  Region:      us-east-1");
Console.WriteLine();

var config = new AmazonS3Config { RegionEndpoint = RegionEndpoint.USEast1 };
var client = new AmazonS3Client(config);

// DO NOT make any SDK calls before this point — that's the bug trigger.
// The first concurrent batch will all try to resolve IMDS credentials simultaneously.

Console.WriteLine($"Launching {concurrency} concurrent PutObject operations...");
Console.WriteLine("(No prior SDK calls — credentials not yet cached)");
Console.WriteLine();
for (int i = 0; i < concurrency; i++)
{
    await semaphore.WaitAsync();
    var index = i;
    tasks[i] = Task.Run(async () =>
    {
        try
        {
            var key = $"{DateTime.UtcNow.Ticks}-{index:D4}-test-key.txt";
            await s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                ContentBody = "hello world",
                Key = key,
            });
            Log.Information("Task {Index}: SUCCESS", index);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Task {Index}: FAILED - {Type}: {Message}", index, ex.GetType().Name, ex.Message);
            lock (errorLock) { errors.Add(ex); }
        }
        finally
        {
            semaphore.Release();
        }
    });
}

await Task.WhenAll(tasks);

if (errors.Count > 0)
    Log.Error("{Count} task(s) failed out of {Total}", errors.Count, concurrency);