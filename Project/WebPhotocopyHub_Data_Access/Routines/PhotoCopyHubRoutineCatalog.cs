namespace PhotoCopyHub.DataAccess.Routines;

public interface IPhotoCopyHubRoutineCatalog
{
    bool IsAllowed(string p_strRoutineName);
    IReadOnlyCollection<string> ListRoutineNames();
}

public sealed class PhotoCopyHubRoutineCatalog : IPhotoCopyHubRoutineCatalog
{
    private static readonly HashSet<string> Arr_Routine_Name = new(StringComparer.Ordinal)
    {
        FQ_101_USR_sp_sel_List,
        FQ_101_USR_sp_sel_Get_By_ID,
        FQ_110_PRD_sp_sel_List,
        FQ_110_PRD_sp_sel_Get_By_ID,
        FQ_110_PRD_sp_del_Deactivate,
        FQ_111_PRC_sp_sel_List,
        FQ_111_PRC_sp_sel_Get_By_ID,
        FQ_112_SVS_sp_sel_List,
        FQ_112_SVS_sp_sel_Get_By_ID,
        FQ_112_SVS_sp_del_Deactivate,
        FQ_201_WLT_sp_sel_List,
        FQ_201_WLT_sp_sel_List_By_User_ID,
        FQ_202_TOP_sp_sel_List,
        FQ_202_TOP_sp_sel_Get_By_ID,
        FQ_301_PRJ_sp_sel_List,
        FQ_301_PRJ_sp_sel_Get_By_ID,
        FQ_401_POR_sp_sel_List,
        FQ_401_POR_sp_sel_Get_By_ID,
        FQ_402_POI_sp_sel_List_By_Order_ID,
        FQ_501_SVO_sp_sel_List,
        FQ_501_SVO_sp_sel_Get_By_ID,
        FQ_601_FIL_sp_sel_List_By_User_ID
    };

    public const string FQ_101_USR_sp_sel_List = "FQ_101_USR_sp_sel_List";
    public const string FQ_101_USR_sp_sel_Get_By_ID = "FQ_101_USR_sp_sel_Get_By_ID";
    public const string FQ_110_PRD_sp_sel_List = "FQ_110_PRD_sp_sel_List";
    public const string FQ_110_PRD_sp_sel_Get_By_ID = "FQ_110_PRD_sp_sel_Get_By_ID";
    public const string FQ_110_PRD_sp_del_Deactivate = "FQ_110_PRD_sp_del_Deactivate";
    public const string FQ_111_PRC_sp_sel_List = "FQ_111_PRC_sp_sel_List";
    public const string FQ_111_PRC_sp_sel_Get_By_ID = "FQ_111_PRC_sp_sel_Get_By_ID";
    public const string FQ_112_SVS_sp_sel_List = "FQ_112_SVS_sp_sel_List";
    public const string FQ_112_SVS_sp_sel_Get_By_ID = "FQ_112_SVS_sp_sel_Get_By_ID";
    public const string FQ_112_SVS_sp_del_Deactivate = "FQ_112_SVS_sp_del_Deactivate";
    public const string FQ_201_WLT_sp_sel_List = "FQ_201_WLT_sp_sel_List";
    public const string FQ_201_WLT_sp_sel_List_By_User_ID = "FQ_201_WLT_sp_sel_List_By_User_ID";
    public const string FQ_202_TOP_sp_sel_List = "FQ_202_TOP_sp_sel_List";
    public const string FQ_202_TOP_sp_sel_Get_By_ID = "FQ_202_TOP_sp_sel_Get_By_ID";
    public const string FQ_301_PRJ_sp_sel_List = "FQ_301_PRJ_sp_sel_List";
    public const string FQ_301_PRJ_sp_sel_Get_By_ID = "FQ_301_PRJ_sp_sel_Get_By_ID";
    public const string FQ_401_POR_sp_sel_List = "FQ_401_POR_sp_sel_List";
    public const string FQ_401_POR_sp_sel_Get_By_ID = "FQ_401_POR_sp_sel_Get_By_ID";
    public const string FQ_402_POI_sp_sel_List_By_Order_ID = "FQ_402_POI_sp_sel_List_By_Order_ID";
    public const string FQ_501_SVO_sp_sel_List = "FQ_501_SVO_sp_sel_List";
    public const string FQ_501_SVO_sp_sel_Get_By_ID = "FQ_501_SVO_sp_sel_Get_By_ID";
    public const string FQ_601_FIL_sp_sel_List_By_User_ID = "FQ_601_FIL_sp_sel_List_By_User_ID";

    public bool IsAllowed(string p_strRoutineName)
    {
        return Arr_Routine_Name.Contains(p_strRoutineName);
    }

    public IReadOnlyCollection<string> ListRoutineNames()
    {
        return Arr_Routine_Name.OrderBy(p_strName => p_strName, StringComparer.Ordinal).ToList();
    }
}
