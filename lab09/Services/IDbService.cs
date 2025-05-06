using lab09.Model;

namespace lab09.Services;

public interface IDbService
{
    Task<int> CreateProductWarehouse(ProductWarehouseDto productWarehouse);
}