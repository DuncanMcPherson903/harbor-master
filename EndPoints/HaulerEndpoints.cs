using HarborMaster.Models;
using HarborMaster.Services;

namespace HarborMaster.Endpoints
{
  public static class HaulerEndpoints
  {
    public static void MapHaulerEndpoints(this WebApplication app)
    {
      app.MapGet("/haulers", async (DatabaseService db) =>
          await db.GetAllHaulersAsync());

      app.MapGet("/haulers/{id}", async (int id, DatabaseService db) =>
      {
        var hauler = await db.GetHaulerByIdAsync(id);
        return hauler != null ? Results.Ok(hauler) : Results.NotFound();
      });

      app.MapPost("/haulers", async (Hauler hauler, DatabaseService db) =>
      {
        try
        {
          if (string.IsNullOrWhiteSpace(hauler.Name))
          {
            return Results.BadRequest("Name is required");
          }

          if (hauler.Capacity <= 0)
          {
            return Results.BadRequest("Capacity must be greater than zero");
          }

          var newHauler = await db.CreateHaulerAsync(hauler);

          return Results.Created($"/haulers/{newHauler.Id}", newHauler);
        }
        catch (Exception ex)
        {
          return Results.Problem($"An error occurred while creating the hauler: {ex.Message}");
        }
      });

      app.MapDelete("/haulers/{id}", async (int id, DatabaseService db) =>
      {
        try
        {
          bool deleted = await db.DeleteHaulerAsync(id);

          if (deleted)
          {
            // Return a 204 No Content response
            return Results.NoContent();
          }
          else
          {
            // Return a 404 Not Found response
            return Results.NotFound();
          }
        }
        catch (Exception ex)
        {
          return Results.Problem($"An error occurred while deleting the hauler: {ex.Message}");
        }
      });
    }
  }
}
