using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using TalentX.Infrastructure.TextExtraction;
using Xunit;

namespace TalentX.Infrastructure.Tests.TextExtraction;

public class DocxDocumentParserTests
{
    [Fact]
    public void ExtractContent_ParagraphWithMultipleRuns_JoinsWithSpaces()
    {
        // Arrange: Create a DOCX with multiple text runs in one paragraph
        using var memoryStream = new MemoryStream();
        
        using (var doc = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());
            
            // Create a paragraph with multiple runs (simulating different formatting)
            var paragraph = new Paragraph();
            
            // Run 1: Email address
            var run1 = new Run();
            var text1 = new Text("test@example.com");
            run1.Append(text1);
            paragraph.Append(run1);
            
            // Run 2: Location (different formatting would be here in real doc)
            var run2 = new Run();
            var text2 = new Text("Dhaka");
            run2.Append(text2);
            paragraph.Append(run2);
            
            body.Append(paragraph);
            mainPart.Document.Save();
        }
        
        memoryStream.Position = 0;
        
        // Act
        var (plainText, links) = DocxDocumentParser.ExtractContent(memoryStream);
        
        // Assert: Text runs should be joined with spaces, not concatenated
        plainText.Should().Contain("test@example.com Dhaka");
        plainText.Should().NotContain("test@example.comDhaka");
    }
    
    [Fact]
    public void ExtractContent_EmailFollowedByLocation_PreservesWordBoundary()
    {
        // Arrange: Simulate the bug scenario from TX26-433
        using var memoryStream = new MemoryStream();
        
        using (var doc = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());
            
            var paragraph = new Paragraph();
            
            // Run 1: Email
            var run1 = new Run();
            run1.Append(new Text("1716789357afmnaibuddin@gmail.com"));
            paragraph.Append(run1);
            
            // Run 2: Location (Shantinagar)
            var run2 = new Run();
            run2.Append(new Text("Shantinagar"));
            paragraph.Append(run2);
            
            body.Append(paragraph);
            mainPart.Document.Save();
        }
        
        memoryStream.Position = 0;
        
        // Act
        var (plainText, _) = DocxDocumentParser.ExtractContent(memoryStream);
        
        // Assert: Should have space between email and location
        plainText.Should().Contain("1716789357afmnaibuddin@gmail.com Shantinagar");
        plainText.Should().NotContain("1716789357afmnaibuddin@gmail.comShantinagar");
    }
    
    [Fact]
    public void ExtractContent_SingleRunParagraph_ExtractsCorrectly()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        
        using (var doc = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());
            
            var paragraph = new Paragraph();
            var run = new Run();
            run.Append(new Text("Single run text"));
            paragraph.Append(run);
            
            body.Append(paragraph);
            mainPart.Document.Save();
        }
        
        memoryStream.Position = 0;
        
        // Act
        var (plainText, _) = DocxDocumentParser.ExtractContent(memoryStream);
        
        // Assert
        plainText.Should().Contain("Single run text");
    }
    
    [Fact]
    public void ExtractContent_EmptyParagraph_SkipsEmptyLines()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        
        using (var doc = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());
            
            // Empty paragraph
            body.Append(new Paragraph());
            
            // Paragraph with content
            var paragraph = new Paragraph();
            var run = new Run();
            run.Append(new Text("Content"));
            paragraph.Append(run);
            body.Append(paragraph);
            
            mainPart.Document.Save();
        }
        
        memoryStream.Position = 0;
        
        // Act
        var (plainText, _) = DocxDocumentParser.ExtractContent(memoryStream);
        
        // Assert
        plainText.Should().Contain("Content");
        // Should not have excessive empty lines
        var lines = plainText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(1);
    }
    
    [Fact]
    public void ExtractContent_MultipleParagraphs_PreservesLineBreaks()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        
        using (var doc = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());
            
            // First paragraph
            var para1 = new Paragraph();
            para1.Append(new Run(new Text("First line")));
            body.Append(para1);
            
            // Second paragraph
            var para2 = new Paragraph();
            para2.Append(new Run(new Text("Second line")));
            body.Append(para2);
            
            mainPart.Document.Save();
        }
        
        memoryStream.Position = 0;
        
        // Act
        var (plainText, _) = DocxDocumentParser.ExtractContent(memoryStream);
        
        // Assert
        plainText.Should().Contain("First line");
        plainText.Should().Contain("Second line");
        var lines = plainText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(2);
    }
}
