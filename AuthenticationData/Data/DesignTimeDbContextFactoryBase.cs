using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthenticationData.Data;

public abstract class DesignTimeDbContextFactoryBase<TContext> :
    IDesignTimeDbContextFactory<TContext> where TContext : AppDbContext
{
    protected abstract TContext CreateNewInstance(DbContextOptions<TContext> options);

    public TContext CreateDbContext(string[] args)
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        string basePath = $"{Directory.GetParent(currentDirectory).FullName}/AuthenticationApi";
        var envVariable = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Console.WriteLine($"Variable de entorno: {envVariable}");
        return Create(basePath, envVariable);
    }

    private TContext Create(string basePath, string environment)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables();

        var configuration = builder.Build();

        var connectionstr = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionstr))
        {
            throw new InvalidOperationException(
                "Could not find a connection string named 'DefaultConnection'.");
        }
        else
        {
            return CreateWithConnectionString(connectionstr);
        }
    }

    private TContext CreateWithConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException(
                $"{nameof(connectionString)} is null or empty.",
             nameof(connectionString));
        }

        var optionsBuilder = new DbContextOptionsBuilder<TContext>();

        optionsBuilder.UseSqlServer(connectionString);

        DbContextOptions<TContext> options = optionsBuilder.Options;

        return CreateNewInstance(options);
    }  
}
