using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Net.Http.Headers;
using TinyRepository.Ef;
using TinyRepository.Extensions;
using TinyRepository.Sample.DTOs;
using TinyRepository.Sample.Entities;
using TinyRepository.Sample.Services;

namespace TinyRepository.Sample;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Alternative way to register DbContext: builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());
        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("SQLConnection"), sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);

                // Setting the SQL Server database compatibility level to 2025
                sqlOptions.UseCompatibilityLevel(170);
            });

            // Configure logging of executed SQL queries
            options.LogTo(Console.WriteLine, [RelationalEventId.CommandExecuted]);

            // Enable sensitive data logging (useful for debugging, but use with caution in production)
            options.EnableSensitiveDataLogging();

            // Ignore the warning related to pending model changes, but not disponable in EF Core 8
            //options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        //builder.Services.AddAttributeWhitelistScan(typeof(SampleEntity).Assembly);
        builder.Services.AddRepositoryPattern<AppDbContext>();
        // scan attributi nell'assembly di dominio con maxDepth = 3
        builder.Services.AddAttributeWhitelistScan(opt => opt.MaxDepth = 3, typeof(SampleEntity).Assembly);

        builder.Services.AddTransient<ISampleService, SampleService>();
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyHeader()
                    .AllowAnyMethod()
                    .SetIsOriginAllowed(_ => true) // Allows requests from any origin
                    .AllowCredentials().WithExposedHeaders(HeaderNames.ContentDisposition); // Exposes the Content-Disposition header
            });
        });

        var app = builder.Build();
        // Configure the HTTP request pipeline.

        // If behind a proxy, uncomment and configure the KnownProxies list
        //app.UseForwardedHeaders(new()
        //{
        //    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        //    KnownProxies = { }
        //});

        //app.UseHttpsRedirection(); // Uncomment if HTTPS redirection is needed

        app.UseSwagger();
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", $"{app.Environment.ApplicationName} v1"));

        app.UseRouting();
        app.UseCors();

        var exampleApi = app.MapGroup("/api/example")
            .WithDescription("Example APIs for TinyRepository")
            .WithTags("Example APIs");

        exampleApi.MapPost("/filter", async Task<Results<Ok<SampleEntity>, NotFound>> (ISampleService sampleService, EntityRequest request, HttpContext httpContext) =>
        {
            var response = await sampleService.GetByIdAsync(request.Id, request.Include, httpContext.RequestAborted);

            return response is not null ? TypedResults.Ok(response) : TypedResults.NotFound();
        })
        .WithName("GetSampleEntityById")
        .WithDescription("Gets a SampleEntity by its ID with optional related data inclusion.")
        .Produces<Ok<SampleEntity>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        exampleApi.MapPost("/create", async Task<IResult> (ISampleService sampleService, SampleEntity item, HttpContext httpContext) =>
        {
            if (item.Name is null)
            {
                return TypedResults.Problem("Name cannot be null", statusCode: StatusCodes.Status400BadRequest);
            }

            var result = await sampleService.CreateAsync(item, httpContext.RequestAborted);

            return TypedResults.Created($"/api/example/{result.Id}", result);
        })
        .WithName("CreateSampleEntity")
        .WithDescription("Creates a new SampleEntity.")
        .Produces<Created<SampleEntity>>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        app.Run();
    }
}