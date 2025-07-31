using System.Data.Common;
using System.Reflection;
using System.Text.RegularExpressions;
using AutoCRUD.Models;
using Dapper;

namespace AutoCRUD.Data;

public abstract class Repository<S, C, E, I> : IRepository<E, I>
where S : DbConnection, new()
where C : DbCommand, new()
where E : IEntity<I>
where I : struct 
{
    protected static readonly Regex _securityRegex = new(@"^[a-zA-Z_][a-zA-Z0-9_]*$");

    protected Dictionary<string, (string? sqlInsertUpdate, IEnumerable<PropertyInfo> properties, string fields)>? _propertiesSqlInfos;

    public Repository(string tablename, string keyfieldname, string connectionstring, string? searchcolumnname = null)
    {
        ConnectionString = connectionstring;

        _propertiesSqlInfos ??= new Dictionary<string, (string?, IEnumerable<PropertyInfo>, string)>();

        if (!string.IsNullOrWhiteSpace(tablename) && !_securityRegex.IsMatch(tablename))
            throw new ArgumentException("Invalid table name.", nameof(tablename));

        if (string.IsNullOrWhiteSpace(keyfieldname) || !_securityRegex.IsMatch(keyfieldname))
            throw new ArgumentException("Invalid key field name.", nameof(keyfieldname));

        if (!string.IsNullOrWhiteSpace(searchcolumnname) && !_securityRegex.IsMatch(searchcolumnname))
            throw new ArgumentException("Invalid search column name.", nameof(searchcolumnname));

        if (string.IsNullOrWhiteSpace(searchcolumnname)) searchcolumnname = null;

        TableName = tablename ?? typeof(E).Name;
        keyFieldName = keyfieldname;
        SearchColumnName = searchcolumnname ?? keyfieldname;

        var properties = typeof(E).GetProperties().Where((p) => p.Name.ToLower() != SearchColumnName.ToLower() || p.Name.ToLower() == keyfieldname.ToLower());

        var fields = properties.Select((p) => p.Name);

        _propertiesSqlInfos.Add(TableName, (null, properties, String.Join(',', fields)));

        SqlMapper.AddTypeHandler(new StringArrayTypeHandler());
    }

    public string TableName { get; private set; }

    public string keyFieldName { get; private set; }

    public string SearchColumnName { get; private set; }

    public string ConnectionString { get; set; }

    protected virtual S CreateConnection()
    {
        var conn = new S();
        conn.ConnectionString = ConnectionString;
        return conn;
    }

    public virtual async Task<long> CountAsync()
    {
        using (var connection = CreateConnection())
        {
            await connection.OpenAsync();

            using (var cmd = new C())
            {
                cmd.Connection = connection;
                cmd.CommandText = $"SELECT COUNT({keyFieldName}) FROM {TableName}";

                return (long)(await cmd.ExecuteScalarAsync().ConfigureAwait(false) ?? 0);
            }
        }
    }

    public virtual async Task<bool> DeleteAsync(I id) {
        using (var conn = CreateConnection())
        {
            await conn.OpenAsync();

            string sql = @$"
                Delete from {TableName}
                WHERE {keyFieldName} = @value";

            var affecteds = await conn.ExecuteAsync(sql, new { value = id }).ConfigureAwait(false);

            return affecteds > 0;
        }
    }

    public virtual async Task<E?> FindByFieldAsync(string fieldname, object searchvalue)
    {

        fieldname = fieldname ?? throw new ArgumentException(nameof(fieldname));

        if (!_securityRegex.IsMatch(fieldname)) throw new ArgumentException(nameof(fieldname));

        searchvalue = searchvalue ?? throw new ArgumentException(nameof(searchvalue));

        using (var conn = CreateConnection())
        {
            await conn.OpenAsync();

            string sql = @$"
                SELECT {_propertiesSqlInfos?[TableName].fields}
                FROM {TableName} 
                WHERE {fieldname} = @value";

            return (
                await conn.QueryAsync<E?>(sql, new { value = searchvalue })
                .ConfigureAwait(false)
            ).FirstOrDefault();
        }
    }

    public virtual async Task<E?> FindByFieldAsync(string fieldname, E searchentity)
    {

        var field = _propertiesSqlInfos?[TableName].properties.Single((p) => p.Name.ToLower() == fieldname.ToLower());

        return await FindByFieldAsync(fieldname, field?.GetValue(searchentity) ?? string.Empty);
    }

    public virtual async Task<E?> FindByIDAsync(I id)
    {
        using (var conn = CreateConnection())
        {
            await conn.OpenAsync();

            string sql = @$"
                SELECT {_propertiesSqlInfos?[TableName].fields} 
                FROM {TableName}
                WHERE {keyFieldName} = @value";

            return (
                await conn.QueryAsync<E?>(sql, new { value = id })
                .ConfigureAwait(false)
            ).FirstOrDefault();
        }
    }

    public virtual async Task<bool> InsertAsync(E data)
    {

        if (data is null) return false;

        using (var conn = CreateConnection())
        {
            try
            {
                await conn.OpenAsync();

                var sql = GetSQLInsertUpdateAsync();

                var rowsAffected = await conn.ExecuteAsync(sql, data).ConfigureAwait(false);

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }

    public abstract Task<int> InsertAsync(IEnumerable<E> data);

    public virtual async Task<IEnumerable<E?>> SearchAsync(string? searchterm, int pageNumber = 1, int pageSize = 25)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1, nameof(pageNumber));
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1, nameof(pageSize));

        using (var conn = CreateConnection())
        {
            await conn.OpenAsync();

            int offset = (pageNumber - 1) * pageSize;

            string sql = @$"
                SELECT {_propertiesSqlInfos?[TableName].fields}
                FROM {TableName}                
                {(string.IsNullOrWhiteSpace(searchterm) ? string.Empty : $"WHERE {SearchColumnName} ILIKE '%' || @value || '%'")}
                LIMIT @pageSize OFFSET @offset";

            return await conn.QueryAsync<E?>(sql,
                string.IsNullOrWhiteSpace(searchterm)
                    ? new
                    {
                        pageSize,
                        offset
                    }
                    : new
                    {
                        value = searchterm,
                        pageSize,
                        offset
                    }
            ).ConfigureAwait(false);
        }
    }

    protected abstract string GetSQLInsertUpdateAsync();
}