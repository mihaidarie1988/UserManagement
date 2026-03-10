using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UserManagement.Authorization;

namespace UserManagement.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserManagementController : ControllerBase
    {
        // Simple in-memory user model
        public new record User(int Id, string Name, string Email);
        public record WriteUserRequest(string Name, string Email);
        public record PatchUserRequest(string? Name, string? Email);

        // In-memory user store for demo purposes only
        private static readonly List<User> Users =
        [
            new User(1, "Alice", "alice@example.com"),
            new User(2, "Bob", "bob@example.com")
        ];

        /// <summary>
        /// Creates a new user.
        /// </summary>
        /// <param name="user">The user to create.</param>
        /// <response code="201">User created.</response>
        /// <response code="401">Unauthorized - invalid or missing JWT.</response>
        [HttpPost("users")]
        [Authorize(Policy = AuthorizationPolicies.CreatePolicy)]
        public IActionResult CreateUser([FromBody] WriteUserRequest user)
        {
            var nextId = Users.Count == 0 ? 1 : Users.Max(u => u.Id) + 1;
            var created = new User(nextId, user.Name, user.Email);
            Users.Add(created);

            return CreatedAtAction(nameof(GetUserById), new { id = created.Id }, created);
        }

        /// <summary>
        /// Gets all users.
        /// </summary>
        /// <response code="200">Returns the list of users.</response>
        /// <response code="401">Unauthorized - invalid or missing JWT.</response>
        [HttpGet("users")]
        [Authorize(Policy = AuthorizationPolicies.ReadPolicy)]
        public IActionResult GetUsers()
        {
            return Ok(Users);
        }

        /// <summary>
        /// Gets a user by identifier.
        /// </summary>
        /// <param name="id">User identifier.</param>
        /// <response code="200">Returns the user.</response>
        /// <response code="401">Unauthorized - invalid or missing JWT.</response>
        /// <response code="404">User not found.</response>
        [HttpGet("users/{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.ReadPolicy)]
        public IActionResult GetUserById(int id)
        {
            var user = Users.FirstOrDefault(u => u.Id == id);
            if (user is null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        /// <summary>
        /// Replaces an existing user.
        /// </summary>
        /// <param name="id">User identifier.</param>
        /// <param name="user">The updated user.</param>
        /// <response code="204">User updated.</response>
        /// <response code="401">Unauthorized - invalid or missing JWT.</response>
        /// <response code="404">User not found.</response>
        [HttpPut("users/{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.UpdatePolicy)]
        public IActionResult UpdateUser(int id, [FromBody] WriteUserRequest user)
        {
            var existingIndex = Users.FindIndex(u => u.Id == id);
            if (existingIndex < 0)
            {
                return NotFound();
            }

            var updated = new User(id, user.Name, user.Email);
            Users[existingIndex] = updated;

            return NoContent();
        }

        /// <summary>
        /// Partially updates a user.
        /// </summary>
        /// <param name="id">User identifier.</param>
        /// <param name="user">The user fields to update.</param>
        /// <response code="204">User updated.</response>
        /// <response code="401">Unauthorized - invalid or missing JWT.</response>
        /// <response code="404">User not found.</response>
        [HttpPatch("users/{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.UpdatePolicy)]
        public IActionResult PatchUser(int id, [FromBody] PatchUserRequest user)
        {
            var existingIndex = Users.FindIndex(u => u.Id == id);
            if (existingIndex < 0)
            {
                return NotFound();
            }

            var current = Users[existingIndex];
            var patched = new User(
                id,
                string.IsNullOrWhiteSpace(user.Name) ? current.Name : user.Name,
                string.IsNullOrWhiteSpace(user.Email) ? current.Email : user.Email);

            Users[existingIndex] = patched;

            return NoContent();
        }

        /// <summary>
        /// Deletes a user.
        /// </summary>
        /// <param name="id">User identifier.</param>
        /// <response code="204">User deleted.</response>
        /// <response code="401">Unauthorized - invalid or missing JWT.</response>
        /// <response code="404">User not found.</response>
        [HttpDelete("users/{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.DeletePolicy)]
        public IActionResult DeleteUser(int id)
        {
            var existingIndex = Users.FindIndex(u => u.Id == id);
            if (existingIndex < 0)
            {
                return NotFound();
            }

            Users.RemoveAt(existingIndex);

            return NoContent();
        }
    }
}
