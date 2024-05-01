using System.Reflection;
using AutoCRUD.Models;
using System.Data.SqlClient;
using Dapper;
using System.Text;
using System.Data;

namespace AutoCRUD.Data.SqlClient;

public class SqlClientRepository<E> : IRepository<E> where E : IEntity {
   private static Dictionary<string, (string? sql, IEnumerable<PropertyInfo> properties, string fields)>? _propertiesSqlInfos;

    public SqlClientRepository(string tablename, string keyfieldname, string connectionstring, string? searchcolumnname = null)
    {
        ConnectionString = connectionstring;
        
        _propertiesSqlInfos ??= new Dictionary<string, (string?, IEnumerable<PropertyInfo>, string)>();
        
        keyFieldName = keyfieldname ?? throw new ArgumentException(nameof(keyfieldname));
        if (keyfieldname.Contains('\'')) throw new ArgumentException(nameof(keyfieldname));
        
        if (tablename is not null && tablename.Contains('\'')) throw new ArgumentException(nameof(tablename));

        if(string.IsNullOrWhiteSpace(searchcolumnname)) searchcolumnname = null;            

        SearchColumnName = searchcolumnname ?? keyfieldname;

        TableName ??= typeof(E).Name;

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
        using (var connection = new SqlConnection(ConnectionString))
        {
            await connection.OpenAsync();
            
            using (var cmd = new SqlCommand($"SELECT COUNT(id) FROM {TableName}", connection))
            {
                return (long)(await cmd.ExecuteScalarAsync() ?? 0);
            }
        }
    }

    public async Task<E?> FindByIDAsync(Guid id) {
        using (var conn = new SqlConnection(ConnectionString))
        {
            await conn.OpenAsync();

            string sql = @$"
                SELECT {_propertiesSqlInfos?[TableName].fields} 
                FROM {TableName}
                WHERE {keyFieldName} = @value";

            return (await conn.QueryAsync<E?>(sql, new { value = id })).FirstOrDefault();
        }
    }

    public async Task<IEnumerable<E?>> SearchAsync(string value) {

        value = value ?? throw new ArgumentException(nameof(value));

        using (var conn = new SqlConnection(ConnectionString))
        {
            await conn.OpenAsync();

            string sql = @$"
                SELECT {_propertiesSqlInfos?[TableName].fields}
                FROM {TableName} 
                WHERE {SearchColumnName} like '%' || @value || '%'";

            return await conn.QueryAsync<E?>(sql, new { value = value });
        }
    }

    public async Task<E?> FindByFieldAsync(string fieldname, object value) {

        fieldname = fieldname ?? throw new ArgumentException(nameof(fieldname));

        if(fieldname.Contains('\'')) throw new ArgumentException(nameof(fieldname));

        value = value ?? throw new ArgumentException(nameof(value));

        using (var conn = new SqlConnection(ConnectionString))
        {
            await conn.OpenAsync();

            string sql = @$"
                SELECT {_propertiesSqlInfos?[TableName].fields}
                FROM {TableName} 
                WHERE {fieldname} = @value";
                
            return (await conn.QueryAsync<E?>(sql, new { value = value })).FirstOrDefault();
        }
    }

    public async Task<E?> FindByFieldAsync(string fieldname, E entity) {

        var field = _propertiesSqlInfos?[TableName].properties.Single((p)=> p.Name.ToLower() == fieldname.ToLower());

        return await FindByFieldAsync(fieldname, field?.GetValue(entity) ?? string.Empty);
    }

    public async Task<bool> InsertAsync(E data)
    {

        if (data is null) return false;

        using (var connection = new SqlConnection(ConnectionString))
        {
            try
            {
                await connection.OpenAsync();

                var rowsAffected = await connection.ExecuteAsync(GetSQLAsync(), data).ConfigureAwait(false);

                return rowsAffected > 0;
                
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

        using (var connection = new SqlConnection(ConnectionString)) {

            await connection.OpenAsync();

            var bulkCopy = new SqlBulkCopy(connection);

            bulkCopy.DestinationTableName = TableName;
            bulkCopy.BatchSize = data.Count();            

            _ = bulkCopy.WriteToServerAsync(ConvertToDataTable(data));     

        }

        return data.Count();
    }

   public async Task<bool> DeleteAsync(Guid id) {
        using (var conn = new SqlConnection(ConnectionString))
        {
            await conn.OpenAsync();

            string sql = @$"
                Delete {TableName}
                WHERE {keyFieldName} = @value";

            await conn.ExecuteAsync(sql, new { value = id });

            return true;
        }
    }

    private string GetSQLAsync() {
        if (_propertiesSqlInfos is not null && string.IsNullOrWhiteSpace(_propertiesSqlInfos[TableName].sql)) {
            const string chartemp1 = " = @";

            if (string.IsNullOrWhiteSpace(TableName)) throw new Exception($"Table {TableName} not found.");

            var fields = String.Join(',', _propertiesSqlInfos[TableName].properties.Select((p)=> p.Name));
            var valuesupdate = String.Join(',', _propertiesSqlInfos[TableName].properties.Select((p)=> p.Name + chartemp1 + p.Name));

            var sBInsertUpdate = new StringBuilder(@$"
            if not exists(Select 1 from {TableName} where {keyFieldName}=@value)
                INSERT INTO {TableName}({fields})
                VALUES(@{fields.Replace(",",",@")})
            else
                UPDATE {TableName}
                {valuesupdate}
                where {keyFieldName}=@value",
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

    private DataTable ConvertToDataTable(IEnumerable<E> items)
    {
        DataTable dataTable = new DataTable();

        PropertyInfo[] properties = typeof(E).GetProperties();

        foreach (PropertyInfo property in properties)
        {
            dataTable.Columns.Add(property.Name, property.PropertyType);
        }

        foreach (E item in items)
        {
            DataRow row = dataTable.NewRow();
            foreach (PropertyInfo property in properties)
            {
                row[property.Name] = property.GetValue(item);
            }
            dataTable.Rows.Add(row);
        }

        return dataTable;
    }    
}
