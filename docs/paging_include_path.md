Esempio: Paging con include path

```csharp
var page = await _repo.GetPagedAsync(
    pageNumber: 1,
    pageSize: 10,
    sortDescriptors: new[] { SortDescriptor.Desc("Author.LastName"), SortDescriptor.Asc("PublishedAt") },
    filter: e => e.IsPublished,
    asNoTracking: true,
    includePaths: new[] { "Author", "Author.Books", "Author.Books.Publisher" });
```