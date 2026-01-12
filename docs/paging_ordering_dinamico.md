#Esempio: paging con ordering dinamico (string property):

```csharp
var paged = await _repo.GetPagedAsync(
    pageNumber: 1,
    pageSize: 20,
    orderByProperty: "Author.LastName",
    descending: false,
    filter: e => e.IsPublished,
    asNoTracking: true,
    includes: e => e.Author, e => e.Tags);
```