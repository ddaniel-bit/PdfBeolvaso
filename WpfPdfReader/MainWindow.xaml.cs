using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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
            try
            {
                var findings = new List<Finding>();
                Finding currentFinding = null;
                string currentSection = null;

                using (PdfReader reader = new PdfReader(filePath))
                using (PdfDocument pdf = new PdfDocument(reader))
                {
                    for (int page = 1; page <= pdf.GetNumberOfPages(); page++)
                    {
                        // A PDF feldolgozási logikája itt folytatódik...
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
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a PDF fájl beolvasása során: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

                // Kapcsolódás a szerverhez és válasz fogadása
                string serverResponse = GetServerResponse(finding.Title);
                expander.Header = new TextBlock
                {
                    Text = $"{finding.Title} - Kategória: {serverResponse}",
                    TextWrapping = TextWrapping.Wrap
                };

                expander.Content = contentPanel;
                FindingsStackPanel.Children.Add(expander);
            }
        }

        private string GetServerResponse(string title)
        {
            try
            {
                // Kapcsolódás a szerverhez
                using (TcpClient client = new TcpClient("127.0.0.1", 8080))
                {
                    NetworkStream stream = client.GetStream();

                    // Üzenet küldése a szervernek
                    byte[] messageBytes = Encoding.ASCII.GetBytes(title);
                    stream.Write(messageBytes, 0, messageBytes.Length);

                    // Válasz fogadása a szervertől
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    return response;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a szerverhez való csatlakozáskor: {ex.Message}");
                return "N/A";
            }
        }

        private void ReadPdfButton_Click(object sender, RoutedEventArgs e)
        {
            string filePath = "../../../SampleNetworkVulnerabilityScanReport.pdf";
            ReadPdf(filePath);
        }
    }
}
