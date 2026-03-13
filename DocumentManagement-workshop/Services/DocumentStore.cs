namespace DocumentManagement.Workshop.Services;

using Models;

public sealed class DocumentStore
{
    private readonly List<Document> _documents =
    [
        new Document(1, "Project Proposal", "Initial project proposal document."),
        new Document(2, "Meeting Notes", "Notes from the kick-off meeting."),
        new Document(3, "Budget Overview", "Q1 budget overview and projections."),
        new Document(4, "Technical Specification", "System architecture and design notes.")
    ];

    private int _nextId = 5;

    public IReadOnlyList<Document> GetAll() => _documents;

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

    public int NextId() => _nextId++;
}
