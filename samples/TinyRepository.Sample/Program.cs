using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Net.Http.Headers;
using TinyRepository.DTOs;
using TinyRepository.Ef;
using TinyRepository.Extensions;
using TinyRepository.Filters;
using TinyRepository.Interfaces;
using TinyRepository.Metadata.Interfaces;
using TinyRepository.Paging;
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

        builder.Services.AddRepositoryPattern<AppDbContext>();

        builder.Services.AddAttributeWhitelistScan(opt => opt.MaxDepth = 4, typeof(Article).Assembly);
        builder.Services.AddMetadataService(config => { /* leave MaxDepth null to reuse AddAttributeWhitelistScan value */ }, typeof(Article).Assembly);

        builder.Services.AddTransient<ISampleService, SampleService>();
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new() { Title = builder.Environment.ApplicationName, Version = "v1" });
            options.OperationFilter<MetadataOperationFilter>();
        });

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

        #region "Sample API"

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

        #endregion

        #region "Article API"

        var articleApi = app.MapGroup("/api/articles")
            .WithDescription("Article APIs for TinyRepository")
            .WithTags("Article APIs");

        // endpoint demo con alias e include parsing
        app.MapGet("/articles", async (IRepository<Article, int> repo, HttpRequest req) =>
        {
            // include param: comma separated (can be alias)
            var includeQuery = req.Query["include"].ToString();
            var includePaths = string.IsNullOrWhiteSpace(includeQuery) ? [] : includeQuery.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            var page = int.TryParse(req.Query["page"], out var p) ? Math.Max(1, p) : 1;
            var pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 10;

            var useSplit = bool.TryParse(req.Query["split"], out var s) && s;

            // sort param: alias or property, direction param "asc" or "desc"
            var sort = req.Query["sort"].ToString();
            var dir = req.Query["dir"].ToString() ?? "asc";
            var descending = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase);

            PagedResult<Article> pageResult;
            if (!string.IsNullOrWhiteSpace(sort))
            {
                pageResult = await repo.GetPagedTwoStageAsync(page, pageSize,
                    orderByProperty: sort,
                    descending: descending,
                    filter: null,
                    asNoTracking: true,
                    cancellationToken: default,
                    useAsSplitQuery: useSplit,
                    includePaths: includePaths);
            }
            else
            {
                pageResult = await repo.GetPagedTwoStageAsync(page, pageSize,
                    sortDescriptors: [Sorting.SortDescriptor.Asc("Title")],
                    filter: null,
                    asNoTracking: true,
                    cancellationToken: default,
                    useAsSplitQuery: useSplit,
                    includePaths: includePaths);
            }

            return Results.Ok(new { pageResult.TotalCount, pageResult.PageNumber, pageResult.PageSize, pageResult.Items });
        });

        #endregion

        #region "Metadata API"

        var metadataApi = app.MapGroup("/api/metadata")
            .WithDescription("Metadata APIs for TinyRepository")
            .WithTags("Metadata APIs");

        metadataApi.MapGet("/entities/{name}/whitelist", async (string name, IMetadataService metadata) =>
        {
            var dto = await metadata.GetEntityWhitelistAsync(name);
            if (dto == null)
            {
                return Results.NotFound(new { message = $"Entity '{name}' not found in scanned assemblies." });
            }

            return Results.Ok(dto);
        })
        .WithName("GetEntityWhitelist")
        .Produces<EntityWhitelistDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/metadata/entities", async (IMetadataService metadata) =>
        {
            var names = await metadata.GetAllEntityNamesAsync();
            return Results.Ok(names);
        })
        .WithName("GetEntities")
        .Produces<string[]>(StatusCodes.Status200OK);

        app.MapGet("/metadata/entities/{name}/whitelist", async (string name, IMetadataService metadata) =>
        {
            var dto = await metadata.GetEntityWhitelistAsync(name);

            if (dto == null)
            {
                return Results.NotFound(new { message = $"Entity '{name}' not found in scanned assemblies." });
            }

            return Results.Ok(dto);
        })
        .WithName("GetEntityWhitelist")
        .Produces<EntityWhitelistDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        #endregion

        app.Run();
    }
}