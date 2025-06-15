using System.Reflection;
using AutoCRUD.Models;
using System.Data.SqlClient;
using Dapper;
using System.Text;
using System.Data;

namespace AutoCRUD.Data.SqlClient;

public class SqlClientRepository<E,I>  : Repository<SqlConnection, SqlCommand, E, I> 
where E : IEntity<I>
where I : struct 
{
    public SqlClientRepository(string tablename, string keyfieldname, string connectionstring, string? searchcolumnname = null)
    : base(tablename, keyfieldname, connectionstring, searchcolumnname) {}

    public override async Task<long> CountAsync() {
        using (var conn = CreateConnection())
        {
            await conn.OpenAsync();
            
            using (var cmd = new SqlCommand($"SELECT COUNT(id) FROM {TableName}", conn))
            {
                var count = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                return Convert.ToInt64(count ?? 0);
            }
        }
    }

    public override async Task<IEnumerable<E?>> SearchAsync(string? searchterm, int pageNumber = 1, int pageSize = 25) {

        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1, nameof(pageNumber));
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1, nameof(pageSize));

        using (var conn = CreateConnection())
        {
            await conn.OpenAsync();

            int offset = (pageNumber - 1) * pageSize;

            string sql = @$"
                SELECT {_propertiesSqlInfos?[TableName].fields}
                FROM {TableName} 
                {(string.IsNullOrWhiteSpace(searchterm) ? string.Empty : $"WHERE {SearchColumnName} like '%' + @value + '%'")}
                ORDER BY {keyFieldName}
                OFFSET @offset ROWS
                FETCH NEXT @pageSize ROWS ONLY;";

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
    
    public override async Task<int> InsertAsync(IEnumerable<E> data)
    {
        data = data ?? throw new ArgumentNullException(nameof(data));

        using (var conn = CreateConnection()) {

            await conn.OpenAsync();

            var bulkCopy = new SqlBulkCopy(conn);

            bulkCopy.DestinationTableName = TableName;
            bulkCopy.BatchSize = data.Count();            

            _ = bulkCopy.WriteToServerAsync(ConvertToDataTable(data));     

        }

        return data.Count();
    }

    protected override string GetSQLInsertUpdateAsync() {
        if (_propertiesSqlInfos is not null && string.IsNullOrWhiteSpace(_propertiesSqlInfos[TableName].sqlInsertUpdate)) {
            const string chartemp1 = " = @";

            if (string.IsNullOrWhiteSpace(TableName)) throw new Exception($"Table {TableName} not found.");

            var fields = String.Join(',', _propertiesSqlInfos[TableName].properties.Select((p)=> p.Name));
            var valuesupdate = String.Join(',', _propertiesSqlInfos[TableName].properties.Select((p)=> p.Name + chartemp1 + p.Name));

            var sBInsertUpdate = new StringBuilder(@$"
            if not exists(Select 1 from {TableName} where {keyFieldName}=@{keyFieldName})
                INSERT INTO {TableName}({fields})
                VALUES(@{fields.Replace(",",",@")})
            else
                UPDATE {TableName}
                set {valuesupdate}
                where {keyFieldName}=@{keyFieldName}",
                1152
            );

            if (_propertiesSqlInfos is not null)
                _propertiesSqlInfos[TableName] = (
                    sBInsertUpdate.ToString().TrimEnd([' ', ',']), 
                    _propertiesSqlInfos[TableName].properties, 
                    _propertiesSqlInfos[TableName].fields
                );

            return _propertiesSqlInfos?[TableName].sqlInsertUpdate ?? string.Empty;
        }
        else 
            return _propertiesSqlInfos?[TableName].sqlInsertUpdate ?? string.Empty;
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
