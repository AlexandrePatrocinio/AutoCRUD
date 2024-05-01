using AutoCRUD.Models;

namespace AutoCRUD.Data;

public interface IRepository<E> where E : IEntity {

    string TableName { get; }

    string keyFieldName { get; }

    string SearchColumnName { get; }

    string ConnectionString { get; set; }

    Task<long> CountAsync();

    Task<E?> FindByIDAsync(Guid id);

    Task<IEnumerable<E?>> SearchAsync(string valeur);

    Task<E?> FindByFieldAsync(string fieldname, object value);

    Task<E?> FindByFieldAsync(string fieldname, E value);

    Task<bool> InsertAsync(E data);

    Task<int> InsertAsync(IEnumerable<E> data);

    Task<bool> DeleteAsync(Guid id);

}
