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
var tasks = Enumerable.Range(0, 256).Select(i => s3Client.PutObjectAsync(new PutObjectRequest
{
    BucketName = bucketName,
    ContentBody = "hello world",
    Key = $"{DateTime.UtcNow.Ticks}-{i}-test-key.txt"
}));
try
{
    await Task.WhenAll(tasks);
}
catch (Exception ex)
{
    Log.Error(ex, "One or more S3 uploads failed");
}