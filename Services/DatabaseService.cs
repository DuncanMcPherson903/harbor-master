using Npgsql;
using HarborMaster.Models;
using System.Data;

namespace HarborMaster.Services
{
  public class DatabaseService
  {
    private readonly string _connectionString;

    public DatabaseService(IConfiguration configuration)
    {
      _connectionString = configuration.GetConnectionString("HarborMasterConnectionString") ??
          throw new InvalidOperationException("Connection string 'HarborMasterConnectionString' not found.");
    }

    public NpgsqlConnection CreateConnection()
    {
      return new NpgsqlConnection(_connectionString);
    }

    // Helper method to execute non-query SQL commands
    public async Task ExecuteNonQueryAsync(string sql, Dictionary<string, object>? parameters = null)
    {
      using var connection = CreateConnection();
      await connection.OpenAsync();

      using var command = new NpgsqlCommand(sql, connection);
      if (parameters != null)
      {
        foreach (var param in parameters)
        {
          command.Parameters.AddWithValue(param.Key, param.Value);
        }
      }

      await command.ExecuteNonQueryAsync();
    }

    public async Task InitializeDatabaseAsync()
    {
      // First, create the database if it doesn't exist
      using var connection = new NpgsqlConnection(_connectionString.Replace("Database=harbormaster", "Database=postgres"));
      await connection.OpenAsync();

      // Check if database exists
      using var checkCommand = new NpgsqlCommand(
          "SELECT 1 FROM pg_database WHERE datname = 'harbormaster'",
          connection);
      var exists = await checkCommand.ExecuteScalarAsync();

      if (exists == null)
      {
        // Create the database
        using var createDbCommand = new NpgsqlCommand(
            "CREATE DATABASE harbormaster",
            connection);
        await createDbCommand.ExecuteNonQueryAsync();
      }

      // Now connect to the harbormaster database and create tables
      string sql = File.ReadAllText("database-setup.sql");
      await ExecuteNonQueryAsync(sql);
    }

