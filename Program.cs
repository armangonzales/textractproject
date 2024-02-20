using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
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
            var region = RegionEndpoint.APSoutheast1;

            // Initialize Amazon Textract and S3 clients
            using var textractClient = new AmazonTextractClient(accessKeyId, secretAccessKey, region);
            using var s3Client = new AmazonS3Client(accessKeyId, secretAccessKey, region);

            // Specify the S3 bucket name and the PDF file key
            var bucketName = "armansample";
            var pdfFileKey = "sampleimg1.jpg";

            // Download the PDF file from S3
            var pdfContent = await GetS3ObjectContentAsync(s3Client, bucketName, pdfFileKey);

            // Specify the parameters for the AnalyzeDocument request
            var request = new AnalyzeDocumentRequest
            {
                Document = new Document
                {
                    Bytes = new MemoryStream(pdfContent)
                },
                FeatureTypes = new List<string> { "TABLES", "FORMS" } // Specify the features to extract
            };

            // Call the AnalyzeDocument operation asynchronously
            var response = await textractClient.AnalyzeDocumentAsync(request);

            // Process the response
            var extractionResult = ProcessResponse(response);

            // Output the extracted data
            extractionResult.Output();
        }

        static async Task<byte[]> GetS3ObjectContentAsync(IAmazonS3 s3Client, string bucketName, string objectKey)
        {
            var getObjectRequest = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey
            };

            using var response = await s3Client.GetObjectAsync(getObjectRequest);
            using var responseStream = response.ResponseStream;
            using var memoryStream = new MemoryStream();

            await responseStream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        static ExtractionResult ProcessResponse(AnalyzeDocumentResponse response)
        {
            var extractionResult = new ExtractionResult();

            // Dictionary to hold tables and their corresponding text blocks
            var tableBlocks = new Dictionary<string, List<Block>>();

            // Iterate through the blocks in the response
            foreach (var block in response.Blocks)
            {
                switch (block.BlockType)
                {
                    case "LINE":
                        // For lines, add the text to the raw text list
                        extractionResult.RawText.Add(block.Text);
                        break;
                    case "TABLE":
                        // For tables, add the table block to the dictionary
                        tableBlocks[block.Id] = new List<Block> { block };
                        break;
                    case "CELL":
                        // For cells, find the corresponding table and add the cell block to it
                        var cellTableId = block.Relationships.FirstOrDefault(r => r.Type == "CHILD")?.Ids[0];
                        if (cellTableId != null && tableBlocks.ContainsKey(cellTableId))
                        {
                            tableBlocks[cellTableId].Add(block);
                        }
                        break;
                    case "KEY_VALUE_SET":
                        // For key-value sets, add the form block and its relationships to the extraction result
                        extractionResult.Forms.Add(new Form { KeyValueSetBlock = block, Relationships = block.Relationships });
                        break;
                }
            }

            // Process tables
            foreach (var tableBlockList in tableBlocks.Values)
            {
                var table = new Table();
                foreach (var cellBlock in tableBlockList)
                {
                    table.AddCell(cellBlock.Text);
                }
                extractionResult.Tables.Add(table);
            }

            return extractionResult;
        }
    }

    class ExtractionResult
    {
        public List<string> RawText { get; } = new List<string>();
        public List<Table> Tables { get; } = new List<Table>();
        public List<Form> Forms { get; } = new List<Form>();

        public void Output()
        {
            Console.WriteLine("Tables:");
            foreach (var table in Tables)
            {
                table.Output();
            }

            Console.WriteLine("\nForms:");
            foreach (var form in Forms)
            {
                form.Output();
            }

            Console.WriteLine("\nRaw Text:");
            foreach (var text in RawText)
            {
                Console.WriteLine(text);
            }
        }
    }

    class Table
    {
        public List<List<string>> Rows { get; } = new List<List<string>>();

        public void AddCell(string cellText)
        {
            if (Rows.Count == 0 || Rows[Rows.Count - 1].Count > 0)
            {
                Rows.Add(new List<string>());
            }
            Rows[Rows.Count - 1].Add(cellText);
        }

        public void Output()
        {
            foreach (var row in Rows)
            {
                foreach (var cell in row)
                {
                    Console.Write(cell + " ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }

    class Form
    {
        public Block? KeyValueSetBlock { get; set; }
        public List<Relationship>? Relationships { get; set; }

        public void Output()
        {

            if (KeyValueSetBlock != null && Relationships != null){
            foreach (var relationship in Relationships)
            {
                Console.WriteLine($"Key: {relationship.Type}, Value: {relationship.Ids[0]}");
            }
            Console.WriteLine();
        }
       }
    }
}
