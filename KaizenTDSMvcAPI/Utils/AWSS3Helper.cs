using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using SystemLibrary.Utility;

namespace KaizenTDSMvcAPI.Utils
{
    public class AWSS3Helper
    {
        static string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        public bool Download_from_s3(string bucketname, string prefix, string fileName, string Str_Dest_Path)
        {
            string Str_Res = string.Empty;

            try
            {
                if (!Directory.Exists(Str_Dest_Path)) 
                    Directory.CreateDirectory(Str_Dest_Path);
                string accessKey = ConfigurationManager.AppSettings["AWSAccessKey"].ToString();
                string secretKey = ConfigurationManager.AppSettings["AWSSecretKey"].ToString();
                AmazonS3Config config = new AmazonS3Config();
                config.RegionEndpoint = RegionEndpoint.USWest2;

                AmazonS3Client S3_Client = new AmazonS3Client(accessKey, secretKey, config);
                ListObjectsRequest request = new ListObjectsRequest();
                request.BucketName = bucketname;
                request.Prefix = prefix + "/" + fileName;
                
                ListObjectsResponse response = S3_Client.ListObjects(request);
                if (response.S3Objects.Count() == 0)
                    return false;
                var files = response.S3Objects.FindAll(r => r.Key.Contains(fileName)).OrderByDescending(r => r.LastModified);

                if (files != null && files.Count() > 0)
                {
                    GetObjectRequest Req = new GetObjectRequest();
                    GetObjectResponse Resp = new GetObjectResponse();
                    Req.BucketName = files.First().BucketName;
                    Req.Key = files.First().Key;
                    Resp = S3_Client.GetObject(Req);

                    if (!Directory.Exists(Str_Dest_Path))
                    {
                        Directory.CreateDirectory(Str_Dest_Path);
                    }
                    //string fileName = file.Key.Split('/').Last();
                    Resp.WriteResponseStreamToFile(System.IO.Path.Combine(Str_Dest_Path, fileName));
                    //Str_Res = "ok," + System.IO.Path.Combine(Str_Dest_Path, fileName);


                    //GetPreSignedUrlRequest urlRequest = new GetPreSignedUrlRequest() { BucketName = file.BucketName, Key = file.Key, Protocol = Protocol.HTTP, Expires = DateTime.Now.AddHours(3) };
                    //var url = S3_Client.GetPreSignedURL(urlRequest);

                    return true;
                }
                else return false;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public Stream Download_from_s3(string bucketname, string awsFileFullName)
        {
            MemoryStream rs = new MemoryStream();
            string Str_Res = string.Empty;
            GetObjectRequest Req = new GetObjectRequest();
            GetObjectResponse resp = new GetObjectResponse();
            try
            {
                string accessKey = ConfigurationManager.AppSettings["AWSAccessKey"].ToString();
                string secretKey = ConfigurationManager.AppSettings["AWSSecretKey"].ToString();
                AmazonS3Config config = new AmazonS3Config();
                config.RegionEndpoint = RegionEndpoint.USWest2;

                AmazonS3Client S3_Client = new AmazonS3Client(accessKey, secretKey, config);
                ListObjectsRequest request = new ListObjectsRequest();
                request.BucketName = bucketname;
                request.Prefix = awsFileFullName;

                ListObjectsResponse response = S3_Client.ListObjects(request);
                var file = response.S3Objects.FindAll(r => r.Key.Contains(awsFileFullName)).OrderByDescending(r => r.LastModified).FirstOrDefault();
                if (response.S3Objects.Count() > 0 && file != null)
                {
                    Req.BucketName = file.BucketName;
                    Req.Key = file.Key;
                    resp = S3_Client.GetObject(Req);
                    var getObjectResponse = S3_Client.GetObject(Req);
                    using (Stream responseStream = resp.ResponseStream)
                    {
                        var bytes = ReadStream(responseStream);
                        rs = new MemoryStream(bytes);
                    }
                }
                else return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return rs;
        }

        public void DownloadDirectory_from_s3(string bucketname, string prefix, string downloadPath)
        {
            string accessKey = ConfigurationManager.AppSettings["AWSAccessKey"].ToString();
            string secretKey = ConfigurationManager.AppSettings["AWSSecretKey"].ToString();

            var bucketRegion = RegionEndpoint.USWest2;
            var credentials = new BasicAWSCredentials(accessKey, secretKey);
            var client = new AmazonS3Client(credentials, bucketRegion);

            var request = new ListObjectsRequest
            {
                BucketName = bucketname,
                Prefix = prefix
            };

            try
            {
                var utility = new TransferUtility(client);
                ListObjectsResponse response = null;
                do
                {
                    response = client.ListObjects(request);
                    var s3Objects = response.S3Objects;
                    //foreach (var obj in s3Objects)
                    //{
                    //    utility.Download($"{downloadPath}\\{obj.Key}", bucketname, obj.Key);
                    //}

                    s3Objects.AsParallel().WithDegreeOfParallelism(20).ForAll(obj =>
                    {
                        if (downloadPath.Contains(obj.Key) == false)
                        {
                            var s3ObjArry = obj.Key.Split('/');
                            var folderName = s3ObjArry[s3ObjArry.Count() - 2];
                            utility.Download($"{downloadPath}\\{folderName}\\{s3ObjArry.LastOrDefault()}", bucketname, obj.Key);
                        }
                            
                    });
                    if (response.IsTruncated)
                    {
                        request.Marker = response.NextMarker;
                    }
                    else { request = null; }
                }
                while (request != null);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public void DownloadDirectory_from_s3_V2(string bucketname, string prefix, string downloadPath)
        {
            string accessKey = ConfigurationManager.AppSettings["AWSAccessKey"].ToString();
            string secretKey = ConfigurationManager.AppSettings["AWSSecretKey"].ToString();

            var bucketRegion = RegionEndpoint.USWest2;
            var credentials = new BasicAWSCredentials(accessKey, secretKey);
            var client = new AmazonS3Client(credentials, bucketRegion);

            var request = new ListObjectsRequest
            {
                BucketName = bucketname,
                Prefix = prefix
            };

            try
            {
                var utility = new TransferUtility(client);
                utility.DownloadDirectory(bucketname, prefix, downloadPath);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public string GenerateFileURL_from_s3(string bucketname, string fileName)
        {
            string Str_Res = string.Empty;
            string url = string.Empty;
            try
            {
                string accessKey = ConfigurationManager.AppSettings["AWSAccessKey"].ToString();
                string secretKey = ConfigurationManager.AppSettings["AWSSecretKey"].ToString();
                AmazonS3Config config = new AmazonS3Config();
                config.RegionEndpoint = RegionEndpoint.USWest2;

                AmazonS3Client S3_Client = new AmazonS3Client(accessKey, secretKey, config);
                ListObjectsRequest request = new ListObjectsRequest();
                request.BucketName = bucketname;
                request.Prefix = fileName;
                ListObjectsResponse response = S3_Client.ListObjects(request);
                if (response.S3Objects.Count() == 0)
                    return url;
                var file = response.S3Objects.FindAll(r => r.Key.Contains(fileName)).OrderByDescending(r => r.LastModified).FirstOrDefault();

                if (file != null)
                {
                    GetPreSignedUrlRequest urlRequest = new GetPreSignedUrlRequest() { BucketName = file.BucketName, Key = file.Key, Protocol = Protocol.HTTP, Expires = DateTime.Now.AddHours(3) };
                    url = S3_Client.GetPreSignedURL(urlRequest);

                }
                return url;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public Dictionary<string, string> GenerateAllURL_from_s3(string bucketname, string prefix)
        {
            string Str_Res = string.Empty;
            Dictionary<string, string> urlDic = new Dictionary<string, string>();
            try
            {
                string accessKey = ConfigurationManager.AppSettings["AWSAccessKey"].ToString();
                string secretKey = ConfigurationManager.AppSettings["AWSSecretKey"].ToString();
                AmazonS3Config config = new AmazonS3Config();
                config.RegionEndpoint = RegionEndpoint.USWest2;

                AmazonS3Client S3_Client = new AmazonS3Client(accessKey, secretKey, config);
                ListObjectsRequest request = new ListObjectsRequest();
                request.BucketName = bucketname;
                request.Prefix = prefix;
                ListObjectsResponse response = null;
                do
                {
                    response = S3_Client.ListObjects(request);
                    var files = response.S3Objects;
                    foreach (var item in files)
                    {
                        GetPreSignedUrlRequest urlRequest = new GetPreSignedUrlRequest() { BucketName = item.BucketName, Key = item.Key, Protocol = Protocol.HTTP, Expires = DateTime.Now.AddHours(3) };
                        urlDic.Add(item.Key, S3_Client.GetPreSignedURL(urlRequest));
                    }
                    if (response.IsTruncated)
                    {
                        request.Marker = response.NextMarker;
                    }
                    else { request = null; }
                }
                while (request != null);

                return urlDic;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        private static RegionEndpoint ParseRegion(string region)
        {
            RegionEndpoint regionEndPoint;

            switch (region)
            {
                case "APNortheast1":
                    regionEndPoint = RegionEndpoint.APNortheast1;
                    break;
                case "APSoutheast1":
                    regionEndPoint = RegionEndpoint.APSoutheast1;
                    break;
                case "APSoutheast2":
                    regionEndPoint = RegionEndpoint.APSoutheast2;
                    break;
                case "CNNorth1":
                    regionEndPoint = RegionEndpoint.CNNorth1;
                    break;
                case "EUWest1":
                    regionEndPoint = RegionEndpoint.EUWest1;
                    break;
                case "EUCentral1":
                    regionEndPoint = RegionEndpoint.EUCentral1;
                    break;
                case "SAEast1":
                    regionEndPoint = RegionEndpoint.SAEast1;
                    break;
                case "USEast1":
                    regionEndPoint = RegionEndpoint.USEast1;
                    break;
                case "USGovCloudWest1":
                    regionEndPoint = RegionEndpoint.USGovCloudWest1;
                    break;
                case "USWest1":
                    regionEndPoint = RegionEndpoint.USWest1;
                    break;
                case "USWest2":
                    regionEndPoint = RegionEndpoint.USWest2;
                    break;
                default:
                    regionEndPoint = RegionEndpoint.USEast1;
                    break;
            }

            return regionEndPoint;
        }

        private static byte[] ReadStream(Stream responseStream)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}