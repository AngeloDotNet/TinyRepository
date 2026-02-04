namespace TinyRepository.Provider.Interfaces;

public interface IAliasProvider<T>
{
    /// <summary>
    /// Ritorna una mappa alias -> actual path (es. "authorName" => "Author.LastName").
    /// La mappa può essere vuota se non sono stati definiti alias.
    /// </summary>
    IDictionary<string, string> GetAliasMap();
}