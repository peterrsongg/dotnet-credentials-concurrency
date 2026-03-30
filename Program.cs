using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using AWS.Logger;
using AWS.Logger.SeriLog;
using Serilog;
using var s3Client = new AmazonS3Client(RegionEndpoint.USWest2);
var bucketName = "dotnet-8554";
Log.Logger = new LoggerConfiguration()
    .WriteTo.AWSSeriLog(new AWSLoggerConfig(bucketName))
    .CreateLogger();
var tasks = Enumerable.Range(0, 64).Select(i => s3Client.PutObjectAsync(new PutObjectRequest
{
    BucketName = bucketName,
    ContentBody = "hello world",
    Key = $"{DateTime.UtcNow.Ticks}-{i}-test-key.txt"
}));
await Task.WhenAll(tasks);