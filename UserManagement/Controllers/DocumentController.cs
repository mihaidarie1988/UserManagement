using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserManagement.Authorization;

namespace UserManagement.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DocumentController : ControllerBase
    {
        public record Document(int Id, string Title, string Content, string CreatedBy);
        public record WriteDocumentRequest(string Title, string Content);
        public record PatchDocumentRequest(string? Title, string? Content);

        // In-memory document store for demo purposes only
        private static readonly List<Document> Documents =
        [
            new Document(1, "Project Proposal", "Initial project proposal document.", "admin"),
            new Document(2, "Meeting Notes", "Notes from the kick-off meeting.", "admin")
        ];

        private bool IsAdmin() => HttpContext.User.IsInRole(AuthorizationPolicies.AdminRole);
        private string? CurrentUsername() => HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

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
            var nextId = Documents.Count == 0 ? 1 : Documents.Max(d => d.Id) + 1;
            var created = new Document(nextId, document.Title, document.Content, CurrentUsername()!);
            Documents.Add(created);

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
            if (IsAdmin())
                return Ok(Documents);

            var username = CurrentUsername();
            return Ok(Documents.Where(d => d.CreatedBy == username));
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
        public IActionResult GetDocumentById(int id)
        {
            var document = Documents.FirstOrDefault(d => d.Id == id);
            if (document is null)
                return NotFound();

            if (!IsAdmin() && document.CreatedBy != CurrentUsername())
                return Forbid();

            return Ok(document);
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
        public IActionResult UpdateDocument(int id, [FromBody] WriteDocumentRequest document)
        {
            var existingIndex = Documents.FindIndex(d => d.Id == id);
            if (existingIndex < 0)
                return NotFound();

            var existing = Documents[existingIndex];
            if (!IsAdmin() && existing.CreatedBy != CurrentUsername())
                return Forbid();

            Documents[existingIndex] = new Document(id, document.Title, document.Content, existing.CreatedBy);

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
        public IActionResult PatchDocument(int id, [FromBody] PatchDocumentRequest document)
        {
            var existingIndex = Documents.FindIndex(d => d.Id == id);
            if (existingIndex < 0)
                return NotFound();

            var current = Documents[existingIndex];
            if (!IsAdmin() && current.CreatedBy != CurrentUsername())
                return Forbid();

            Documents[existingIndex] = new Document(
                id,
                string.IsNullOrWhiteSpace(document.Title) ? current.Title : document.Title,
                string.IsNullOrWhiteSpace(document.Content) ? current.Content : document.Content,
                current.CreatedBy);

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
        public IActionResult DeleteDocument(int id)
        {
            var existingIndex = Documents.FindIndex(d => d.Id == id);
            if (existingIndex < 0)
                return NotFound();

            if (!IsAdmin() && Documents[existingIndex].CreatedBy != CurrentUsername())
                return Forbid();

            Documents.RemoveAt(existingIndex);

            return NoContent();
        }
    }
}
