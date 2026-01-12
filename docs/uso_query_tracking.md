Esempio: uso di Query con tracking (se vuoi modificare dopo):

```csharp
var tracked = _repo.Query(asNoTracking: false, e => e.RelatedEntities)
    .FirstOrDefault(e => e.Id == id);
if (tracked != null) {
    tracked.SomeProp = "x";
    await _uow.SaveChangesAsync();
}
```