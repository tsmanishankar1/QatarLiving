using Dapr.Actors;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.ISubscriptionService
{
    /// <summary>
    /// Interface for UserManagement Actor that handles user role management operations
    /// </summary>
    public interface IUserManagementActor : IActor
    {
        /// <summary>
        /// Changes a user's role to the specified new role
        /// </summary>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="newRole">The name of the new role to assign</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if role change was successful, false otherwise</returns>
        Task<bool> ChangeUserRoleAsync(Guid userId, string newRole, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current roles assigned to a user
        /// </summary>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of role names assigned to the user</returns>
        Task<string[]> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a user to a specific role without removing existing roles
        /// </summary>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="roleName">The name of the role to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if user was successfully added to role, false otherwise</returns>
        Task<bool> AddUserToRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a user from a specific role
        /// </summary>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="roleName">The name of the role to remove</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if user was successfully removed from role, false otherwise</returns>
        Task<bool> RemoveUserFromRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a user is in a specific role
        /// </summary>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="roleName">The name of the role to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if user is in the specified role, false otherwise</returns>
        Task<bool> IsUserInRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Handles expired subscription by changing user role from Subscriber to User
        /// </summary>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if subscription expiry was handled successfully, false otherwise</returns>
        Task<bool> HandleSubscriptionExpiryAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}