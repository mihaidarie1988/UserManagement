namespace DocumentManagement.Workshop.Models;

public record Document(int Id, string Title, string Content);
public record WriteDocumentRequest(string Title, string Content);
public record PatchDocumentRequest(string? Title, string? Content);
