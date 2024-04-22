using WebApplication2.Dto;
using WebApplication2.Exceptions;
using WebApplication2.Repositories;

namespace WebApplication2.Services;

public interface IWarehouseService
{
    public Task<int> RegisterProductInWarehouseAsync(RegisterProductInWarehouseRequestDTO dto);
}

public class WarehouseService : IWarehouseService
{
    private readonly IWarehouseRepository _warehouseRepository;
    public WarehouseService(IWarehouseRepository warehouseRepository)
    {
        _warehouseRepository = warehouseRepository;
    }
    
    public async Task<int> RegisterProductInWarehouseAsync(RegisterProductInWarehouseRequestDTO dto)
    {
        // Example Flow:
        // check if product exists else throw NotFoundException
        if (!await _warehouseRepository.CheckProductExists(dto.IdProduct.Value))
            throw new NotFoundException("Product not found.");
        
        // check if warehouse exists else throw NotFoundException
        if (!await _warehouseRepository.CheckWarehouseExists(dto.IdWarehouse.Value))
            throw new NotFoundException("Warehouse not found.");
        
        // get order if exists else throw NotFoundException
        var order = await _warehouseRepository.GetOrder(dto.IdProduct.Value, dto.Amount);
        if (order == null)
            throw new NotFoundException("Order not found or insufficient quantity in any existing orders.");
        
        const int idOrder = 1;
        // check if product is already in warehouse else throw ConflictException
        if (await _warehouseRepository.CheckProductInWarehouse(order.IdOrder))
        {
            throw new ConflictException("This product has already been added to the warehouse for the specified order.");
        }

        var idProductWarehouse = await _warehouseRepository.RegisterProductInWarehouseAsync(
            idWarehouse: dto.IdWarehouse!.Value,
            idProduct: dto.IdProduct!.Value,
            idOrder: idOrder,
            createdAt: DateTime.UtcNow);

        if (!idProductWarehouse.HasValue)
            throw new Exception("Failed to register product in warehouse");

        return idProductWarehouse.Value;
    }
}