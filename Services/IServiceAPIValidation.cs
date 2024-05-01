using AutoCRUD.Data;
using AutoCRUD.Models;

namespace AutoCRUD.Services;

public interface IServiceAutoCRUDValidation<E> where E : IEntity
{
    Task<(bool Valid, IEntity? Entity)> IsValidEntityAsync(IEntity Entity, IRepository<E> repository);

    (bool Valid, Guid Guid) isValidID(string id, IRepository<E> repository);

    (bool Valid, string SearchTerm) isSearchTermValid(string t, IRepository<E> repository);

    Task<(bool Valid, IEntity? Entity)> isPostValidAsync(IEntity Entity, IRepository<E> repository);

    Task<(bool Valid, IEntity? Entity)> isGetValidAsync(IEntity Entity, IRepository<E> repository);

    Task<(bool Valid, IEntity? Entity)> isGetValidAsync(Guid guid, IRepository<E> repository);

    Task<(bool Valid, IEntity? Entity)> isPutValidAsync(IEntity Entity, IRepository<E> repository);

    Task<(bool Valid, IEntity? Entity)> isDeleteValidAsync(IEntity Entity, IRepository<E> repository);

    Task<(bool Valid, IEntity? Entity)> isDeleteValidAsync(Guid guid, IRepository<E> repository);

}
