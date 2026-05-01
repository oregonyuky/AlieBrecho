# Manual das instruções

Este documento descreve a arquitetura e o padrão de organização do sistema, seguindo o modelo em camadas:

- **Core**
- **Infrastructure**
- **Presentation**

Comando no NuGet:

```powershell
dotnet nuget locals all --clear
dotnet restore
dotnet clean 
dotnet build
```
---

# Core

## Common

📁 `Core/Application/Common/Repositories`

### IEntityDbSet.cs

```csharp
using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Application.Common.Repositories;

public interface IEntityDbSet
{
    public DbSet<Token> Token { get; set; }
    public DbSet<Company> Company { get; set; }
    public DbSet<FileImage> FileImage { get; set; }
    public DbSet<FileDocument> FileDocument { get; set; }

    public DbSet<Category> Category { get; set; }
    // Adicionar aqui a classe !!!

}
```

## Features

📁 `Core/Application/Features/CategoryManager`

### Commands 

- CreateCategory.cs
- DeleteCategory.cs
- UpdateCategory.cs

### Queries 

- GetCategoryList.cs 
- GetCategorySingle.cs

## Entities

📁 `Core\Domain\Entities`

- Category.cs

# Infraestructure

📁 `Infrastructure\Infrastructure\DataAccessManager\EFCore`

### Configurations

- CategoryConfiguration.cs

### Contexts

#### DataContext.cs

```cs
using Application.Common.Repositories;
using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Configurations;
using Infrastructure.SecurityManager.AspNetIdentity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccessManager.EFCore.Contexts;

public class DataContext : IdentityDbContext<ApplicationUser>, IEntityDbSet
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }
    public DbSet<FileImage> FileImage { get; set; }
    public DbSet<FileDocument> FileDocument { get; set; }
    public DbSet<Token> Token { get; set; }
    public DbSet<Company> Company { get; set; }
    // Adicionar aqui a sua classe !!!

    public DbSet<Category> Category { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new FileImageConfiguration());
        modelBuilder.ApplyConfiguration(new FileDocumentConfiguration());
        modelBuilder.ApplyConfiguration(new TokenConfiguration());
        modelBuilder.ApplyConfiguration(new CompanyConfiguration());
        // Adicionar aqui também !!!
    }
}
``` 

---

📁 `Infrastructure\Infrastructure\SecurityManager\`

### NavigationMenu

- NavigationTreeStructure.cs

     ```cs
       public static readonly string JsonStructure = """
    [
        {
            "URL": "#",
            "Name": "Categorias",
            "IsModule": true,
            "Children": [
                {
                    "URL": "/Categories/Category",
                    "Name": "Category",
                    "IsModule": false
                }
            ]
        },
        {
            "URL": "#",
            "Name": "Profiles",
            "IsModule": true,
            "Children": [
                {
                    "URL": "/Profiles/MyProfile",
                    "Name": "My Profile",
                    "IsModule": false
                }
            ]
        },
        {
            "URL": "#",
            "Name": "Settings",
            "IsModule": true,
            "Children": [
                {
                    "URL": "/Companies/MyCompany",
                    "Name": "My Company",
                    "IsModule": false
                }
            ]
        }
        // Nessa parte adicionar o URL, etc da sua classe !!!
    ]
    """;
     ```

---

📁 `Infrastructure\Infrastructure\SeedManager\`

### Demos

- CategorySeeder.cs

```cs
public static IServiceCollection RegisterDemoSeedManager(this IServiceCollection services, IConfiguration configuration)
{
    services.AddScoped<CategorySeeder>();
    // Adicionar aqui o da sua classe !!!
    return services;
}
public static IHost SeedDemoData(this IHost host)
{
    using var scope = host.Services.CreateScope();
    var serviceProvider = scope.ServiceProvider;

    var context = serviceProvider.GetRequiredService<DataContext>();
    if (!context.Category.Any()) 
    {
        var categorySeeder = serviceProvider.GetRequiredService<CategorySeeder>();
        categorySeeder.GenerateDataAsync().Wait();
        // Adicionar aqui o da sua classe !!!

    }
    return host;
}
```
Presentation

### BackEnd

📁 `Presentation/ASPNET/BackEnd/Controllers`

- CategoryController.js

---

### FrontEnd

📁 `Presentation/ASPNET/FrontEnd/Pages/Categories`

- Category.cshtml
- Category.cshtml.js