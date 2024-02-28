using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.Textract;
using Amazon.Textract.Model;
using Newtonsoft.Json;

namespace armanproject
{
    class TextClass
    {
        public string Text { get; set; }
        public float Confidence { get; set; }
    }

    class TableCell
    {
        public string Text { get; set; }
        public float Confidence { get; set; }
        public int RowIndex { get; set; }
        public int ColumnIndex { get; set; }

        public static async Task<TableCell> ExtractFromBlock(Block block, AnalyzeDocumentResponse response)
        {
            TableCell tableCell = new TableCell();

            if (block.BlockType == "CELL")
            {
                StringBuilder textBuilder = new StringBuilder();

                foreach (var relationship in block.Relationships)
                {
                    if (relationship.Type == "CHILD")
                    {
                        foreach (var childId in relationship.Ids)
                        {
                            var childBlock = response.Blocks.Find(b => b.Id == childId);
                            if (childBlock != null && childBlock.BlockType == "WORD")
                            {
                                textBuilder.Append(childBlock.Text);
                                textBuilder.Append(" "); 
                            }
                        }
                    }
                }

                tableCell.Text = textBuilder.ToString().Trim();
                tableCell.Confidence = block.Confidence;
                tableCell.RowIndex = block.RowIndex;
                tableCell.ColumnIndex = block.ColumnIndex;
            }

            return tableCell;
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            var awsCredentials = new Amazon.Runtime.BasicAWSCredentials("AKIATRMF5LV4HOEP45HR", "PjMUHt2sHUmfqd05v/ezbtbp6bbkwD4wTo0D9yum");
            var awsRegion = Amazon.RegionEndpoint.APSoutheast1;

            var textractClient = new AmazonTextractClient(awsCredentials, awsRegion);

            string s3BucketName = "armansample";
            string s3ObjectKey = "sampleimg1.jpg";

            var extractedText = new List<TextClass>();
            var extractedTableCells = new List<TableCell>();

            var request = new AnalyzeDocumentRequest
            {
                Document = new Document
                {
                    S3Object = new S3Object
                    {
                        Bucket = s3BucketName,
                        Name = s3ObjectKey
                    }
                },

                FeatureTypes = new List<string> { "TABLES", "FORMS" }
            };

            var response = await textractClient.AnalyzeDocumentAsync(request);

            foreach (var item in response.Blocks)
            {
                if (item.BlockType == "WORD")
                {
                    var textClass = new TextClass
                    {
                        Text = item.Text,
                        Confidence = item.Confidence
                    };
                    extractedText.Add(textClass);
                }
                else if (item.BlockType == "CELL")
                {
                    TableCell tableCell = await TableCell.ExtractFromBlock(item, response);
                    if (!string.IsNullOrWhiteSpace(tableCell.Text)) 
                    {
                        extractedTableCells.Add(tableCell);
                    }
                }
            }
            

            Console.WriteLine("Extracted text:");
            foreach (var text in extractedText)
            {
                var json = JsonConvert.SerializeObject(text, Formatting.Indented);
                Console.WriteLine(json);
            }

            Console.WriteLine("\nExtracted table cells:");
            foreach (var cell in extractedTableCells)
            {
                var json = JsonConvert.SerializeObject(cell, Formatting.Indented);
                Console.WriteLine(json);
            }

            Console.ReadLine();
        }
    }
}
