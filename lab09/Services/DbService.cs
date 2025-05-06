using lab09.Exceptions;
using lab09.Model;
using Microsoft.Data.SqlClient;

namespace lab09.Services;

public class DbService : IDbService
{
    private readonly string? _connectionString;
    public DbService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default");
    }

    public async Task<int> CreateProductWarehouse(ProductWarehouseDto productWarehouseDto)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();
        
        var transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        try
        {
            command.Parameters.Clear();
            command.CommandText = "SELECT 1 FROM Product WHERE IdProduct = @Id";
            command.Parameters.AddWithValue("@Id", productWarehouseDto.IdProduct);
            var result = await command.ExecuteScalarAsync();
            if (result == null)
            {
                throw new NotFoundException($"Product with Id {productWarehouseDto.IdProduct} was not found.");
            }
            command.Parameters.Clear();
            
            command.CommandText = "SELECT 1 FROM Warehouse WHERE IdWarehouse = @Id";
            command.Parameters.AddWithValue("@Id", productWarehouseDto.IdWarehouse);
            result = await command.ExecuteScalarAsync();
            if (result == null)
            {
                throw new NotFoundException("Warehouse with Id " + productWarehouseDto.IdWarehouse + " was not found.");
            }
            command.Parameters.Clear();
            command.CommandText = "SELECT IdOrder FROM [Order] WHERE IdProduct = @IdProduct AND Amount >= @Amount AND CreatedAt <= @CreatedAt";
            command.Parameters.AddWithValue("@IdProduct", productWarehouseDto.IdProduct);
            command.Parameters.AddWithValue("@Amount", productWarehouseDto.Amount);
            command.Parameters.AddWithValue("@CreatedAt", productWarehouseDto.CreatedAt);
            var orderId = await command.ExecuteScalarAsync();
            if (orderId == null)
            {
                throw new NotFoundException($"Order for amount {productWarehouseDto.Amount} of product {productWarehouseDto.IdProduct} was not found.");
            }
            orderId = Convert.ToInt32(orderId);
            command.Parameters.Clear();
            command.CommandText = "SELECT 1 FROM Product_Warehouse WHERE IdOrder = @IdOrder";
            command.Parameters.AddWithValue("@IdOrder", orderId);
            result = await command.ExecuteScalarAsync();
            if (result != null)
            {
                throw new InvalidOperationException("Order has already been processed.");
            }
            command.Parameters.Clear();
            //aktualizacja tabeli Order o nowe FulfilledAt
            var dateNow = DateTime.Now;
            command.CommandText = "UPDATE [Order] SET FulfilledAt = @FulfilledAt WHERE IdOrder = @IdOrder";
            command.Parameters.AddWithValue("@IdOrder", orderId);
            command.Parameters.AddWithValue("@FulfilledAt", dateNow);
            await command.ExecuteNonQueryAsync();
            
            //zdobycie ceny
            command.Parameters.Clear();
            int price;
            command.CommandText = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
            command.Parameters.AddWithValue("@IdProduct", productWarehouseDto.IdProduct);
            price = Convert.ToInt32(await command.ExecuteScalarAsync());
            command.Parameters.Clear();
            
            //wstawianie rekordu do Product_Warehouse
            command.CommandText = @"INSERT INTO Product_Warehouse 
                              (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                              OUTPUT INSERTED.IdProductWarehouse
                              VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt)";
            command.Parameters.AddWithValue("@IdWarehouse", productWarehouseDto.IdWarehouse);
            command.Parameters.AddWithValue("@IdProduct", productWarehouseDto.IdProduct);
            command.Parameters.AddWithValue("@IdOrder", orderId);
            command.Parameters.AddWithValue("@Amount", productWarehouseDto.Amount);
            command.Parameters.AddWithValue("@Price", price * productWarehouseDto.Amount);
            command.Parameters.AddWithValue("@CreatedAt", dateNow);
            
            result = await command.ExecuteScalarAsync();
            
            await transaction.CommitAsync();
            
            return Convert.ToInt32(result);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}