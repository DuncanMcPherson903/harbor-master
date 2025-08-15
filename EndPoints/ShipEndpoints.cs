using HarborMaster.Models;
using HarborMaster.Services;

namespace HarborMaster.Endpoints
{
  public static class ShipEndpoints
  {
    /*
       This method becomes an extension method of the app
       defined in Program.cs by using the `this` keyword
    */
    public static void MapShipEndpoints(this WebApplication app)
    {
      // GET /ships - Get all ships
      app.MapGet("/ships", async (DatabaseService db) =>
          await db.GetAllShipsAsync());

      // GET /ships/{id} - Get a ship by ID
      app.MapGet("/ships/{id}", async (int id, DatabaseService db) =>
      {
        var ship = await db.GetShipByIdAsync(id);
        return ship != null ? Results.Ok(ship) : Results.NotFound();
      });

      // POST /ships - Create a new ship
      app.MapPost("/ships", async (Ship ship, DatabaseService db) =>
      {
        try
        {
          // Validate input
          if (string.IsNullOrWhiteSpace(ship.Name))
          {
            return Results.BadRequest("Name is required");
          }

          if (string.IsNullOrWhiteSpace(ship.Type))
          {
            return Results.BadRequest("Type is required");
          }

          // If a dock is specified, check if it exists and has capacity
          if (ship.DockId.HasValue)
          {
            bool dockHasCapacity = await db.DockHasAvailableCapacityAsync(ship.DockId.Value);
            if (!dockHasCapacity)
            {
              return Results.BadRequest("The specified dock does not exist or has no available capacity");
            }
          }

          // Create the ship
          var newShip = await db.CreateShipAsync(ship);

          // Return a 201 Created response with the location of the new resource
          return Results.Created($"/ships/{newShip.Id}", newShip);
        }
        catch (Exception ex)
        {
          return Results.Problem($"An error occurred while creating the ship: {ex.Message}");
        }
      });

      // DELETE /ships/{id} - Delete a ship
      app.MapDelete("/ships/{id}", async (int id, DatabaseService db) =>
      {
        try
        {
          bool deleted = await db.DeleteShipAsync(id);

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
          return Results.Problem($"An error occurred while deleting the ship: {ex.Message}");
        }
      });

      app.MapPut("/ships/{id}", async (int id, Ship updatedShip, DatabaseService db) =>
      {
        try
        {

          // Check if the Ship exists
          var existingShip = await db.GetShipByIdAsync(id);
          if (existingShip == null)
          {
            return Results.NotFound($"Ship with ID {id} not found");
          }

          if (updatedShip.DockId != id)
          {
            // Check if the dock exists
            var existingDock = await db.GetDockByIdAsync(updatedShip.DockId.GetValueOrDefault());
            if (existingDock == null)
            {
              return Results.NotFound($"Dock with ID {updatedShip.DockId} not found");
            }

            // Check if dock has capacity
            var openDock = await db.DoesDockHaveCapacityAsync(updatedShip);
            if (openDock == false)
            {
              return Results.BadRequest($"Dock with ID {updatedShip.DockId} is at capacity");
            }
          }

          // Validate input
          if (string.IsNullOrWhiteSpace(updatedShip.Name))
          {
            return Results.BadRequest("Name is required");
          }

          if (string.IsNullOrWhiteSpace(updatedShip.Type))
          {
            return Results.BadRequest("Type is required");
          }


          // Set the ID from the route parameter
          updatedShip.Id = id;

          // Update the ship
          var result = await db.UpdateShipAsync(updatedShip);

          // Return the updated ship
          return Results.Ok(result);
        }
        catch (Exception ex)
        {
          return Results.Problem($"An error occurred while updating the ship: {ex.Message}");
        }
      });
    }
  }
}
