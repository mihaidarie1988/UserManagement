namespace DocumentManagement.Workshop.Controllers;

using Microsoft.AspNetCore.Mvc;
using Models;
using Services;

[ApiController]
[Route("[controller]")]
public class DocumentController(DocumentStore store) : ControllerBase
{
    /// <summary>
    /// Creates a new document.
    /// </summary>
    /// <param name="document">The document to create.</param>
    /// <response code="201">Document created.</response>
    [HttpPost]
    public IActionResult CreateDocument([FromBody] WriteDocumentRequest document)
    {
        var created = store.Add(new Document(store.NextId(), document.Title, document.Content));
        return CreatedAtAction(nameof(GetDocumentById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Gets all documents.
    /// </summary>
    /// <response code="200">Returns the list of documents.</response>
    [HttpGet]
    public IActionResult GetDocuments() => Ok(store.GetAll());

    /// <summary>
    /// Gets a document by identifier.
    /// </summary>
    /// <param name="id">Document identifier.</param>
    /// <response code="200">Returns the document.</response>
    /// <response code="404">Document not found.</response>
    [HttpGet("{id:int}")]
    public IActionResult GetDocumentById(int id)
    {
        var document = store.FindById(id);
        return document is null ? NotFound() : Ok(document);
    }

    /// <summary>
    /// Replaces an existing document.
    /// </summary>
    /// <param name="id">Document identifier.</param>
    /// <param name="document">The updated document.</param>
    /// <response code="204">Document updated.</response>
    /// <response code="404">Document not found.</response>
    [HttpPut("{id:int}")]
    public IActionResult UpdateDocument(int id, [FromBody] WriteDocumentRequest document)
    {
        var index = store.FindIndex(id);
        if (index < 0)
            return NotFound();

        store.Update(index, new Document(id, document.Title, document.Content));
        return NoContent();
    }

    /// <summary>
    /// Partially updates a document.
    /// </summary>
    /// <param name="id">Document identifier.</param>
    /// <param name="document">The document fields to update.</param>
    /// <response code="204">Document updated.</response>
    /// <response code="404">Document not found.</response>
    [HttpPatch("{id:int}")]
    public IActionResult PatchDocument(int id, [FromBody] PatchDocumentRequest document)
    {
        var index = store.FindIndex(id);
        if (index < 0)
            return NotFound();

        var current = store.GetAll()[index];
        store.Update(index, new Document(
            id,
            string.IsNullOrWhiteSpace(document.Title) ? current.Title : document.Title,
            string.IsNullOrWhiteSpace(document.Content) ? current.Content : document.Content));

        return NoContent();
    }

    /// <summary>
    /// Deletes a document.
    /// </summary>
    /// <param name="id">Document identifier.</param>
    /// <response code="204">Document deleted.</response>
    /// <response code="404">Document not found.</response>
    [HttpDelete("{id:int}")]
    public IActionResult DeleteDocument(int id)
    {
        var index = store.FindIndex(id);
        if (index < 0)
            return NotFound();

        store.Remove(index);
        return NoContent();
    }
}
