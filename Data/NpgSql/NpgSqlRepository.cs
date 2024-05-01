using Npgsql;
using Dapper;
using System.Reflection;
using System.Text;
using AutoCRUD.Models;

namespace AutoCRUD.Data.NpgSql;

public class NpgSqlRepository<E> : IRepository<E> where E : IEntity {

    private static Dictionary<string, (string? sql, IEnumerable<PropertyInfo> properties, string fields)>? _propertiesSqlInfos;

    public NpgSqlRepository(string tablename, string keyfieldname, string connectionstring, string? searchcolumnname = null)
    {
        ConnectionString = connectionstring;
        
        _propertiesSqlInfos ??= new Dictionary<string, (string?, IEnumerable<PropertyInfo>, string)>();
        
        keyFieldName = keyfieldname ?? throw new ArgumentException(nameof(keyfieldname));
        if (keyfieldname.Contains('\'')) throw new ArgumentException(nameof(keyfieldname));
        
        if (tablename is not null && tablename.Contains('\'')) throw new ArgumentException(nameof(tablename));

        if(string.IsNullOrWhiteSpace(searchcolumnname)) searchcolumnname = null;            

        SearchColumnName = searchcolumnname ?? keyfieldname;

        TableName = tablename ?? typeof(E).Name;

        var properties = typeof(E).GetProperties().Where((p)=> p.Name.ToLower() != SearchColumnName.ToLower() || p.Name.ToLower() == keyfieldname.ToLower());

        var arrayStringfields = properties.Where((p)=> p.PropertyType == typeof(String[]));

        var fields = properties.Select((p)=> 
            !arrayStringfields.Contains(p) ? 
            p.Name : 
            $"string_to_array(REPLACE(REPLACE({p.Name}, '{{',''), '}}',''), ',') AS {p.Name}"
        );

        _propertiesSqlInfos.Add(TableName, (null, properties, String.Join(',', fields)));
    }

    public string TableName { get; private set; }

    public string keyFieldName { get; private set; }

    public string SearchColumnName { get; private set; }

    public string ConnectionString { get; set; }

    public async Task<long> CountAsync() {
        using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString))
        {
            await connection.OpenAsync();
            
            using (var cmd = new NpgsqlCommand($"SELECT COUNT(id) FROM {TableName}", connection))
            {
                return (long)(await cmd.ExecuteScalarAsync() ?? 0);
            }
        }
    }

    public async Task<E?> FindByIDAsync(Guid id) {
        using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
        {
            await conn.OpenAsync();

            string sql = @$"
                SELECT {_propertiesSqlInfos?[TableName].fields} 
                FROM {TableName}
                WHERE {keyFieldName} = @value";

            return (await conn.QueryAsync<E?>(sql, new { value = id })).FirstOrDefault();
        }
    }

    public async Task<IEnumerable<E?>> SearchAsync(string searchterm) {

        searchterm = searchterm ?? throw new ArgumentException(nameof(searchterm));

        using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
        {
            await conn.OpenAsync();

            string sql = @$"
                SELECT {_propertiesSqlInfos?[TableName].fields}
                FROM {TableName} 
                WHERE {SearchColumnName} like '%' || @value || '%'";

            return await conn.QueryAsync<E?>(sql, new { value = searchterm });
        }
    }

    public async Task<E?> FindByFieldAsync(string fieldname, object searchvalue) {

        fieldname = fieldname ?? throw new ArgumentException(nameof(fieldname));

        if(fieldname.Contains('\'')) throw new ArgumentException(nameof(fieldname));

        searchvalue = searchvalue ?? throw new ArgumentException(nameof(searchvalue));

        using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
        {
            await conn.OpenAsync();

            string sql = @$"
                SELECT {_propertiesSqlInfos?[TableName].fields}
                FROM {TableName} 
                WHERE {fieldname} = @value";
                
            return (await conn.QueryAsync<E?>(sql, new { value = searchvalue })).FirstOrDefault();
        }
    }

    public async Task<E?> FindByFieldAsync(string fieldname, E searchentity) {

        var field = _propertiesSqlInfos?[TableName].properties.Single((p)=> p.Name.ToLower() == fieldname.ToLower());

        return await FindByFieldAsync(fieldname, field?.GetValue(searchentity) ?? string.Empty);
    }

    public async Task<bool> InsertAsync(E data)
    {

        if (data is null) return false;

        using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString))
        {
            try
            {
                await connection.OpenAsync();

                var rowsAffected = await connection.ExecuteAsync(GetSQLAsync(), data).ConfigureAwait(false);

                return rowsAffected > 0;
            }
            catch (PostgresException ex) {
                Console.WriteLine(ex.Message + " " + ex.Detail);
                return false;                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
    
    public async Task<int> InsertAsync(IEnumerable<E> data)
    {
        data = data ?? throw new ArgumentNullException(nameof(data));

        using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString)) {

            await connection.OpenAsync();

            var bulkCopy = new NpgSqlBulkCopy(connection);

            bulkCopy.DestinationTableName = TableName;

            bulkCopy.WriteToServerAsync(data);                 

        }

        return data.Count();
    }

    public async Task<bool> DeleteAsync(Guid id) {
        using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
        {
            await conn.OpenAsync();

            string sql = @$"
                Delete from {TableName}
                WHERE {keyFieldName} = @value";

            var affecteds = await conn.ExecuteAsync(sql, new { value = id });

            return affecteds > 0;
        }
    }

    private string GetSQLAsync() {
        if (_propertiesSqlInfos is not null && string.IsNullOrWhiteSpace(_propertiesSqlInfos[TableName].sql)) {
            const string chartemp1 = " = EXCLUDED.";    

            if (string.IsNullOrWhiteSpace(TableName)) throw new NpgsqlException($"Table {TableName} not found.");

            var fields = String.Join(',', _propertiesSqlInfos[TableName].properties.Select((p)=> p.Name));
            var valuesupdate = String.Join(',', _propertiesSqlInfos[TableName].properties.Select((p)=> p.Name + chartemp1 + p.Name));

            var sBInsertUpdate = new StringBuilder(@$"
                INSERT INTO {TableName}({fields})
                    VALUES(@{fields.Replace(",",",@")})
                ON CONFLICT ({keyFieldName}) DO 
                    UPDATE SET {valuesupdate}", 
                1152
            );

            if (_propertiesSqlInfos is not null)
                _propertiesSqlInfos[TableName] = (
                    sBInsertUpdate.ToString().TrimEnd([' ', ',']), 
                    _propertiesSqlInfos[TableName].properties, 
                    _propertiesSqlInfos[TableName].fields
                );

            return _propertiesSqlInfos?[TableName].sql ?? string.Empty;
        }
        else 
            return _propertiesSqlInfos?[TableName].sql ?? string.Empty;
    }
} 