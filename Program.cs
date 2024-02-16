using System;
using System.Collections.Generic;
using Amazon;
using Amazon.Textract;
using Amazon.Textract.Model;

namespace armanproject
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            // Replace these values with your AWS access key ID and secret access key
            string accessKeyId = "AKIATRMF5LV4HOEP45HR";
            string secretAccessKey = "PjMUHt2sHUmfqd05v/ezbtbp6bbkwD4wTo0D9yum";

            // Specify the AWS region where your Amazon Textract resources are located
            var region = RegionEndpoint.APSoutheast1; // Specify the correct AWS region code

            // Configure the Amazon Textract client
            var config = new AmazonTextractConfig
            {
                RegionEndpoint = region
            };

            // Initialize the Amazon Textract client
            using (var client = new AmazonTextractClient(accessKeyId, secretAccessKey, config))
            {
                // Specify the parameters for the AnalyzeDocument request
                var request = new AnalyzeDocumentRequest
                {
                    Document = new Document
                    {
                        S3Object = new S3Object
                        {
                            Bucket = "textract-console-ap-southeast-1-b22603d0-88bd-4104-ba39-6dbb9ca", // Replace with your S3 bucket name
                            Name = "5e38317b_ca34_49c7_b821_7439eb899d1c_example2.pdf" // Replace with the name of your document (PDF or image)
                        }
                    },
                    FeatureTypes = new List<string> { "TABLES", "FORMS" } // Specify the features to extract
                };

                // Call the AnalyzeDocument operation asynchronously
                var response = await client.AnalyzeDocumentAsync(request);

                // Process the response
    foreach (var page in response.Blocks)
    {
    // Extract text blocks
    if (page.BlockType == "LINE")
    {
        Console.WriteLine(page.Text);
    }
    }

                // Process the response to extract form data, tables, etc.
                // Display or save the extracted data as needed
            }
        }
    }
}