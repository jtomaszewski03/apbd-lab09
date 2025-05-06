using System.ComponentModel.DataAnnotations;

namespace lab09.Model;

public class ProductWarehouseDto
{
    public int IdProduct { get; set; }
    public int IdWarehouse { get; set; }
    [Range(1, int.MaxValue)]
    public int Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}