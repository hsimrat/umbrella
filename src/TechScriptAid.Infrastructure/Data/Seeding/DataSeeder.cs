using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using TechScriptAid.Core.Entities;
using TechScriptAid.Infrastructure.Data;

namespace TechScriptAid.Infrastructure.Data.Seeding
{
    public interface IDataSeeder
    {
        Task SeedAsync();
    }

    public class DataSeeder : IDataSeeder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(IServiceProvider serviceProvider, ILogger<DataSeeder> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                // Apply pending migrations
                if ((await context.Database.GetPendingMigrationsAsync()).Any())
                {
                    _logger.LogInformation("Applying pending migrations...");
                    await context.Database.MigrateAsync();
                    _logger.LogInformation("Migrations applied successfully.");
                }

                // Seed data
                await SeedDocumentsAsync(context);
                await SeedDocumentAnalysesAsync(context);

                _logger.LogInformation("Database seeding completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }

        private async Task SeedDocumentsAsync(ApplicationDbContext context)
        {
            if (await context.Documents.AnyAsync())
            {
                _logger.LogInformation("Documents already exist. Skipping document seeding.");
                return;
            }

            var documents = new List<Document>
            {
                new Document
                {
                    Title = "Enterprise AI Architecture Guide",
                    Description = "Comprehensive guide for implementing AI in enterprise applications",
                    Content = @"# Enterprise AI Architecture Guide

## Introduction
This guide provides a comprehensive overview of implementing AI capabilities in enterprise applications...

## Key Components
1. Data Pipeline
2. Model Management
3. API Gateway
4. Security Layer

## Best Practices
- Always validate input data
- Implement proper error handling
- Monitor model performance
- Ensure data privacy compliance",
                    DocumentType = DocumentType.TechnicalDocument,
                    FileName = "enterprise-ai-guide.md",
                    FileSize = 2048,
                    ContentHash = GenerateHash("enterprise-ai-guide-content"),
                    Tags = new List<string> { "AI", "Architecture", "Enterprise", "Guide" },
                    Metadata = new Dictionary<string, string>
                    {
                        { "Author", "TechScriptAid Team" },
                        { "Version", "1.0" },
                        { "Language", "en" }
                    }
                },
                new Document
                {
                    Title = "Clean Architecture Best Practices",
                    Description = "Best practices for implementing clean architecture in .NET applications",
                    Content = @"# Clean Architecture Best Practices

## Overview
Clean Architecture promotes separation of concerns and maintainability...

## Core Principles
- Independence of frameworks
- Testability
- Independence of UI
- Independence of Database

## Implementation in .NET
Use these patterns for optimal results...",
                    DocumentType = DocumentType.TechnicalDocument,
                    FileName = "clean-architecture.md",
                    FileSize = 1536,
                    ContentHash = GenerateHash("clean-architecture-content"),
                    Tags = new List<string> { "Architecture", "Clean Code", ".NET", "Best Practices" },
                    Metadata = new Dictionary<string, string>
                    {
                        { "Author", "TechScriptAid Team" },
                        { "Version", "1.0" },
                        { "Framework", ".NET 8" }
                    }
                },
                new Document
                {
                    Title = "Q4 2024 Financial Report",
                    Description = "Quarterly financial report for Q4 2024",
                    Content = @"# Q4 2024 Financial Report

## Executive Summary
Strong growth in AI services division...

## Revenue Breakdown
- Software Licenses: $2.5M
- AI Services: $1.8M
- Consulting: $1.2M

## Outlook
Positive growth expected in 2025...",
                    DocumentType = DocumentType.Report,
                    FileName = "q4-2024-financial.pdf",
                    FileSize = 3072,
                    ContentHash = GenerateHash("q4-financial-content"),
                    Tags = new List<string> { "Financial", "Q4", "2024", "Report" },
                    Metadata = new Dictionary<string, string>
                    {
                        { "Department", "Finance" },
                        { "Period", "Q4-2024" },
                        { "Confidential", "true" }
                    }
                },
                new Document
                {
                    Title = "AI Implementation Meeting Notes",
                    Description = "Notes from the AI implementation strategy meeting",
                    Content = @"# AI Implementation Strategy Meeting
Date: June 14, 2025

## Attendees
- CTO, VP Engineering, AI Team Lead

## Discussion Points
1. GPT-4 integration timeline
2. Infrastructure requirements
3. Security considerations

## Action Items
- Prepare POC by end of month
- Review security protocols
- Schedule follow-up meeting",
                    DocumentType = DocumentType.Memo,
                    FileName = "ai-meeting-notes.docx",
                    FileSize = 512,
                    ContentHash = GenerateHash("meeting-notes-content"),
                    Tags = new List<string> { "Meeting", "AI", "Strategy" },
                    Metadata = new Dictionary<string, string>
                    {
                        { "MeetingDate", "2025-06-14" },
                        { "Attendees", "5" },
                        { "Duration", "60 minutes" }
                    }
                }
            };

            context.Documents.AddRange(documents);
            await context.SaveChangesAsync();
            _logger.LogInformation($"Seeded {documents.Count} documents.");
        }

        private async Task SeedDocumentAnalysesAsync(ApplicationDbContext context)
        {
            if (await context.DocumentAnalyses.AnyAsync())
            {
                _logger.LogInformation("Document analyses already exist. Skipping analysis seeding.");
                return;
            }

            var documents = await context.Documents.ToListAsync();
            var analyses = new List<DocumentAnalysis>();

            foreach (var document in documents.Take(2)) // Add analyses for first 2 documents
            {
                analyses.Add(new DocumentAnalysis
                {
                    DocumentId = document.Id,
                    Document = document,
                    AnalysisType = AnalysisType.Summary,
                    Status = AnalysisStatus.Completed,
                    StartedAt = DateTime.UtcNow.AddMinutes(-5),
                    CompletedAt = DateTime.UtcNow.AddMinutes(-3),
                    DurationInSeconds = 120,
                    ModelUsed = "gpt-4",
                    ModelVersion = "2024-01-01",
                    Prompt = "Summarize this document in 3-5 sentences",
                    TokensUsed = 1500,
                    Cost = 0.045m,
                    Summary = $"This document titled '{document.Title}' provides comprehensive information about {document.Tags.FirstOrDefault()}. " +
                             "It covers key concepts and best practices that are essential for implementation. " +
                             "The content is well-structured and suitable for technical audiences.",
                    Keywords = new List<string> { "Technical", "Implementation", "Best Practices", document.Tags.FirstOrDefault() ?? "General" },
                    Sentiment = "Neutral",
                    SentimentScore = 0.65m,
                    Results = new Dictionary<string, object>
                    {
                        { "WordCount", 500 },
                        { "ReadingLevel", "Professional" },
                        { "MainTopics", new[] { "Architecture", "Implementation", "Guidelines" } }
                    }
                });

                analyses.Add(new DocumentAnalysis
                {
                    DocumentId = document.Id,
                    Document = document,
                    AnalysisType = AnalysisType.KeywordExtraction,
                    Status = AnalysisStatus.Completed,
                    StartedAt = DateTime.UtcNow.AddMinutes(-10),
                    CompletedAt = DateTime.UtcNow.AddMinutes(-9),
                    DurationInSeconds = 60,
                    ModelUsed = "gpt-3.5-turbo",
                    ModelVersion = "2024-01-01",
                    Prompt = "Extract top 10 keywords from this document",
                    TokensUsed = 800,
                    Cost = 0.008m,
                    Keywords = document.Tags.Concat(new[] { "Technology", "Innovation", "Enterprise" }).Take(10).ToList(),
                    Results = new Dictionary<string, object>
                    {
                        { "KeywordRelevanceScores", new Dictionary<string, double>
                            {
                                { document.Tags.FirstOrDefault() ?? "General", 0.95 },
                                { "Technology", 0.85 },
                                { "Innovation", 0.75 }
                            }
                        }
                    }
                });
            }

            // Add a pending analysis
            var lastDocument = documents.Last();
            analyses.Add(new DocumentAnalysis
            {
                DocumentId = lastDocument.Id,
                Document = lastDocument,
                AnalysisType = AnalysisType.SentimentAnalysis,
                Status = AnalysisStatus.Pending,
                ModelUsed = "gpt-4",
                ModelVersion = "2024-01-01",
                Prompt = "Analyze the sentiment of this document",
                Results = new Dictionary<string, object>()
            });

            context.DocumentAnalyses.AddRange(analyses);
            await context.SaveChangesAsync();
            _logger.LogInformation($"Seeded {analyses.Count} document analyses.");
        }

        private static string GenerateHash(string input)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }
    }

    // Extension method for IServiceCollection
    public static class DataSeederExtensions
    {
        public static IServiceCollection AddDataSeeder(this IServiceCollection services)
        {
            services.AddScoped<IDataSeeder, DataSeeder>();
            return services;
        }

        public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
            await seeder.SeedAsync();
        }
    }
}
