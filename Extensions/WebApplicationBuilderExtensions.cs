using AutoCRUD.Data;
using AutoCRUD.Data.NpgSql;
using AutoCRUD.Data.SqlClient;
using AutoCRUD.Models;
using AutoCRUD.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoCRUD.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddServiceAutoCRUDValidation<E,S>(this WebApplicationBuilder builder) 
        where E : IEntity 
        where S : class, IServiceAutoCRUDValidation<E> {

        builder.Services.AddSingleton<IServiceAutoCRUDValidation<E>, S>();

        return builder;
    }

    public static WebApplicationBuilder AddNpgSqlRepository<E>(
        this WebApplicationBuilder builder, 
        string tablename, 
        string keyfieldname, 
        string GetConnectionString, 
        string? searchcolumnname = null) where E : IEntity {

        builder.Services.AddSingleton<IRepository<E>, NpgSqlRepository<E>>(
            (service) => new NpgSqlRepository<E>(tablename, keyfieldname, GetConnectionString, searchcolumnname)
        );

        return builder;
    }

    public static WebApplicationBuilder AddSqlClientRepository<E>(
        this WebApplicationBuilder builder, 
        string tablename, 
        string keyfieldname, 
        string GetConnectionString, 
        string? searchcolumnname = null) where E : IEntity {

        builder.Services.AddSingleton<IRepository<E>, SqlClientRepository<E>>(
            (service) => new SqlClientRepository<E>(tablename, keyfieldname, GetConnectionString, searchcolumnname)
        );

        return builder;
    }    
}