using HarborMaster.Models;
using HarborMaster.Services;

namespace HarborMaster.Endpoints
{
  public static class DockEndpoints
  {
    public static void MapDockEndpoints(this WebApplication app)
    {
      app.MapGet("/docks", async (DatabaseService db) =>
          await db.GetAllDocksAsync());

      app.MapGet("/docks/{id}", async (int id, DatabaseService db) =>
      {
        var dock = await db.GetDockByIdAsync(id);
        return dock != null ? Results.Ok(dock) : Results.NotFound();
      });

      app.MapPost("/docks", async (Dock dock, DatabaseService db) =>
      {
        try
        {
          if (string.IsNullOrWhiteSpace(dock.Location))
          {
            return Results.BadRequest("Location is required");
          }

          if (dock.Capacity <= 0)
          {
            return Results.BadRequest("Capacity must be greater than zero");
          }

          var newDock = await db.CreateDockAsync(dock);

          return Results.Created($"/docks/{newDock.Id}", newDock);
        }
        catch (Exception ex)
        {
          return Results.Problem($"An error occurred while creating the dock: {ex.Message}");
        }
      });

      app.MapDelete("/docks/{id}", async (int id, DatabaseService db) =>
      {
        try
        {

          bool dockOccupied = await db.DockHasShipsAsync(id);

          if (dockOccupied)
          {
            return Results.BadRequest("The specified dock is currently occupied");
          }

          bool deleted = await db.DeleteDockAsync(id);

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
          return Results.Problem($"An error occurred while deleting the dock: {ex.Message}");
        }
      });

      // PUT /docks/{id} - Update a dock
      app.MapPut("/docks/{id}", async (int id, Dock updatedDock, DatabaseService db) =>
      {
        try
        {
          // Check if the dock exists
          var existingDock = await db.GetDockByIdAsync(id);
          if (existingDock == null)
          {
            return Results.NotFound($"Dock with ID {id} not found");
          }

          // Validate input
          if (string.IsNullOrWhiteSpace(updatedDock.Location))
          {
            return Results.BadRequest("Location is required");
          }

          if (updatedDock.Capacity <= 0)
          {
            return Results.BadRequest("Capacity must be greater than zero");
          }

          // Check if the new capacity is sufficient for current ships
          if (updatedDock.Capacity < existingDock.Capacity)
          {
            bool canUpdate = await db.CanUpdateDockCapacityAsync(id, updatedDock.Capacity);
            if (!canUpdate)
            {
              return Results.BadRequest("Cannot reduce capacity below the number of ships currently at this dock");
            }
          }

          // Set the ID from the route parameter
          updatedDock.Id = id;

          // Update the dock
          var result = await db.UpdateDockAsync(updatedDock);

          // Return the updated dock
          return Results.Ok(result);
        }
        catch (Exception ex)
        {
          return Results.Problem($"An error occurred while updating the dock: {ex.Message}");
        }
      });
    }
  }
}
