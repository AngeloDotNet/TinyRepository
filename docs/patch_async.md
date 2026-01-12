#Esempio: PatchAsync

```csharp
var success = await _repo.PatchAsync(5, entity => {
    entity.Name = "Nuovo nome";
});
if (success) await uow.SaveChangesAsync();
```