namespace DocumentManagement.Models;

public record Document(int Id, string Title, string Content, string CreatedBy);
public record WriteDocumentRequest(string Title, string Content);
public record PatchDocumentRequest(string? Title, string? Content);
