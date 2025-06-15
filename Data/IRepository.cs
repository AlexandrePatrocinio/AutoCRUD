using AutoCRUD.Models;

namespace AutoCRUD.Data;

public interface IRepository<E, I> 
where E : IEntity<I> 
where I : struct {

    string TableName { get; }

    string keyFieldName { get; }

    string SearchColumnName { get; }

    string ConnectionString { get; set; }

    Task<long> CountAsync();

    Task<E?> FindByIDAsync(I id);

    Task<IEnumerable<E?>> SearchAsync(string? valeur, int pageNumber, int pageSize);

    Task<E?> FindByFieldAsync(string fieldname, object value);

    Task<E?> FindByFieldAsync(string fieldname, E value);

    Task<bool> InsertAsync(E data);

    Task<int> InsertAsync(IEnumerable<E> data);

    Task<bool> DeleteAsync(I id);

}
