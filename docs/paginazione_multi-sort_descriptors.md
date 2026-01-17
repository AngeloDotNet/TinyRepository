Esempio: Paginazione con multi-sort descriptors

```csharp
var descriptors = new List<SortDescriptor> {
    SortDescriptor.Desc("Author.LastName"),
    SortDescriptor.Asc("PublishedAt")
};

var page = await _repo.GetPagedAsync(pageNumber: 1, pageSize: 20, sortDescriptors: descriptors, filter: a => a.IsPublished, asNoTracking: true, includes: a => a.Author);
```