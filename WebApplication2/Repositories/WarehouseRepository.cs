using System.Data;
using System.Data.SqlClient;
using WebApplication2.Models;

namespace WebApplication2.Repositories;

public interface IWarehouseRepository
{
    public Task<int?> RegisterProductInWarehouseAsync(int idWarehouse, int idProduct, int idOrder, DateTime createdAt);
    public Task RegisterProductInWarehouseByProcedureAsync(int idWarehouse, int idProduct, DateTime createdAt);
    Task<bool> CheckProductExists(int productId);
    Task<bool> CheckWarehouseExists(int warehouseId);
    Task<Order> GetOrder(int productId, int amount);
    Task<bool> CheckProductInWarehouse(int orderId);
}

public class WarehouseRepository : IWarehouseRepository
{
    private readonly IConfiguration _configuration;
    public WarehouseRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task<int?> RegisterProductInWarehouseAsync(int idWarehouse, int idProduct, int idOrder, DateTime createdAt)
    {
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();
        
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var query = "UPDATE \"Order\" SET FulfilledAt = @FulfilledAt WHERE IdOrder = @IdOrder";
            await using var command = new SqlCommand(query, connection);
            command.Transaction = (SqlTransaction)transaction;
            command.Parameters.AddWithValue("@IdOrder", idOrder);
            command.Parameters.AddWithValue("@FulfilledAt", DateTime.UtcNow);
            await command.ExecuteNonQueryAsync();
            
            command.CommandText = @"
                      INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, CreatedAt, Amount, Price)
                      OUTPUT Inserted.IdProductWarehouse
                      VALUES (@IdWarehouse, @IdProduct, @IdOrder, @CreatedAt, 0, 0);";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@IdWarehouse", idWarehouse);
            command.Parameters.AddWithValue("@IdProduct", idProduct);
            command.Parameters.AddWithValue("@IdOrder", idOrder);
            command.Parameters.AddWithValue("@CreatedAt", createdAt);
            var idProductWarehouse = (int)await command.ExecuteScalarAsync();

            await transaction.CommitAsync();
            return idProductWarehouse;
        }
        catch
        {
            await transaction.RollbackAsync();
            return null;
        }
    }
    
    public async Task RegisterProductInWarehouseByProcedureAsync(int idWarehouse, int idProduct, DateTime createdAt)
    {
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();
        await using var command = new SqlCommand("AddProductToWarehouse", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("IdProduct", idProduct);
        command.Parameters.AddWithValue("IdWarehouse",idWarehouse);
        command.Parameters.AddWithValue("Amount", 0);
        command.Parameters.AddWithValue("CreatedAt", createdAt);
        await command.ExecuteNonQueryAsync();
    }
    
    public async Task<bool> CheckProductExists(int productId)
    {
        var connectionString = _configuration["ConnectionStrings:DefaultConnection"];
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        var query = "SELECT COUNT(*) FROM Product WHERE IdProduct = @ProductId";
        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@ProductId", productId);
        var result = (int)await command.ExecuteScalarAsync();
        return result > 0;
    }

    public async Task<bool> CheckWarehouseExists(int warehouseId)
    {
        var connectionString = _configuration["ConnectionStrings:DefaultConnection"];
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        var query = "SELECT COUNT(*) FROM Warehouse WHERE IdWarehouse = @WarehouseId";
        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@WarehouseId", warehouseId);
        var result = (int)await command.ExecuteScalarAsync();
        return result > 0;
    }

    public async Task<Order> GetOrder(int productId, int amount)
    {
        // Simplified logic for example
        // You need to implement actual data fetching and object creation logic
        return new Order { IdOrder = 1, ProductId = productId, Amount = amount};
    }

    public async Task<bool> CheckProductInWarehouse(int orderId)
    {
        var connectionString = _configuration["ConnectionStrings:DefaultConnection"];
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        var query = "SELECT COUNT(*) FROM Product_Warehouse WHERE IdOrder = @OrderId";
        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@OrderId", orderId);
        var result = (int)await command.ExecuteScalarAsync();
        return result > 0;
    }
}