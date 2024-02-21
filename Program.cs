using System;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.Textract;
using Amazon.Textract.Model;

namespace armanproject
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Configure your AWS credentials and region
            var accessKeyId = "AKIATRMF5LV4HOEP45HR";
            var secretAccessKey = "PjMUHt2sHUmfqd05v/ezbtbp6bbkwD4wTo0D9yum";
            var region = RegionEndpoint.APSoutheast1; // Update with your region

            // Initialize Amazon Textract client
            using var textractClient = new AmazonTextractClient(accessKeyId, secretAccessKey, region);

            // Specify the S3 bucket name and the document key
            var bucketName = "armansample";
            var documentKey = "example8.pdf";

           var jobId = await StartDocumentTextDetectionAsync(textractClient, bucketName, documentKey);

            Console.WriteLine($"Text detection job started with ID: {jobId}");

            // Wait for the job to complete
            Console.WriteLine("Waiting for job to complete...");
            var jobStatus = await WaitForJobCompletionAsync(textractClient, jobId);

            if (jobStatus == "SUCCEEDED")
            {
                // Get the results of the text detection job
                var extractedText = await GetExtractedTextAsync(textractClient, jobId);

                // Output the extracted content
                extractedText.Output();
            }
            else
            {
                Console.WriteLine($"Job failed with status: {jobStatus}");
            }
        }

        static async Task<string> StartDocumentTextDetectionAsync(AmazonTextractClient textractClient, string bucketName, string documentKey)
        {
            var request = new StartDocumentTextDetectionRequest
            {
                DocumentLocation = new DocumentLocation
                {
                    S3Object = new S3Object
                    {
                        Bucket = bucketName,
                        Name = documentKey
                    }
                }
            };

            var response = await textractClient.StartDocumentTextDetectionAsync(request);
            return response.JobId;
        }

        static async Task<string> WaitForJobCompletionAsync(AmazonTextractClient textractClient, string jobId)
        {
            string jobStatus = null;

            while (true)
            {
                var response = await textractClient.GetDocumentTextDetectionAsync(new GetDocumentTextDetectionRequest
                {
                    JobId = jobId
                });

                jobStatus = response.JobStatus;

                if (jobStatus == "SUCCEEDED" || jobStatus == "FAILED" || jobStatus == "PARTIAL_SUCCESS")
                {
                    break;
                }

                // Wait before polling again
                await Task.Delay(5000); // Wait for 5 seconds before polling again
            }

            return jobStatus;
        }

        static async Task<ExtractedContent> GetExtractedTextAsync(AmazonTextractClient textractClient, string jobId)
        {
            var response = await textractClient.GetDocumentTextDetectionAsync(new GetDocumentTextDetectionRequest
            {
                JobId = jobId
            });

            // Process the blocks and separate them into text, tables, and forms
            var textBlocks = new List<Block>();
            var tableBlocks = new List<Block>();
            var formBlocks = new List<Block>();

            foreach (var item in response.Blocks)
            {
                switch (item.BlockType)
                {
                    case "LINE":
                        textBlocks.Add(item);
                        break;
                    case "TABLE":
                        tableBlocks.Add(item);
                        break;
                    case "KEY_VALUE_SET":
                        formBlocks.Add(item);
                        break;
                }
            }

            return new ExtractedContent
            {
                Text = new Text(textBlocks),
                Tables = new Tables(tableBlocks),
                Forms = new Forms(formBlocks)
            };
        }
    }

    class ExtractedContent
    {
        public Text Text { get; set; }
        public Tables Tables { get; set; }
        public Forms Forms { get; set; }

        public void Output()
        {
            Console.WriteLine("Text:");
            Text.Output();

            Console.WriteLine("\nTables:");
            Tables.Output();

            Console.WriteLine("\nForms:");
            Forms.Output();
        }
    }

    class Text
    {
        private List<Block> _textBlocks;

        public Text(List<Block> textBlocks)
        {
            _textBlocks = textBlocks;
        }

        public void Output()
        {
            foreach (var block in _textBlocks)
            {
                Console.WriteLine(block.Text);
            }
        }
    }

    class Tables
    {
        private List<Block> _tableBlocks;

        public Tables(List<Block> tableBlocks)
        {
            _tableBlocks = tableBlocks;
        }

        public void Output()
        {
            foreach (var block in _tableBlocks)
            {
                Console.WriteLine("Table:");
                Console.WriteLine(block.Text);
            }
        }
    }

    class Forms
    {
        private List<Block> _formBlocks;

        public Forms(List<Block> formBlocks)
        {
            _formBlocks = formBlocks;
        }

        public void Output()
        {
            foreach (var block in _formBlocks)
            {
                Console.WriteLine("Form:");
                Console.WriteLine(block.Text);
            }
        }
    }
}
