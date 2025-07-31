# TechScriptAid Enterprise AI Development

Welcome to the official code repository for the TechScriptAid YouTube channel! This repository contains all the code from our tutorial series on building production-ready enterprise applications with .NET and AI.

## 🎯 About This Series

Learn how to build enterprise-grade applications combining:
- **.NET 8** - Latest features and best practices
- **Clean Architecture** - Maintainable and testable code
- **Azure AI Services** - Integration with OpenAI and Cognitive Services
- **Real-world patterns** - From 17+ years of enterprise experience

## 📁 Project Structure
TechScriptAid.EnterpriseAI/
├── src/
│   ├── TechScriptAid.API/           # Web API Layer
│   ├── TechScriptAid.Core/          # Domain Entities & Interfaces
│   ├── TechScriptAid.Infrastructure/# Data Access & External Services
│   └── TechScriptAid.AI/            # AI Integration Layer
├── tests/                           # Unit and Integration Tests
├── docs/                            # Documentation
│   └── episodes/                    # Episode-specific guides
└── samples/                         # Code samples and demos

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or VS Code
- Azure Account (free tier works)
- Git

### Running the Application
```bash
# Clone the repository
git clone https://github.com/hsimrat/TechScriptAid-Enterprise-AI.git

# Navigate to the project
cd TechScriptAid-Enterprise-AI

# Restore dependencies
dotnet restore

# Run the API
dotnet run --project src/TechScriptAid.API
📺 Episodes

Episode 001 - Setting Up Enterprise Development Environment
Episode 002 - Building Your First Clean Architecture API (Coming Soon)
Episode 003 - Adding Azure OpenAI Integration (Coming Soon)

🛠️ Technologies Used

Backend: .NET 8, ASP.NET Core, C# 12
Architecture: Clean Architecture, CQRS, Repository Pattern
AI/ML: Azure OpenAI, Semantic Kernel, ML.NET
Database: SQL Server, Entity Framework Core
Testing: xUnit, Moq, FluentAssertions
CI/CD: GitHub Actions, Azure DevOps

📧 Connect

YouTube: TechScriptAid Channel
GitHub: @hsimrat
LinkedIn: [Your LinkedIn Profile]

📝 License
This project is licensed under the MIT License - see the LICENSE file for details.

⭐ If you find this helpful, please star the repository and subscribe to the channel!
