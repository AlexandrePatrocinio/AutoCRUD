using System.Reflection;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace AutoCRUD.Data.NpgSql;

public class NpgSqlBulkCopy : IDisposable {

    public NpgSqlBulkCopy(NpgsqlConnection connection)
    {
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public NpgsqlConnection connection { get; private set; }

    public string? DestinationTableName { get; set; }

    public async void WriteToServerAsync<T>(IEnumerable<T> data) {
        var openConnection = false;
        string? query = null;
        try
        {
            ArgumentNullException.ThrowIfNull(DestinationTableName, nameof(DestinationTableName));
            
            PropertyInfo[] properties = typeof(T).GetProperties();
            int colCount = properties.Length;

            NpgsqlDbType[] types = new NpgsqlDbType[colCount];

            openConnection = connection.State == System.Data.ConnectionState.Open;

            if (!openConnection) 
                await connection.OpenAsync();
            
            (query, types) = await GetSQLInformationsAsync(colCount);

            using (var writer = connection.BeginBinaryImport($"COPY {DestinationTableName} ({query}) FROM STDIN (FORMAT BINARY)"))
            {
                foreach (var t in data)
                {
                    writer.StartRow();

                    for (int i = 0; i < colCount; i++)
                    {
                        if (properties[i].GetValue(t) is null)
                        {
                            writer.WriteNull();
                        }
                        else
                        {
                            var value = properties[i].GetValue(t);

                            switch (types[i])
                            {
                                case NpgsqlDbType.Uuid:
                                    writer.Write(value is not null ? (Guid)value : Guid.Empty, types[i]);
                                    break;
                                case NpgsqlDbType.Bigint:
                                    writer.Write(value is not null ? (long)value : 0, types[i]);
                                    break;
                                case NpgsqlDbType.Integer:
                                    writer.Write(value is not null ? (int)value : 0, types[i]);
                                    break;
                                case NpgsqlDbType.Smallint:
                                    writer.Write(value is not null ? (short)value : 0, types[i]);
                                    break;
                                case NpgsqlDbType.Char:
                                    writer.Write(value is not null ? (char)value : '\0', types[i]);
                                    break;
                                case NpgsqlDbType.Varchar:
                                    writer.Write(value?.ToString(), types[i]);
                                    break;
                                case NpgsqlDbType.Bit:
                                case NpgsqlDbType.Boolean:
                                    writer.Write(value is not null ? (bool)value : false, types[i]);
                                    break;
                                case NpgsqlDbType.Date:
                                    writer.Write(value is not null ? (DateTime)value : DateTime.MinValue, types[i]);
                                    break;
                                case NpgsqlDbType.Double:
                                case NpgsqlDbType.Money:
                                case NpgsqlDbType.Real:
                                    writer.Write(value is not null ? (float)value : 0f, types[i]);
                                    break;
                                case NpgsqlTypes.NpgsqlDbType.Array:
                                case NpgsqlTypes.NpgsqlDbType.Text:
                                    writer.Write(properties[i].GetValue(t), NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Text);
                                    break;
                                    // ... other cases for different types
                            }
                        }
                    }
                }

                writer.Complete();
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error executing NpgSqlBulkCopy.WriteToServer ().", ex);
        }
        finally {
            if (!openConnection)
                await connection.CloseAsync(); 
        }
    }

    private async Task<(string, NpgsqlDbType[])> GetSQLInformationsAsync(int columnCount) {
        var sB = new StringBuilder(384);
        NpgsqlDbType[] types = new NpgsqlDbType[columnCount];

        using (var cmd = new NpgsqlCommand($"SELECT * FROM {DestinationTableName} LIMIT 1", connection))
        {
            using (var rdr = await cmd.ExecuteReaderAsync())
            {
                if (rdr.FieldCount != columnCount)
                {
                    throw new ArgumentOutOfRangeException("data", "Column count in Destination Table does not match propertie count in source data class.");
                }

                var columns = rdr.GetColumnSchema();                    
                for (int i = 0; i < columnCount; i++)
                {
                    types[i] = columns[i].NpgsqlDbType ?? NpgsqlDbType.Varchar;
                    sB.Append(", " + columns[i].ColumnName);
                }
            }
        }

        return (sB.ToString(), types);
    }

    public void Dispose()
    {

    }
}