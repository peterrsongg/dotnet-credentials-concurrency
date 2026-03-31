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
var errors = new List<Exception>();
var errorLock = new object();

var tasks = Enumerable.Range(0, 64).Select(i => Task.Run(async () =>
{
    try
    {
        var key = $"{DateTime.UtcNow.Ticks}-{i:D4}-test-key.txt";
        await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucketName,
            ContentBody = "hello world",
            Key = key,
        });
        Log.Information("Task {Index}: SUCCESS", i);
        Console.WriteLine("Task{Index} failed", i);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Task {Index}: FAILED - {Type}: {Message}", i, ex.GetType().Name, ex.Message);
        Console.WriteLine("Task {Index}: FAILED - {Type}: {Message}", i, ex.GetType().Name, ex.Message);
        lock (errorLock) { errors.Add(ex); }
    }
}));

await Task.WhenAll(tasks);

if (errors.Count > 0)
    Log.Error("{Count} task(s) failed out of 64", errors.Count);