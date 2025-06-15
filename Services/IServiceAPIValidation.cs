using AutoCRUD.Data;
using AutoCRUD.Models;

namespace AutoCRUD.Services;

public interface IServiceAutoCRUDValidation<E, I>
where E : IEntity<I>
where I : struct
{
    Task<(bool Valid, IEntity<I>? Entity)> IsValidEntityAsync(IEntity<I> Entity, IRepository<E, I> repository);

    (bool Valid, I Id) isValidID(string id, IRepository<E, I>pository);

    (bool Valid, string SearchTerm) isSearchTermValid(string t, IRepository<E, I>pository);

    Task<(bool Valid, IEntity<I>? Entity)> isPostValidAsync(IEntity<I> Entity, IRepository<E, I>pository);

    Task<(bool Valid, IEntity<I>? Entity)> isGetValidAsync(IEntity<I> Entity, IRepository<E, I>pository);

    Task<(bool Valid, IEntity<I>? Entity)> isGetValidAsync(I Id, IRepository<E, I>pository);

    Task<(bool Valid, IEntity<I>? Entity)> isPutValidAsync(IEntity<I> Entity, IRepository<E, I>pository);

    Task<(bool Valid, IEntity<I>? Entity)> isDeleteValidAsync(IEntity<I> Entity, IRepository<E, I>pository);

    Task<(bool Valid, IEntity<I>? Entity)> isDeleteValidAsync(I Id, IRepository<E, I>pository);

}
