namespace OrderService.Domain.Repositories;

using OrderService.Domain.Entities;

/// <summary>
/// Contrat pour la persistance des commandes.
/// Définit les opérations d'accès aux données sans spécifier comment.
/// Interface dans Domain, implémentation dans Infrastructure.
/// </summary>
public interface IOrderRepository
{
    /// <summary>Récupère une commande par son identifiant</summary>
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>Récupère toutes les commandes d'un client</summary>
    Task<List<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>Ajoute une nouvelle commande</summary>
    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>Met à jour une commande existante</summary>
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>Supprime une commande</summary>
    Task DeleteAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>Sauvegarde tous les changements en attente (Unit of Work)</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
