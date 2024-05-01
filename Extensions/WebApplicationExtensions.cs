using AutoCRUD.Data;
using AutoCRUD.Models;
using AutoCRUD.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace AutoCRUD.Extensions;

public static class WebApplicationExtensionsExtensions
{
    public static WebApplication UseAutoCRUD<E>(this WebApplication app, string? defaultroute = null) where E : IEntity {

        defaultroute = defaultroute ?? typeof(E).Name + "s";

        defaultroute = (defaultroute.StartsWith('/') ? string.Empty : "/") + defaultroute.ToLower();

        app.MapPost(
            defaultroute, 
            async Task<IResult> (
                [FromBody]E entity, 
                [FromServices]IRepository<E> repository, 
                [FromServices]IServiceAutoCRUDValidation<E> serviceAutoCRUDvalidation) => {
            
            var validation = await serviceAutoCRUDvalidation.IsValidEntityAsync(entity, repository);
            if(!validation.Valid) return Results.BadRequest(nameof(entity));

            validation = await serviceAutoCRUDvalidation.isPostValidAsync(validation.Entity ?? entity, repository);
            if(!validation.Valid) return Results.UnprocessableEntity(nameof(entity));

            _ = repository.InsertAsync((E)(validation.Entity ?? entity));
            
            return Results.Created($"{defaultroute}/{entity.Id}", entity);
        });

        app.MapGet(
            $"{defaultroute}/{{id}}", 
            async Task<IResult> (
                string id, 
                [FromServices]IRepository<E> repository, 
                [FromServices]IServiceAutoCRUDValidation<E> serviceAutoCRUDvalidation) => {

            var validationid = serviceAutoCRUDvalidation.isValidID(id, repository);
            if(!validationid.Valid) return Results.BadRequest(nameof(id));

            var validation = await serviceAutoCRUDvalidation.isGetValidAsync(validationid.Guid, repository);
            if(!validation.Valid) return Results.UnprocessableEntity(nameof(id));

            var Entity = validation.Entity ?? await repository.FindByIDAsync(validationid.Guid);
            if (Entity is null)
                return Results.NotFound(validationid.Guid);
            else
                return Results.Ok(Entity);
        });

        app.MapPut(
            defaultroute, 
            async Task<IResult> (
                [FromBody]E entity, 
                [FromServices]IRepository<E> repository, 
                [FromServices]IServiceAutoCRUDValidation<E> serviceAutoCRUDvalidation) => {

            var validation = await serviceAutoCRUDvalidation.IsValidEntityAsync(entity, repository);
            if(!validation.Valid) return Results.BadRequest(nameof(entity));

            validation = await serviceAutoCRUDvalidation.isPutValidAsync(validation.Entity ?? entity, repository);        
            if(!validation.Valid) return Results.UnprocessableEntity(nameof(entity));

            _ = repository.InsertAsync((E)(validation.Entity ?? entity));
            
            return Results.Ok(entity);
        });

        app.MapDelete(
            $"{defaultroute}/{{id}}",
            async Task<IResult>  (
                string id,
                [FromServices]IRepository<E> repository,
                [FromServices]IServiceAutoCRUDValidation<E> serviceAutoCRUDvalidation) => {

            var validationid = serviceAutoCRUDvalidation.isValidID(id, repository);
            if(!validationid.Valid) return Results.BadRequest(nameof(id));

            var validation = await serviceAutoCRUDvalidation.isDeleteValidAsync(validationid.Guid, repository);
            if(!validation.Valid) return Results.UnprocessableEntity(nameof(id));

            var Entity = validation.Entity ?? await repository.FindByIDAsync(validationid.Guid);
            if (Entity is null)
                return Results.NotFound(validationid.Guid);
            else {
                _ = repository.DeleteAsync(validationid.Guid);
                return Results.Ok(Entity);
            }
        });

        app.MapDelete(
            defaultroute, 
            async Task<IResult> (
                [FromBody]E entity, 
                [FromServices]IRepository<E> repository, 
                [FromServices]IServiceAutoCRUDValidation<E> serviceAutoCRUDvalidation) => {

            var validation = await serviceAutoCRUDvalidation.IsValidEntityAsync(entity, repository);
            if(!validation.Valid) return Results.BadRequest(nameof(entity));

            validation = await serviceAutoCRUDvalidation.isDeleteValidAsync(validation.Entity ?? entity, repository);
            if(!validation.Valid) return Results.UnprocessableEntity(nameof(entity));

            var Entity = validation.Entity ?? await repository.FindByIDAsync(entity.Id);
            if (Entity is null)
                return Results.NotFound(entity.Id);
            else {
                _ = repository.DeleteAsync(Entity.Id);
                return Results.Ok(Entity);
            }
        });

        app.MapGet(
            defaultroute,  
            async Task<IResult> (
                [FromQuery]string t, 
                [FromServices]IRepository<E> repository, 
                [FromServices]IServiceAutoCRUDValidation<E> serviceAutoCRUDvalidation) => {

            var validation = serviceAutoCRUDvalidation.isSearchTermValid(t, repository);
            if(validation.Valid) return Results.BadRequest(nameof(t));

            var entitys = await repository.SearchAsync(validation.SearchTerm ?? t);
            if (entitys is null)
                return Results.NotFound(validation.SearchTerm ?? t);
            else
                return Results.Ok(entitys);
        });

        app.MapGet(
            $"/count-{defaultroute.TrimStart('/')}", 
            async Task<IResult> ([FromServices]IRepository<E> repository) => 
            
            Results.Ok(await repository.CountAsync()));

        return app;
    }
}