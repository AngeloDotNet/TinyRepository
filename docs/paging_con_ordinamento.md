# Esempio: paging con ordinamento

```csharp
// repository iniettato: IRepository<SampleEntity,int> _repo
var page = 1;
var pageSize = 10;

var paged = await _repo.GetPagedAsync(
    page,
    pageSize,
    orderBy: q => q.OrderBy(e => e.Name),   // oppure q => q.OrderByDescending(...)
    filter: e => e.Name != null && e.Name.Contains("abc"),
    asNoTracking: true);
```