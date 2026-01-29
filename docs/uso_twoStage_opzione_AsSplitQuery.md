Esempio: Esempio d'uso del two stage con opzione AsSplitQuery:

```csharp
var page = await _repo.GetPagedTwoStageAsync(
    pageNumber: 1,
    pageSize: 20,
    sortDescriptors: new[] { SortDescriptor.Desc("Author.LastName"), SortDescriptor.Asc("PublishedAt") },
    filter: e => e.IsPublished,
    asNoTracking: true,
    cancellationToken: CancellationToken.None,
    useAsSplitQuery: true,
    includePaths: new[] { "Author", "Author.Books.Publisher" });
```