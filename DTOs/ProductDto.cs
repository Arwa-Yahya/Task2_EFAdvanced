namespace Task2_EF.DTOs;

public class ProductDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}