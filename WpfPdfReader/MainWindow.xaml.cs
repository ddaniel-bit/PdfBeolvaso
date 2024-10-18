using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace WpfPdfReader
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ReadPdf(string filePath)
        {
            var findings = new List<Finding>();
            Finding currentFinding = null;
            string currentSection = null;

            using (PdfReader reader = new PdfReader(filePath))
            using (PdfDocument pdf = new PdfDocument(reader))
            {
                for (int page = 1; page <= pdf.GetNumberOfPages(); page++)
                {
                    var strategy = new LocationTextExtractionStrategy();
                    PdfCanvasProcessor parser = new PdfCanvasProcessor(strategy);
                    parser.ProcessPageContent(pdf.GetPage(page));
                    string text = strategy.GetResultantText();
                    var lines = text.Split(new[] { "\n", "\r\n" }, StringSplitOptions.None);

                    foreach (var line in lines)
                    {
                        string trimmedLine = line.Trim();
                        if (Regex.IsMatch(trimmedLine, @"^\d+ - .+"))
                        {
                            if (currentFinding != null)
                            {
                                findings.Add(currentFinding);
                            }
                            currentFinding = new Finding { Title = trimmedLine };
                            currentSection = null;
                        }
                        else if (currentFinding != null)
                        {
                            if (Regex.IsMatch(trimmedLine, @"^(Synopsis|Description|Solution|Risk Factor|See Also|Plugin Information|Plugin Output)$"))
                            {
                                currentSection = trimmedLine;
                                currentFinding.AddDetail(currentSection, "");
                            }
                            else if (!string.IsNullOrWhiteSpace(trimmedLine) && currentSection != null)
                            {
                                currentFinding.AppendToDetail(currentSection, trimmedLine);
                            }
                        }
                    }
                }

                if (currentFinding != null)
                {
                    findings.Add(currentFinding);
                }
            }

            DisplayFindings(findings);
        }

        private void DisplayFindings(List<Finding> findings)
        {
            FindingsStackPanel.Children.Clear();

            foreach (var finding in findings)
            {
                var expander = new Expander
                {
                    Header = new TextBlock
                    {
                        Text = finding.Title,
                        TextWrapping = TextWrapping.Wrap
                    },
                    IsExpanded = false,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var contentPanel = new StackPanel();

                foreach (var detail in finding.Details)
                {
                    contentPanel.Children.Add(new TextBlock
                    {
                        Text = detail.Key,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 10, 0, 5)
                    });
                    contentPanel.Children.Add(new TextBlock
                    {
                        Text = detail.Value,
                        TextWrapping = TextWrapping.Wrap
                    });
                }

                expander.Content = contentPanel;
                FindingsStackPanel.Children.Add(expander);
            }
        }

        private void ReadPdfButton_Click(object sender, RoutedEventArgs e)
        {
            string filePath = "../../../SampleNetworkVulnerabilityScanReport.pdf"; 
            ReadPdf(filePath);
        }
    }

    public class Finding
    {
        public string Title { get; set; }
        public Dictionary<string, string> Details { get; set; } = new Dictionary<string, string>();

        public void AddDetail(string key, string value)
        {
            Details[key] = value;
        }

        public void AppendToDetail(string key, string text)
        {
            if (Details.ContainsKey(key))
            {
                Details[key] += Details[key].Length > 0 ? " " + text : text;
            }
            else
            {
                Details[key] = text;
            }
        }
    }
}