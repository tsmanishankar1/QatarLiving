using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static QLN.Common.DTO_s.AddonDto;

namespace QLN.Common.Infrastructure.IService.IAddonService
{
    public interface IAddonService
    {
        // Quantities
        Task<IEnumerable<QuantityResponse>> GetAllQuantitiesAsync();
        Task<Quantities> CreateQuantityAsync(CreateQuantityRequest request);

        // Currencies
       
        Task<Currency> CreateCurrencyAsync(CreateCurrencyRequest request);

        // UnitCurrency
        Task<UnitCurrency> CreatequantityCurrencyAsync(CreateUnitCurrencyRequest request);
        Task<IEnumerable<UnitCurrencyResponse>> GetByquantityIdAsync(Guid unitId);
 
    }
}


