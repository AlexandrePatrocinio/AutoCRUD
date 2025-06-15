using AutoCRUD.Data;
using AutoCRUD.Models;
using AutoCRUD.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace AutoCRUD.Extensions;

public static class WebApplicationExtensionsExtensions
{
    public static WebApplication UseAutoCRUD<E,I>(this WebApplication app, string? defaultroute = null)
    where E : IEntity<I>
    where I : struct{

        defaultroute = defaultroute ?? typeof(E).Name + "s";

        defaultroute = (defaultroute.StartsWith('/') ? string.Empty : "/") + defaultroute.ToLower();

        app.MapPost(
            defaultroute, 
            async Task<IResult> (
                [FromBody]E entity, 
                [FromServices]IRepository<E,I> repository, 
                [FromServices]IServiceAutoCRUDValidation<E,I> serviceAutoCRUDvalidation) => {
            
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
                [FromServices]IRepository<E,I> repository, 
                [FromServices]IServiceAutoCRUDValidation<E,I> serviceAutoCRUDvalidation) => {

            var validationid = serviceAutoCRUDvalidation.isValidID(id, repository);
            if(!validationid.Valid) return Results.BadRequest(nameof(id));

            var validation = await serviceAutoCRUDvalidation.isGetValidAsync(validationid.Id, repository);
            if(!validation.Valid) return Results.UnprocessableEntity(nameof(id));

            var Entity = validation.Entity ?? await repository.FindByIDAsync(validationid.Id);
            if (Entity is null)
                return Results.NotFound(validationid.Id);
            else
                return Results.Ok(Entity);
        });

        app.MapPut(
            defaultroute, 
            async Task<IResult> (
                [FromBody]E entity, 
                [FromServices]IRepository<E,I> repository, 
                [FromServices]IServiceAutoCRUDValidation<E,I> serviceAutoCRUDvalidation) => {

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
                [FromServices]IRepository<E,I> repository,
                [FromServices]IServiceAutoCRUDValidation<E,I> serviceAutoCRUDvalidation) => {

            var validationid = serviceAutoCRUDvalidation.isValidID(id, repository);
            if(!validationid.Valid) return Results.BadRequest(nameof(id));

            var validation = await serviceAutoCRUDvalidation.isDeleteValidAsync(validationid.Id, repository);
            if(!validation.Valid) return Results.UnprocessableEntity(nameof(id));

            var Entity = validation.Entity ?? await repository.FindByIDAsync(validationid.Id);
            if (Entity is null)
                return Results.NotFound(validationid.Id);
            else {
                _ = repository.DeleteAsync(validationid.Id);
                return Results.Ok(Entity);
            }
        });

        app.MapDelete(
            defaultroute, 
            async Task<IResult> (
                [FromBody]E entity, 
                [FromServices]IRepository<E,I> repository, 
                [FromServices]IServiceAutoCRUDValidation<E,I> serviceAutoCRUDvalidation) => {

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
                [FromServices]IRepository<E,I> repository, 
                [FromServices]IServiceAutoCRUDValidation<E,I> serviceAutoCRUDvalidation,
                [FromQuery] string? t,
                [FromQuery] int pn = 1,
                [FromQuery] int ps = 25) =>
            {
                if (!string.IsNullOrWhiteSpace(t))
                {
                    var validation = serviceAutoCRUDvalidation.isSearchTermValid(t, repository);
                    if (!validation.Valid) return Results.BadRequest(nameof(t));
                }

                var entitys = await repository.SearchAsync(t, pn, ps);
                if (entitys is null)
                    return Results.NotFound(t);
                else
                    return Results.Ok(entitys);
            }
        );

        app.MapGet(
            $"/count-{defaultroute.TrimStart('/')}", 
            async Task<IResult> ([FromServices]IRepository<E,I> repository) => 
            
            Results.Ok(await repository.CountAsync()));

        return app;
    }
}