using WebPhotocopyHub.Domain.Entities;

namespace WebPhotocopyHub.DataAccess.Cache;

public static class CCache_Pricing_Rule
{
    private static readonly object Obj_Lock = new();
    private static List<PricingRule> Arr_Data = new();
    private static Dictionary<Guid, PricingRule> Dic_Data_ID = new();

    public static void Load_Cache_Pricing_Rule(IEnumerable<PricingRule> p_arrData)
    {
        lock (Obj_Lock)
        {
            Arr_Data = p_arrData
                .Where(p_objData => p_objData.IsActive)
                .OrderBy(p_objData => p_objData.PaperSize)
                .ThenBy(p_objData => p_objData.ColorMode)
                .ThenBy(p_objData => p_objData.PrintSide)
                .ToList();
            Dic_Data_ID = Arr_Data.ToDictionary(p_objData => p_objData.Id);
        }
    }

    public static void Add_Data(PricingRule p_objData)
    {
        if (p_objData.Id == Guid.Empty)
        {
            return;
        }

        lock (Obj_Lock)
        {
            if (Dic_Data_ID.ContainsKey(p_objData.Id))
            {
                return;
            }

            Arr_Data.Add(p_objData);
            Dic_Data_ID[p_objData.Id] = p_objData;
        }
    }

    public static void Update_Data(PricingRule p_objData)
    {
        if (p_objData.Id == Guid.Empty)
        {
            return;
        }

        lock (Obj_Lock)
        {
            Delete_Data(p_objData.Id);
            Add_Data(p_objData);
        }
    }

    public static void Delete_Data(Guid p_iAuto_ID)
    {
        lock (Obj_Lock)
        {
            Arr_Data = Arr_Data.Where(p_objData => p_objData.Id != p_iAuto_ID).ToList();
            Dic_Data_ID.Remove(p_iAuto_ID);
        }
    }

    public static PricingRule? Get_Data_By_ID(Guid p_iAuto_ID)
    {
        lock (Obj_Lock)
        {
            return Dic_Data_ID.TryGetValue(p_iAuto_ID, out var v_objData) ? v_objData : null;
        }
    }

    public static List<PricingRule> List_Data()
    {
        lock (Obj_Lock)
        {
            return Arr_Data
                .OrderBy(p_objData => p_objData.PaperSize)
                .ThenBy(p_objData => p_objData.ColorMode)
                .ThenBy(p_objData => p_objData.PrintSide)
                .ToList();
        }
    }
}
