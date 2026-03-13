using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserManagement.Authorization;
using UserManagement.Models;
using UserManagement.Services;

namespace UserManagement.Controllers;

[ApiController]
[Route("[controller]")]
public class DocumentController(DocumentStore store) : ControllerBase
{
    /// <summary>
    /// Creates a new document.
    /// </summary>
    /// <param name="document">The document to create.</param>
    /// <response code="201">Document created.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT.</response>
    [HttpPost]
    [CreateAccess]
    public IActionResult CreateDocument([FromBody] WriteDocumentRequest document)
    {
        var created = store.Add(new Document(
            store.NextId(),
            document.Title,
            document.Content,
            HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!));

        return CreatedAtAction(nameof(GetDocumentById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Gets all documents visible to the caller.
    /// </summary>
    /// <response code="200">Returns the list of documents.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT.</response>
    [HttpGet]
    [ReadAccess]
    public IActionResult GetDocuments()
    {
        var docs = HttpContext.User.IsInRole(AuthorizationPolicies.AdminRole)
            ? store.GetAll()
            : store.GetByOwner(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        return Ok(docs);
    }

    /// <summary>
    /// Gets a document by identifier.
    /// </summary>
    /// <param name="id">Document identifier.</param>
    /// <response code="200">Returns the document.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT.</response>
    /// <response code="403">Forbidden - not the owner of this document.</response>
    /// <response code="404">Document not found.</response>
    [HttpGet("{id:int}")]
    [ReadAccess]
    [DocumentOwnership]
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
    /// <response code="401">Unauthorized - invalid or missing JWT.</response>
    /// <response code="403">Forbidden - not the owner of this document.</response>
    /// <response code="404">Document not found.</response>
    [HttpPut("{id:int}")]
    [UpdateAccess]
    [DocumentOwnership]
    public IActionResult UpdateDocument(int id, [FromBody] WriteDocumentRequest document)
    {
        var index = store.FindIndex(id);
        if (index < 0)
            return NotFound();

        var existing = store.GetAll()[index];
        store.Update(index, new Document(id, document.Title, document.Content, existing.CreatedBy));

        return NoContent();
    }

    /// <summary>
    /// Partially updates a document.
    /// </summary>
    /// <param name="id">Document identifier.</param>
    /// <param name="document">The document fields to update.</param>
    /// <response code="204">Document updated.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT.</response>
    /// <response code="403">Forbidden - not the owner of this document.</response>
    /// <response code="404">Document not found.</response>
    [HttpPatch("{id:int}")]
    [UpdateAccess]
    [DocumentOwnership]
    public IActionResult PatchDocument(int id, [FromBody] PatchDocumentRequest document)
    {
        var index = store.FindIndex(id);
        if (index < 0)
            return NotFound();

        var current = store.GetAll()[index];
        store.Update(index, new Document(
            id,
            string.IsNullOrWhiteSpace(document.Title) ? current.Title : document.Title,
            string.IsNullOrWhiteSpace(document.Content) ? current.Content : document.Content,
            current.CreatedBy));

        return NoContent();
    }

    /// <summary>
    /// Deletes a document.
    /// </summary>
    /// <param name="id">Document identifier.</param>
    /// <response code="204">Document deleted.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT.</response>
    /// <response code="403">Forbidden - not the owner of this document.</response>
    /// <response code="404">Document not found.</response>
    [HttpDelete("{id:int}")]
    [DeleteAccess]
    [DocumentOwnership]
    public IActionResult DeleteDocument(int id)
    {
        var index = store.FindIndex(id);
        if (index < 0)
            return NotFound();

        store.Remove(index);
        return NoContent();
    }
}
