Esempio: uso di paging two-stage:

```csharp
var page = await _repo.GetPagedTwoStageAsync(
    pageNumber: 1,
    pageSize: 10,
    sortDescriptors: new[] { SortDescriptor.Desc("Author.LastName"), SortDescriptor.Asc("PublishedAt") },
    filter: e => e.IsPublished,
    includePaths: new[] { "Author", "Author.Books" });
```