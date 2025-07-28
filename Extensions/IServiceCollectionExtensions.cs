using AutoCRUD.Data;
using AutoCRUD.Data.NpgSql;
using AutoCRUD.Data.SqlClient;
using AutoCRUD.Models;
using AutoCRUD.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AutoCRUD.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddServiceAutoCRUDValidation<E,I,S>(this IServiceCollection services) 
        where E : IEntity<I>
        where I : struct
        where S : class, IServiceAutoCRUDValidation<E, I> {

        services.AddSingleton<IServiceAutoCRUDValidation<E,I>, S>();

        return services;
    }

    public static IServiceCollection AddNpgSqlRepository<E,I>(
        this IServiceCollection services, 
        string tablename, 
        string keyfieldname, 
        string GetConnectionString, 
        string? searchcolumnname = null)
        where E : IEntity<I>
        where I : struct {

        services.AddScoped<IRepository<E,I>, NpgSqlRepository<E,I>>(
            (_) => new NpgSqlRepository<E,I>(tablename, keyfieldname, GetConnectionString, searchcolumnname)
        );

        return services;
    }

    public static IServiceCollection AddSqlClientRepository<E,I>(
        this IServiceCollection services, 
        string tablename, 
        string keyfieldname, 
        string GetConnectionString, 
        string? searchcolumnname = null)
        where E : IEntity<I>
        where I : struct {        

        services.AddScoped<IRepository<E,I>, SqlClientRepository<E,I>>(
            (_) => new SqlClientRepository<E,I>(tablename, keyfieldname, GetConnectionString, searchcolumnname)
        );

        return services;
    }
}