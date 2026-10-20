using System.Data;
using System.Globalization;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Forecasting;
using SalesForecastSystem.Core.Interfaces.Services;
using SalesForecastSystem.Infrastructure.Data;

namespace SalesForecastSystem.Infrastructure.Services;

public sealed class ForecastService(
    AppDbContext context,
    IForecastDataService dataService,
    IConfiguration configuration,
    IWebHostEnvironment environment) : IForecastService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ServiceResult<ForecastRunResponse>> RunAsync(ForecastRunRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var datasetRequest = new ForecastDataQuery
        {
            ProductId = request.ProductId, WarehouseId = request.WarehouseId,
            FromDate = request.FromDate, ToDate = request.ToDate,
            ValidationPercentage = request.ValidationPercentage
        };
        var datasetResult = await dataService.GetDailyDatasetAsync(datasetRequest, cancellationToken);
        if (!datasetResult.IsSuccess)
            return ServiceResult<ForecastRunResponse>.Failure(datasetResult.Error!.Type, datasetResult.Error.Message, datasetResult.Error.FieldName);
        var dataset = datasetResult.Value!;

        if (dataset.TotalQuantitySold <= 0)
        {
            return ServiceResult<ForecastRunResponse>.Failure(
                ServiceErrorType.Validation,
                "Sản phẩm chưa có phát sinh giao dịch bán hàng hoàn tất (Completed) trong khoảng thời gian đã chọn để chạy mô hình dự báo.",
                nameof(request.ProductId));
        }

        var hasTrainingDemand = dataset.Points.Any(p =>
            p.DatasetSplit == ForecastDatasetSplits.Training &&
            (p.DataStatus == ForecastDataStatuses.Sold || p.DataStatus == ForecastDataStatuses.NoSale));

        if (!hasTrainingDemand)
        {
            return ServiceResult<ForecastRunResponse>.Failure(
                ServiceErrorType.Validation,
                "Tập dữ liệu huấn luyện (Training) không có đủ dữ liệu quan sát nhu cầu bán hàng (cần ít nhất một ngày có phát sinh bán hàng hoặc còn tồn kho sẵn sàng bán).",
                nameof(request.FromDate));
        }

        var hasValidationDemand = dataset.Points.Any(p =>
            p.DatasetSplit == ForecastDatasetSplits.Validation &&
            (p.DataStatus == ForecastDataStatuses.Sold || p.DataStatus == ForecastDataStatuses.NoSale));

        if (!hasValidationDemand)
        {
            return ServiceResult<ForecastRunResponse>.Failure(
                ServiceErrorType.Validation,
                "Tập dữ liệu kiểm định (Validation) không có đủ dữ liệu quan sát nhu cầu bán hàng để đánh giá sai số mô hình. Vui lòng mở rộng khoảng thời gian hoặc điều chỉnh tỷ lệ kiểm định.",
                nameof(request.ValidationPercentage));
        }

        var input = new
        {
            productId = dataset.ProductId, warehouseId = dataset.WarehouseId,
            forecastDays = request.ForecastDays,
            points = dataset.Points.Select(point => new
            {
                date = point.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                quantitySold = point.QuantitySold, dataStatus = point.DataStatus, datasetSplit = point.DatasetSplit
            })
        };

        JsonDocument output;
        try { output = await ExecuteModelAsync(input, cancellationToken); }
        catch (InvalidOperationException ex)
        { return ServiceResult<ForecastRunResponse>.Failure(ServiceErrorType.Validation, ex.Message); }
        using (output)
        {
            var root = output.RootElement;
            var model = root.GetProperty("model");
            var modelName = model.GetProperty("name").GetString()!;
            var modelVersion = model.GetProperty("version").GetString()!;
            var forecasts = root.GetProperty("forecasts").EnumerateArray().Select(row => new ForecastValueResponse(
                DateTime.Parse(row.GetProperty("date").GetString()!, CultureInfo.InvariantCulture),
                Decimal(row, "quantity"), Decimal(row, "lowerBound"), Decimal(row, "upperBound"))).ToArray();
            var candidates = root.GetProperty("candidates").EnumerateArray().Select(row => new ForecastCandidateResponse(
                row.GetProperty("name").GetString()!, row.GetProperty("version").GetString()!,
                Decimal(row, "mae"), Decimal(row, "rmse"), row.GetProperty("evaluatedValidationPoints").GetInt32())).ToArray();
            var parameters = JsonSerializer.Serialize(model.GetProperty("parameters"), JsonOptions);
            var diagnostics = JsonSerializer.Serialize(new
            {
                parameters = model.GetProperty("parameters"), candidates,
                evaluatedValidationPoints = root.GetProperty("evaluatedValidationPoints").GetInt32(),
                excludedPointCount = root.GetProperty("excludedPointCount").GetInt32(),
                validationStartDate = root.GetProperty("validationStartDate").GetString(),
                validationEndDate = root.GetProperty("validationEndDate").GetString()
            }, JsonOptions);
            var runId = await SaveRunAsync(request, userId, modelName, modelVersion, parameters, diagnostics, root, forecasts, cancellationToken);
            return ServiceResult<ForecastRunResponse>.Success(ToResponse(runId, request.ProductId!.Value,
                request.WarehouseId!.Value, modelName, modelVersion, root, candidates, forecasts));
        }
    }

    public async Task<ServiceResult<ForecastRunResponse>> GetAsync(long runId, CancellationToken cancellationToken = default)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        int productId, warehouseId; string status, name, version, metadata;
        DateTime trainingStart, trainingEnd, forecastStart, forecastEnd; decimal mae, rmse;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT r.WarehouseId, r.Status, m.Name, m.Version, r.TrainingStartDate, r.TrainingEndDate,
                       r.ForecastStartDate, r.ForecastEndDate, r.MAE, r.RMSE, r.ModelDiagnostics,
                       (SELECT TOP (1) x.ProductId FROM dbo.ForecastResults x WHERE x.ForecastRunId=r.ForecastRunId)
                FROM dbo.ForecastRuns r JOIN dbo.ForecastModels m ON m.ForecastModelId=r.ForecastModelId WHERE r.ForecastRunId=@id;
                """;
            AddParameter(command, "@id", runId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return ServiceResult<ForecastRunResponse>.Failure(ServiceErrorType.NotFound, "Forecast run was not found.");
            warehouseId = reader.GetInt32(0); status = reader.GetString(1); name = reader.GetString(2); version = reader.GetString(3);
            trainingStart = reader.GetDateTime(4); trainingEnd = reader.GetDateTime(5); forecastStart = reader.GetDateTime(6); forecastEnd = reader.GetDateTime(7);
            mae = reader.IsDBNull(8) ? 0 : reader.GetDecimal(8); rmse = reader.IsDBNull(9) ? 0 : reader.GetDecimal(9);
            metadata = reader.IsDBNull(10) ? "{}" : reader.GetString(10); productId = reader.GetInt32(11);
        }
        var forecasts = new List<ForecastValueResponse>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT ForecastDate, ForecastQuantity, LowerBound, UpperBound FROM dbo.ForecastResults WHERE ForecastRunId=@id AND ProductId=@productId ORDER BY ForecastDate;";
            AddParameter(command, "@id", runId); AddParameter(command, "@productId", productId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) forecasts.Add(new ForecastValueResponse(reader.GetDateTime(0), reader.GetDecimal(1), reader.GetDecimal(2), reader.GetDecimal(3)));
        }
        using var metadataDocument = JsonDocument.Parse(metadata);
        var details = metadataDocument.RootElement;
        var candidates = details.TryGetProperty("candidates", out var candidateRows)
            ? candidateRows.EnumerateArray().Select(row => new ForecastCandidateResponse(
                row.GetProperty("name").GetString()!, row.GetProperty("version").GetString()!,
                Decimal(row, "mae"), Decimal(row, "rmse"), row.GetProperty("evaluatedValidationPoints").GetInt32())).ToArray()
            : [];
        var validationStart = details.TryGetProperty("validationStartDate", out var vs) && vs.ValueKind == JsonValueKind.String
            ? DateTime.Parse(vs.GetString()!, CultureInfo.InvariantCulture) : trainingEnd.AddDays(1);
        var validationEnd = details.TryGetProperty("validationEndDate", out var ve) && ve.ValueKind == JsonValueKind.String
            ? DateTime.Parse(ve.GetString()!, CultureInfo.InvariantCulture) : forecastStart.AddDays(-1);
        var evaluated = details.TryGetProperty("evaluatedValidationPoints", out var ep) ? ep.GetInt32() : 0;
        var excluded = details.TryGetProperty("excludedPointCount", out var xp) ? xp.GetInt32() : 0;
        var response = new ForecastRunResponse(runId, productId, warehouseId, status, name, version, trainingStart, trainingEnd,
            validationStart, validationEnd, forecastStart, forecastEnd, mae, rmse, evaluated, excluded, candidates, forecasts);
        return ServiceResult<ForecastRunResponse>.Success(response);
    }

    private async Task<JsonDocument> ExecuteModelAsync(object input, CancellationToken cancellationToken)
    {
        var script = Path.Combine(AppContext.BaseDirectory, "SalesForecastSystem.ML", "main.py");
        if (!File.Exists(script)) script = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "SalesForecastSystem.ML", "main.py"));
        if (!File.Exists(script)) throw new InvalidOperationException("Forecast model files are missing from the application deployment.");
        var python = configuration["Forecasting:PythonExecutable"] ?? "python";
        var tempBase = Path.Combine(Path.GetTempPath(), "SalesForecast", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempBase);
        var inputPath = Path.Combine(tempBase, "request.json");
        var outputPath = Path.Combine(tempBase, "result.json");
        try
        {
            await File.WriteAllTextAsync(inputPath, JsonSerializer.Serialize(input, JsonOptions), cancellationToken);
            var start = new ProcessStartInfo(python)
            {
                UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true,
                CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(script)!
            };
            start.ArgumentList.Add(script); start.ArgumentList.Add("--input"); start.ArgumentList.Add(inputPath);
            start.ArgumentList.Add("--output"); start.ArgumentList.Add(outputPath);
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the Python forecast process.");
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var error = await stderrTask;
            if (process.ExitCode != 0 || !File.Exists(outputPath))
            {
                var cleanError = error.Trim();
                try
                {
                    using var errDoc = JsonDocument.Parse(cleanError);
                    if (errDoc.RootElement.TryGetProperty("error", out var errProp) && errProp.ValueKind == JsonValueKind.String)
                    {
                        cleanError = errProp.GetString()!;
                    }
                }
                catch (JsonException) { }
                throw new InvalidOperationException($"Forecast model failed: {cleanError}".TrimEnd());
            }
            return JsonDocument.Parse(await File.ReadAllTextAsync(outputPath, cancellationToken));
        }
        catch (System.ComponentModel.Win32Exception ex)
        { throw new InvalidOperationException($"Python executable '{python}' is unavailable. Configure Forecasting:PythonExecutable.", ex); }
        finally { try { Directory.Delete(tempBase, true); } catch (IOException) { } }
    }

    private async Task<long> SaveRunAsync(ForecastRunRequest request, int userId, string modelName, string modelVersion,
        string parameters, string diagnostics, JsonElement root, IReadOnlyList<ForecastValueResponse> forecasts, CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var modelCommand = connection.CreateCommand(); modelCommand.Transaction = transaction;
            modelCommand.CommandText = """
                MERGE dbo.ForecastModels WITH (HOLDLOCK) AS target
                USING (SELECT @name AS Name, @version AS Version) AS source ON target.Name=source.Name AND target.Version=source.Version
                WHEN MATCHED THEN UPDATE SET Parameters=@parameters
                WHEN NOT MATCHED THEN INSERT (Name, Version, Parameters, Description) VALUES (@name, @version, @parameters, N'Baseline demand forecast selected by walk-forward validation')
                OUTPUT inserted.ForecastModelId;
                """;
            AddParameter(modelCommand, "@name", modelName); AddParameter(modelCommand, "@version", modelVersion); AddParameter(modelCommand, "@parameters", parameters);
            var modelId = Convert.ToInt32(await modelCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
            await using var runCommand = connection.CreateCommand(); runCommand.Transaction = transaction;
            runCommand.CommandText = """
                INSERT dbo.ForecastRuns(ForecastModelId, WarehouseId, RequestedByUserId, TrainingStartDate, TrainingEndDate, ForecastStartDate, ForecastEndDate, Status, MAE, RMSE, CompletedAt, ModelDiagnostics)
                OUTPUT inserted.ForecastRunId VALUES(@modelId,@warehouseId,@userId,@trainingStart,@trainingEnd,@forecastStart,@forecastEnd,'Completed',@mae,@rmse,SYSUTCDATETIME(),@diagnostics);
                """;
            AddParameter(runCommand, "@modelId", modelId); AddParameter(runCommand, "@warehouseId", request.WarehouseId!.Value); AddParameter(runCommand, "@userId", userId);
            AddParameter(runCommand, "@trainingStart", Date(root, "trainingStartDate")); AddParameter(runCommand, "@trainingEnd", Date(root, "trainingEndDate"));
            AddParameter(runCommand, "@forecastStart", Date(root, "forecastStartDate")); AddParameter(runCommand, "@forecastEnd", Date(root, "forecastEndDate"));
            AddParameter(runCommand, "@mae", Decimal(root, "mae")); AddParameter(runCommand, "@rmse", Decimal(root, "rmse"));
            AddParameter(runCommand, "@diagnostics", diagnostics);
            var runId = Convert.ToInt64(await runCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
            foreach (var forecast in forecasts)
            {
                await using var resultCommand = connection.CreateCommand(); resultCommand.Transaction = transaction;
                resultCommand.CommandText = """
                    INSERT dbo.ForecastResults(ForecastRunId, ProductId, ForecastDate, ForecastQuantity, ForecastRevenue, LowerBound, UpperBound)
                    SELECT @runId,@productId,@date,@quantity,ROUND(@quantity*p.SalePrice,2),@lower,@upper FROM dbo.Products p WHERE p.ProductId=@productId;
                    """;
                AddParameter(resultCommand, "@runId", runId); AddParameter(resultCommand, "@productId", request.ProductId!.Value);
                AddParameter(resultCommand, "@date", forecast.Date); AddParameter(resultCommand, "@quantity", forecast.Quantity);
                AddParameter(resultCommand, "@lower", forecast.LowerBound); AddParameter(resultCommand, "@upper", forecast.UpperBound);
                await resultCommand.ExecuteNonQueryAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
            return runId;
        }
        catch { await transaction.RollbackAsync(cancellationToken); throw; }
    }

    private static ForecastRunResponse ToResponse(long id, int productId, int warehouseId, string modelName, string modelVersion,
        JsonElement root, IReadOnlyList<ForecastCandidateResponse> candidates, IReadOnlyList<ForecastValueResponse> forecasts) =>
        new(id, productId, warehouseId, "Completed", modelName, modelVersion,
            Date(root, "trainingStartDate"), Date(root, "trainingEndDate"), Date(root, "validationStartDate"), Date(root, "validationEndDate"),
            Date(root, "forecastStartDate"), Date(root, "forecastEndDate"), Decimal(root, "mae"), Decimal(root, "rmse"),
            root.GetProperty("evaluatedValidationPoints").GetInt32(), root.GetProperty("excludedPointCount").GetInt32(), candidates, forecasts);

    private static DateTime Date(JsonElement element, string name) => DateTime.Parse(element.GetProperty(name).GetString()!, CultureInfo.InvariantCulture);
    private static decimal Decimal(JsonElement element, string name) => element.GetProperty(name).GetDecimal();
    private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
    { var parameter = command.CreateParameter(); parameter.ParameterName = name; parameter.Value = value; command.Parameters.Add(parameter); }
}
