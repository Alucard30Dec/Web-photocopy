using System.Data;
using Npgsql;

namespace WebPhotocopyHub.DataAccess.Routines;

public interface IWebPhotocopyHubRoutineExecutor
{
    Task<DataTable> Fill_Data_Table_Async(
        string p_strRoutineName,
        IReadOnlyList<NpgsqlParameter>? p_arrParameter = null,
        CancellationToken p_objCancellationToken = default);

    Task<DataTable> Fill_Data_Table_Async(
        NpgsqlConnection p_objConnection,
        NpgsqlTransaction p_objTransaction,
        string p_strRoutineName,
        IReadOnlyList<NpgsqlParameter>? p_arrParameter = null,
        CancellationToken p_objCancellationToken = default);

    Task<T?> Execute_Scalar_Async<T>(
        string p_strRoutineName,
        IReadOnlyList<NpgsqlParameter>? p_arrParameter = null,
        CancellationToken p_objCancellationToken = default);

    Task<T?> Execute_Scalar_Async<T>(
        NpgsqlConnection p_objConnection,
        NpgsqlTransaction p_objTransaction,
        string p_strRoutineName,
        IReadOnlyList<NpgsqlParameter>? p_arrParameter = null,
        CancellationToken p_objCancellationToken = default);

    Task<int> Execute_Non_Query_Async(
        string p_strRoutineName,
        IReadOnlyList<NpgsqlParameter>? p_arrParameter = null,
        CancellationToken p_objCancellationToken = default);

    Task<int> Execute_Non_Query_Async(
        NpgsqlConnection p_objConnection,
        NpgsqlTransaction p_objTransaction,
        string p_strRoutineName,
        IReadOnlyList<NpgsqlParameter>? p_arrParameter = null,
        CancellationToken p_objCancellationToken = default);
}
