using UserManagement.Models;

namespace UserManagement.Services;

public sealed class DocumentStore
{
    private readonly List<Document> _documents =
    [
        new Document(1, "Project Proposal", "Initial project proposal document.", "admin"),
        new Document(2, "Meeting Notes", "Notes from the kick-off meeting.", "admin")
    ];

    public IReadOnlyList<Document> GetAll() => _documents;

    public IEnumerable<Document> GetByOwner(string username) =>
        _documents.Where(d => d.CreatedBy == username);

    public Document? FindById(int id) =>
        _documents.FirstOrDefault(d => d.Id == id);

    public int FindIndex(int id) =>
        _documents.FindIndex(d => d.Id == id);

    public Document Add(Document document)
    {
        _documents.Add(document);
        return document;
    }

    public void Update(int index, Document document) => _documents[index] = document;

    public void Remove(int index) => _documents.RemoveAt(index);

    public int NextId() => _documents.Count == 0 ? 1 : _documents.Max(d => d.Id) + 1;
}