    public async Task SeedDatabaseAsync()
    {
      // Check if data already exists
      using var connection = CreateConnection();
      await connection.OpenAsync();

      // Check if docks table has data
      using var command = new NpgsqlCommand("SELECT COUNT(*) FROM docks", connection);
      var count = Convert.ToInt32(await command.ExecuteScalarAsync());

      if (count > 0)
      {
        // Data already exists, no need to seed
        return;
      }

      // Seed docks
      await ExecuteNonQueryAsync(@"
        INSERT INTO docks (location, capacity) VALUES
        ('North Harbor', 5),
        ('South Harbor', 3),
        ('East Harbor', 7)
    ");

      // Seed haulers
      await ExecuteNonQueryAsync(@"
        INSERT INTO haulers (name, capacity) VALUES
        ('Oceanic Haulers', 10),
        ('Maritime Transport', 15),
        ('Sea Logistics', 8)
    ");

      // Seed ships
      await ExecuteNonQueryAsync(@"
        INSERT INTO ships (name, type, dock_id) VALUES
        ('Serenity', 'Firefly-class transport ship', 1),
        ('Rocinante', 'Corvette-class frigate', 2),
        ('Millennium Falcon', 'YT-1300 light freighter', 3),
        ('Black Pearl', 'Pirate galleon', 1),
        ('Nautilus', 'Submarine vessel', 2),
        ('Flying Dutchman', 'Ghost ship', 3),
        ('Enterprise', 'Constitution-class starship', 1),
        ('Voyager', 'Intrepid-class starship', 2),
        ('Defiant', 'Escort-class warship', 3),
        ('Galactica', 'Battlestar', 1),
        ('Bebop', 'Fishing trawler', 2),
        ('Normandy', 'Stealth frigate', 3),
        ('Pillar of Autumn', 'Halcyon-class cruiser', 1),
        ('Nostromo', 'Commercial towing vessel', 2),
        ('Sulaco', 'Military transport', 3),
        ('Highwind', 'Airship', 1),
        ('Argo', 'Ancient Greek galley', 2),
        ('Nebuchadnezzar', 'Hovership', 3)
    ");

      /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ///////////////////////////////////SHIPS/////////////////////////////////////////////////////////////////////////////////////
      /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    }
    // Get all ships
    public async Task<List<Ship>> GetAllShipsAsync()
    {
      var ships = new List<Ship>();

      using var connection = CreateConnection();
      await connection.OpenAsync();

      using var command = new NpgsqlCommand("SELECT id, name, type, dock_id FROM ships", connection);
      using var reader = await command.ExecuteReaderAsync();

      while (await reader.ReadAsync())
      {
        ships.Add(new Ship
        {
          Id = reader.GetInt32(0),
          Name = reader.GetString(1),
          Type = reader.GetString(2),
          DockId = reader.IsDBNull(3) ? null : reader.GetInt32(3)
        });
      }

      return ships;
    }

    // Get ship by ID
    public async Task<Ship?> GetShipByIdAsync(int id)
    {
      using var connection = CreateConnection();
      await connection.OpenAsync();

      using var command = new NpgsqlCommand(
          "SELECT id, name, type, dock_id FROM ships WHERE id = @id",
          connection);
      command.Parameters.AddWithValue("@id", id);

      using var reader = await command.ExecuteReaderAsync();

      if (await reader.ReadAsync())
      {
        return new Ship
        {
          Id = reader.GetInt32(0),
          Name = reader.GetString(1),
          Type = reader.GetString(2),
          DockId = reader.IsDBNull(3) ? null : reader.GetInt32(3)
        };
      }

      return null;
    }

    // Create a new ship
    public async Task<Ship> CreateShipAsync(Ship ship)
    {
      using var connection = CreateConnection();
      await connection.OpenAsync();

      using var command = new NpgsqlCommand(
          @"INSERT INTO ships (name, type, dock_id)
          VALUES (@name, @type, @dockId)
          RETURNING id",
          connection);

      command.Parameters.AddWithValue("@name", ship.Name);
      command.Parameters.AddWithValue("@type", ship.Type);

      // Handle null DockId
      if (ship.DockId.HasValue)
      {
        command.Parameters.AddWithValue("@dockId", ship.DockId.Value);
      }
      else
      {
        command.Parameters.AddWithValue("@dockId", DBNull.Value);
      }

      // Execute the command and get the generated ID
      ship.Id = Convert.ToInt32(await command.ExecuteScalarAsync());

      return ship;
    }

    // Delete a ship
    public async Task<bool> DeleteShipAsync(int id)
    {
      using var connection = CreateConnection();
      await connection.OpenAsync();

      using var command = new NpgsqlCommand(
          "DELETE FROM ships WHERE id = @id",
          connection);

      command.Parameters.AddWithValue("@id", id);

      int rowsAffected = await command.ExecuteNonQueryAsync();

      return rowsAffected > 0;
    }

    // Update an existing ship
    public async Task<Ship> UpdateShipAsync(Ship ship)
    {
      using var connection = CreateConnection();
      await connection.OpenAsync();

      using var command = new NpgsqlCommand(
          @"UPDATE ships
          SET name = @name,
              type = @type,
              dock_id = @dock_id
          WHERE id = @id",
          connection);

      command.Parameters.AddWithValue("@id", ship.Id);
      command.Parameters.AddWithValue("@name", ship.Name);
      command.Parameters.AddWithValue("@type", ship.Type);
      command.Parameters.AddWithValue("@dock_id", ship.DockId);

      // Execute the command
      await command.ExecuteNonQueryAsync();

      // Retrieve and return the updated ship
      return await GetShipByIdAsync(ship.Id);
    }

    // Check if a dock has capacity
    public async Task<bool> DoesDockHaveCapacityAsync(Ship ship)
    {
      using var connection = CreateConnection();
      await connection.OpenAsync();

      using var command = new NpgsqlCommand(
          @"SELECT capacity
          FROM docks
          WHERE id = @dockId",
          connection);

      command.Parameters.AddWithValue("@dockId", ship.DockId);

      using var command2 = new NpgsqlCommand(
          @"SELECT COUNT(*)
          FROM ships
          WHERE dock_id = @dockId",
          connection);

      command2.Parameters.AddWithValue("@dockId", ship.DockId);

      int dockCapacity = Convert.ToInt32(await command.ExecuteScalarAsync());
      int totalShips = Convert.ToInt32(await command.ExecuteScalarAsync());

      if (totalShips < dockCapacity)
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////DOCKS/////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Get All Docks
    public async Task<List<Dock>> GetAllDocksAsync()
    {
      var docks = new List<Dock>();

      using var connection = CreateConnection();
      await connection.OpenAsync();

      using var command = new NpgsqlCommand("SELECT id, location, capacity FROM docks", connection);
      using var reader = await command.ExecuteReaderAsync();

      while (await reader.ReadAsync())
      {
        docks.Add(new Dock
        {
          Id = reader.GetInt32(0),
          Location = reader.GetString(1),
          Capacity = reader.GetInt32(2)
        });
      }

      return docks;
    }

    // Get Dock by ID
    public async Task<Dock?> GetDockByIdAsync(int id)
    {
      using var connection = CreateConnection();
      await connection.OpenAsync();

      using var command = new NpgsqlCommand(
          "SELECT id, location, capacity FROM docks WHERE id = @id",
          connection);
      command.Parameters.AddWithValue("@id", id);

      using var reader = await command.ExecuteReaderAsync();

      if (await reader.ReadAsync())
      {
        return new Dock
        {
          Id = reader.GetInt32(0),
          Location = reader.GetString(1),
          Capacity = reader.GetInt32(2)
        };
      }

      return null;
    }

    // Create a new dock
    public async Task<Dock> CreateDockAsync(Dock dock)
    {
      using var connection = CreateConnection();
      await connection.OpenAsync();

      using var command = new NpgsqlCommand(
          @"INSERT INTO docks (location, capacity)
          VALUES (@location, @capacity)
          RETURNING id",
          connection);

      command.Parameters.AddWithValue("@location", dock.Location);
      command.Parameters.AddWithValue("@capacity", dock.Capacity);

      // Execute the command and get the generated ID
      dock.Id = Convert.ToInt32(await command.ExecuteScalarAsync());

      return dock;
    }


    // Check if a dock exists and has available capacity
    public async Task<bool> DockHasAvailableCapacityAsync(int dockId)
    {
      using var connection = CreateConnection();
      await connection.OpenAsync();

      // First check if the dock exists
      using var checkDockCommand = new NpgsqlCommand(
          "SELECT 1 FROM docks WHERE id = @dockId",
          connection);
      checkDockCommand.Parameters.AddWithValue("@dockId", dockId);

      var dockExists = await checkDockCommand.ExecuteScalarAsync();
      if (dockExists == null)
      {
        return false; // Dock doesn't exist
      }

      /* Then check if the dock has available capacity

       🧨 This is how you can use SQL to do logical operations
       you might otherwise be tempted to do in code.
       Copy/pasta this SQL into pgAdmin and see the results
       for different dock IDs.
      */
      using var capacityCommand = new NpgsqlCommand(
          @"SELECT d.capacity > COUNT(s.id)
          FROM docks d
          LEFT JOIN ships s ON d.id = s.dock_id
          WHERE d.id = @dockId
          GROUP BY d.capacity",
          connection);
      capacityCommand.Parameters.AddWithValue("@dockId", dockId);

      var hasCapacity = await capacityCommand.ExecuteScalarAsync();

      // If the dock has no ships yet, hasCapacity will be null, but the dock has capacity
      return hasCapacity == null || Convert.ToBoolean(hasCapacity);
    }

    // Delete a dock
    public async Task<bool> DeleteDockAsync(int id)
    {
      using var connection = CreateConnection();
      await connection.OpenAsync();

      using var command = new NpgsqlCommand(
          "DELETE FROM docks WHERE id = @id",
          connection);

      command.Parameters.AddWithValue("@id", id);

      int rowsAffected = await command.ExecuteNonQueryAsync();

      return rowsAffected > 0;
    }

    // Check if a dock is occupied
    public async Task<bool> DockHasShipsAsync(int dockId)
    {
      using var connection = CreateConnection();
      await connection.OpenAsync();

      // First check if the dock has ships
      using var checkDockCommand = new NpgsqlCommand(
          "SELECT * FROM ships WHERE dock_id = @id",
          connection);
      checkDockCommand.Parameters.AddWithValue("@id", dockId);

      var dockHasShips = await checkDockCommand.ExecuteScalarAsync();
      if (dockHasShips != null)
      {
        return true; // Dock has ships
      }
      else
      {
        return false; // Dock is empty
      }
    }

    // Update an existing dock
    public async Task<Dock> UpdateDockAsync(Dock dock)
    {
      using var connection = CreateConnection();
      await connection.OpenAsync();

      using var command = new NpgsqlCommand(
          @"UPDATE docks
          SET location = @location,
              capacity = @capacity
          WHERE id = @id",
          connection);

      command.Parameters.AddWithValue("@id", dock.Id);
      command.Parameters.AddWithValue("@location", dock.Location);
      command.Parameters.AddWithValue("@capacity", dock.Capacity);

      // Execute the command
      await command.ExecuteNonQueryAsync();

      // Retrieve and return the updated dock
      return await GetDockByIdAsync(dock.Id);
    }

    // Check if a dock has enough capacity for its current ships
    public async Task<bool> CanUpdateDockCapacityAsync(int dockId, int newCapacity)
    {
      using var connection = CreateConnection();
      await connection.OpenAsync();

      using var command = new NpgsqlCommand(
          @"SELECT COUNT(*)
          FROM ships
          WHERE dock_id = @dockId",
          connection);

      command.Parameters.AddWithValue("@dockId", dockId);

      // Get the number of ships at this dock
      int shipCount = Convert.ToInt32(await command.ExecuteScalarAsync());

      // Return true if the new capacity is sufficient
      return newCapacity >= shipCount;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////HAULERS/////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Create a new hauler
    public async Task<Hauler> CreateHaulerAsync(Hauler hauler)
    {
      using var connection = CreateConnection();
      await connection.OpenAsync();

      using var command = new NpgsqlCommand(
          @"INSERT INTO haulers (name, capacity)
          VALUES (@name, @capacity)
          RETURNING id",
          connection);

      command.Parameters.AddWithValue("@name", hauler.Name);
      command.Parameters.AddWithValue("@capacity", hauler.Capacity);

      hauler.Id = Convert.ToInt32(await command.ExecuteScalarAsync());

      return hauler;
    }

    // Get all haulers
    public async Task<List<Hauler>> GetAllHaulersAsync()
    {
      var haulers = new List<Hauler>();

      using var connection = CreateConnection();
      await connection.OpenAsync();

      using var command = new NpgsqlCommand("SELECT id, name, capacity FROM haulers", connection);
      using var reader = await command.ExecuteReaderAsync();

      while (await reader.ReadAsync())
      {
        haulers.Add(new Hauler
        {
          Id = reader.GetInt32(0),
          Name = reader.GetString(1),
          Capacity = reader.GetInt32(2)
        });
      }

      return haulers;
    }

    // Get Hauler by ID
    public async Task<Hauler?> GetHaulerByIdAsync(int id)
    {
      using var connection = CreateConnection();
      await connection.OpenAsync();

      using var command = new NpgsqlCommand(
          "SELECT id, name, capacity FROM haulers WHERE id = @id",
          connection);
      command.Parameters.AddWithValue("@id", id);

      using var reader = await command.ExecuteReaderAsync();

      if (await reader.ReadAsync())
      {
        return new Hauler
        {
          Id = reader.GetInt32(0),
          Name = reader.GetString(1),
          Capacity = reader.GetInt32(2)
        };
      }

      return null;
    }

    // Delete a hauler
    public async Task<bool> DeleteHaulerAsync(int id)
    {
      using var connection = CreateConnection();
      await connection.OpenAsync();

      using var command = new NpgsqlCommand(
          "DELETE FROM haulers WHERE id = @id",
          connection);

      command.Parameters.AddWithValue("@id", id);

      int rowsAffected = await command.ExecuteNonQueryAsync();

      return rowsAffected > 0;
    }

    // Update an existing hauler
    public async Task<Hauler> UpdateHaulerAsync(Hauler hauler)
    {
      using var connection = CreateConnection();
      await connection.OpenAsync();

      using var command = new NpgsqlCommand(
          @"UPDATE haulers
          SET name = @name,
          capacity = @capacity
          WHERE id = @id",
          connection);

      command.Parameters.AddWithValue("@id", hauler.Id);
      command.Parameters.AddWithValue("@name", hauler.Name);
      command.Parameters.AddWithValue("@capacity", hauler.Capacity);

      // Execute the command
      await command.ExecuteNonQueryAsync();

      // Retrieve and return the updated hauler
      return await GetHaulerByIdAsync(hauler.Id);
    }
  }
}
