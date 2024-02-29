using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.Textract;
using Amazon.Textract.Model;
using Newtonsoft.Json;
using System.Data;

namespace armanproject
{
    // Class for storing extracted text
    public class TextClass
    {
        public string Text { get; set; }
        public float Confidence { get; set; }
    }

    // Class for storing extracted table data
    public class DataTable
    {
        // Property to store table data
        public System.Data.DataTable Table { get; }

        // Constructor to initialize the table
        public DataTable()
        {
            Table = new System.Data.DataTable();
            Table.Columns.Add("Text", typeof(string)); // Add a single column for text
        }
    }

    // Class for storing extracted forms 
    public class Forms
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    // Main Class
    public class Program
    {
    static async Task Main(string[] args)
    {
        var awsCredentials = new Amazon.Runtime.BasicAWSCredentials("AKIATRMF5LV4HOEP45HR", "PjMUHt2sHUmfqd05v/ezbtbp6bbkwD4wTo0D9yum");
        var awsRegion = Amazon.RegionEndpoint.APSoutheast1;

        var textractClient = new AmazonTextractClient(awsCredentials, awsRegion);

        string s3BucketName = "armansample";
        string s3ObjectKey = "sampleimg1.jpg";

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

        var extractedText = new List<TextClass>();
        var extractedDataTable = new DataTable();
        var extractedForms = new List<Forms>(); 

        // Extracted Text from AWS moved to TextClass
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
                // Extract text from table cell
                StringBuilder cellText = new StringBuilder();
                foreach (var relationship in item.Relationships)
                {
                    if (relationship.Type == "CHILD")
                    {
                        foreach (var childId in relationship.Ids)
                        {
                            var childBlock = response.Blocks.Find(b => b.Id == childId);
                            if (childBlock != null && childBlock.BlockType == "WORD")
                            {
                                cellText.Append(childBlock.Text);
                                cellText.Append(" ");
                            }
                        }
                    }
                }

                // Add the extracted text to the DataTable
                extractedDataTable.Table.Rows.Add(cellText.ToString().Trim());
            }
            else if (item.BlockType == "KEY_VALUE_SET" && item.EntityTypes.Contains("KEY"))
            {
                string key = "";
                string value = "";

                foreach (var relationship in item.Relationships)
                {
                    if (relationship.Type == "CHILD")
                    {
                        foreach (var childId in relationship.Ids)
                        {
                            var childBlock = response.Blocks.Find(b => b.Id == childId);
                            if (childBlock != null && childBlock.BlockType == "WORD")
                            {
                                if (item.EntityTypes.Contains("KEY"))
                                {
                                    key += childBlock.Text + " ";
                                }
                                else if (item.EntityTypes.Contains("VALUE"))
                                {
                                    value += childBlock.Text + " ";
                                }
                            }
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                {
                    extractedForms.Add(new Forms { Key = key.Trim(), Value = value.Trim() });
                }
            }
        }

        // Extracted Text converted to serialized Json Format
        Console.WriteLine("Extracted text:");
        foreach (var text in extractedText)
        {
            if (text.Confidence > 50)
            {
                var json = JsonConvert.SerializeObject(text, Formatting.Indented);
                Console.WriteLine(json);
            }
        }

        // Printing extracted table data
        Console.WriteLine("\nExtracted table data:");
        foreach (DataRow row in extractedDataTable.Table.Rows)
        {
            var json = JsonConvert.SerializeObject(row["Text"]);
            Console.WriteLine(json);
        }

        // Printing extracted forms data
        Console.WriteLine("\nExtracted forms data:");
        foreach (var form in extractedForms)
        {
            var json = JsonConvert.SerializeObject(form, Formatting.Indented);
            Console.WriteLine($"KEY({form.Key}) VALUES({form.Value})");
        }

        Console.ReadLine();
    }
    }
}
