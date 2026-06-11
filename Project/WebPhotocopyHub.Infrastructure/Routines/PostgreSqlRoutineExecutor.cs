using System.Data;
using Npgsql;
using WebPhotocopyHub.DataAccess.Configuration;

namespace WebPhotocopyHub.DataAccess.Routines;

public sealed class PostgreSqlRoutineExecutor : IWebPhotocopyHubRoutineExecutor
{
    private readonly IWebPhotocopyHubConnectionStringProvider _connectionStringProvider;
    private readonly IWebPhotocopyHubRoutineCatalog _routineCatalog;

    public PostgreSqlRoutineExecutor(
        IWebPhotocopyHubConnectionStringProvider connectionStringProvider,
        IWebPhotocopyHubRoutineCatalog routineCatalog)
    {
        _connectionStringProvider = connectionStringProvider;
        _routineCatalog = routineCatalog;
    }

    public async Task<DataTable> Fill_Data_Table_Async(
        string p_strRoutineName,
        IReadOnlyList<NpgsqlParameter>? p_arrParameter = null,
        CancellationToken p_objCancellationToken = default)
    {
        await using var v_objConnection = new NpgsqlConnection(_connectionStringProvider.Get_Connection_String());
        await v_objConnection.OpenAsync(p_objCancellationToken);

        await using var v_objCommand = Create_Command(v_objConnection, null, p_strRoutineName, p_arrParameter);
        return await Fill_Data_Table_Internal_Async(v_objCommand, p_objCancellationToken);
    }

    public async Task<DataTable> Fill_Data_Table_Async(
        NpgsqlConnection p_objConnection,
        NpgsqlTransaction p_objTransaction,
        string p_strRoutineName,
        IReadOnlyList<NpgsqlParameter>? p_arrParameter = null,
        CancellationToken p_objCancellationToken = default)
    {
        await using var v_objCommand = Create_Command(p_objConnection, p_objTransaction, p_strRoutineName, p_arrParameter);
        return await Fill_Data_Table_Internal_Async(v_objCommand, p_objCancellationToken);
    }

    public async Task<T?> Execute_Scalar_Async<T>(
        string p_strRoutineName,
        IReadOnlyList<NpgsqlParameter>? p_arrParameter = null,
        CancellationToken p_objCancellationToken = default)
    {
        await using var v_objConnection = new NpgsqlConnection(_connectionStringProvider.Get_Connection_String());
        await v_objConnection.OpenAsync(p_objCancellationToken);

        await using var v_objCommand = Create_Command(v_objConnection, null, p_strRoutineName, p_arrParameter);
        return Convert_Scalar_Value<T>(await v_objCommand.ExecuteScalarAsync(p_objCancellationToken));
    }

    public async Task<T?> Execute_Scalar_Async<T>(
        NpgsqlConnection p_objConnection,
        NpgsqlTransaction p_objTransaction,
        string p_strRoutineName,
        IReadOnlyList<NpgsqlParameter>? p_arrParameter = null,
        CancellationToken p_objCancellationToken = default)
    {
        await using var v_objCommand = Create_Command(p_objConnection, p_objTransaction, p_strRoutineName, p_arrParameter);
        return Convert_Scalar_Value<T>(await v_objCommand.ExecuteScalarAsync(p_objCancellationToken));
    }

    public async Task<int> Execute_Non_Query_Async(
        string p_strRoutineName,
        IReadOnlyList<NpgsqlParameter>? p_arrParameter = null,
        CancellationToken p_objCancellationToken = default)
    {
        await using var v_objConnection = new NpgsqlConnection(_connectionStringProvider.Get_Connection_String());
        await v_objConnection.OpenAsync(p_objCancellationToken);

        await using var v_objCommand = Create_Command(v_objConnection, null, p_strRoutineName, p_arrParameter);
        return await v_objCommand.ExecuteNonQueryAsync(p_objCancellationToken);
    }

    public async Task<int> Execute_Non_Query_Async(
        NpgsqlConnection p_objConnection,
        NpgsqlTransaction p_objTransaction,
        string p_strRoutineName,
        IReadOnlyList<NpgsqlParameter>? p_arrParameter = null,
        CancellationToken p_objCancellationToken = default)
    {
        await using var v_objCommand = Create_Command(p_objConnection, p_objTransaction, p_strRoutineName, p_arrParameter);
        return await v_objCommand.ExecuteNonQueryAsync(p_objCancellationToken);
    }

    private NpgsqlCommand Create_Command(
        NpgsqlConnection p_objConnection,
        NpgsqlTransaction? p_objTransaction,
        string p_strRoutineName,
        IReadOnlyList<NpgsqlParameter>? p_arrParameter)
    {
        if (!_routineCatalog.IsAllowed(p_strRoutineName))
        {
            throw new InvalidOperationException($"Routine is not registered in WebPhotocopyHub whitelist: {p_strRoutineName}");
        }

        var v_objCommand = new NpgsqlCommand(p_strRoutineName, p_objConnection)
        {
            CommandType = CommandType.StoredProcedure
        };

        if (p_objTransaction is not null)
        {
            v_objCommand.Transaction = p_objTransaction;
        }

        if (p_arrParameter is not null)
        {
            foreach (var v_objParameter in p_arrParameter)
            {
                v_objCommand.Parameters.Add(v_objParameter);
            }
        }

        return v_objCommand;
    }

    private static async Task<DataTable> Fill_Data_Table_Internal_Async(
        NpgsqlCommand p_objCommand,
        CancellationToken p_objCancellationToken)
    {
        var v_dtResult = new DataTable();
        await using var v_objReader = await p_objCommand.ExecuteReaderAsync(p_objCancellationToken);
        v_dtResult.Load(v_objReader);
        return v_dtResult;
    }

    private static T? Convert_Scalar_Value<T>(object? p_objValue)
    {
        if (p_objValue is null || p_objValue is DBNull)
        {
            return default;
        }

        if (p_objValue is T v_objTypedValue)
        {
            return v_objTypedValue;
        }

        return (T)Convert.ChangeType(p_objValue, typeof(T));
    }
}
