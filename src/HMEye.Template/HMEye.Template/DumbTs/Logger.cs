using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace HMEye.DumbTs;

/// <summary>
/// Provides logging and batch storage for time series data points in DumbTs.
/// </summary>
/// <remarks>
/// Handles numeric, boolean, and text data points. Batches and writes to the database at a configurable interval.
/// </remarks>
public class DumbTsLogger : IDisposable
{
	private readonly IDbContextFactory<DumbTsDbContext> _dbContextFactory;
	private readonly ILogger<DumbTsLogger> _logger;
	private readonly Timer _timer;
	private readonly ConcurrentQueue<DataPointBase> _dataQueue = new();
	private readonly int _batchSize = 100;
	private bool _disposed;

	/// <summary>
	/// Occurs when a new numeric data point is added to the batch for saving.
	/// </summary>
	/// <remarks>
	/// Event fires as each numeric data point is queued for database storage, before the batch is saved.
	/// Use <see cref="OnNewData"/> to be notified after the entire batch is saved to the database.
	/// </remarks>
	public event EventHandler<NumericDataPoint>? OnNewNumericDataPoint;

	/// <summary>
	/// Occurs when a new boolean data point is added to the batch for saving.
	/// </summary>
	/// <remarks>
	/// Event fires as each boolean data point is queued for database storage, before the batch is saved.
	/// Use <see cref="OnNewData"/> to be notified after the entire batch is saved to the database.
	/// </remarks>
	public event EventHandler<BooleanDataPoint>? OnNewBooleanDataPoint;

	/// <summary>
	/// Occurs when a new text data point is added to the batch for saving.
	/// </summary>
	/// <remarks>
	/// Event fires as each text data point is queued for database storage, before the batch is saved.
	/// Use <see cref="OnNewData"/> to be notified after the entire batch is saved to the database.
	/// </remarks>
	public event EventHandler<TextDataPoint>? OnNewTextDataPoint;

	/// <summary>
	/// Occurs after a batch of data points has been successfully saved to the database.
	/// </summary>
	/// <remarks>
	/// Event fires once per batch after all points are saved and the transaction is committed.
	/// Use type-specific events (<see cref="OnNewNumericDataPoint"/>, <see cref="OnNewBooleanDataPoint"/>,
	/// <see cref="OnNewTextDataPoint"/>) for notifications about individual data points.
	/// </remarks>
	public event Action? OnNewData;

	public DumbTsLogger(
		IDbContextFactory<DumbTsDbContext> dbContextFactory,
		ILogger<DumbTsLogger> logger,
		TimeSpan writeInterval
	)
	{
		_dbContextFactory = dbContextFactory;
		_logger = logger;
		_timer = new Timer(ProcessQueue, null, TimeSpan.Zero, writeInterval);
	}

	/// <summary>
	/// Adds a numeric data point to the specified series.
	/// </summary>
	/// <remarks>Converts the value to a <see cref="double"/> and enqueues for storage as a decimal(28,8) for
	/// compatibility with and sufficient precision for typical IOT data types.
	/// Ensure that <paramref name="value"/> represents a valid numeric type and is a finite number.</remarks>
	/// <param name="seriesName">The name of the data series the point should be added to.</param>
	/// <param name="value">Value of the data point. Must be a finite number.</param>
	/// <param name="unit">Optional. Unit of measurement (psi, °F, %)</param>
	/// <param name="source">Optional. Note on source of data.</param>
	/// <exception cref="ArgumentException">Thrown if <typeparamref name="T"/> is not a numeric type, or if <paramref name="value"/> is not a finite number.</exception>
	public void AddNumericDataPoint<T>(string seriesName, T value, string? unit = null, string? source = null)
		where T : struct, IConvertible
	{
		if (!IsNumericType<T>())
			throw new ArgumentException("T must be a number!.");

		double doubleValue = Convert.ToDouble(value);
		if (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue))
			throw new ArgumentException("Value must be a finite number.");

		EnqueuePoint(new NumericDataPoint
		{
			SeriesName = seriesName,
			Value = doubleValue,
			Unit = unit,
			Source = source,
			DataType = DataPointType.Numeric,
			OriginalType = typeof(T).Name
		});
	}

	/// <summary>
	/// Adds a boolean data point to the specified series.
	/// </summary>
	/// <remarks>This method enqueues a boolean data point for processing.
	/// <param name="seriesName">The name of the data series the point should be added to.</param>
	/// <param name="value">The boolean value of the data point.</param>
	/// <param name="source">Optional. Note on source of data.</param>
	public void AddBooleanDataPoint(string seriesName, bool value, string? source = null)
	{
		EnqueuePoint(new BooleanDataPoint
		{
			SeriesName = seriesName,
			Value = value,
			Source = source,
			DataType = DataPointType.Boolean
		});
	}

	/// <summary>
	/// Adds a text data point to the specified series.
	/// </summary>
	/// <remarks>This method enqueues a text data point for processing.
	/// <param name="seriesName">The name of the data series the point should be added to.</param>
	/// <param name="value">The string value of the data point.</param>
	/// <param name="source">Optional. Note on source of data.</param>
	public void AddTextDataPoint(string seriesName, string value, string? source = null)
	{
		EnqueuePoint(new TextDataPoint
		{
			SeriesName = seriesName,
			Value = value,
			Source = source,
			DataType = DataPointType.Text
		});
	}

	private void EnqueuePoint(DataPointBase point)
	{
		if (_disposed) throw new ObjectDisposedException(nameof(DumbTsLogger));
		point.Timestamp = DateTime.UtcNow;
		_dataQueue.Enqueue(point);
	}

	private async void ProcessQueue(object? state)
	{
		if (_dataQueue.IsEmpty || _disposed)
			return;

		var pointsToAdd = new List<DataPointBase>();
		while (_dataQueue.TryDequeue(out var point) && pointsToAdd.Count < _batchSize)
		{
			pointsToAdd.Add(point);
		}

		if (pointsToAdd.Count == 0)
			return;

		await using var writeContext = await _dbContextFactory.CreateDbContextAsync();

		try
		{
			await using var transaction = await writeContext.Database.BeginTransactionAsync();
			foreach (var point in pointsToAdd)
			{
				switch (point)
				{
					case NumericDataPoint numeric:
						writeContext.NumericDataPoints.Add(numeric);
						OnNewNumericDataPoint?.Invoke(this, numeric);
						break;
					case BooleanDataPoint boolean:
						writeContext.BooleanDataPoints.Add(boolean);
						OnNewBooleanDataPoint?.Invoke(this, boolean);
						break;
					case TextDataPoint text:
						writeContext.TextDataPoints.Add(text);
						OnNewTextDataPoint?.Invoke(this, text);
						break;
				}
			}
			await writeContext.SaveChangesAsync();
			await transaction.CommitAsync();

			OnNewData?.Invoke();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error saving DumbTs data");
			foreach (var point in pointsToAdd)
			{
				_dataQueue.Enqueue(point);
			}
		}
	}

	/// <summary>
	/// Asynchronously retrieves a collection of DumbTs data points filtered by the specified criteria.
	/// </summary>
	/// <remarks>Queries database for points that match the specified <paramref name="seriesName"/> and optional filters.
	/// Results are ordered by timestamp in descending order. If no filters are
	/// provided, all data points for the specified series are returned.</remarks>
	/// <typeparam name="T">The type of data points to retrieve. Must derive from <see cref="DataPointBase"/>.</typeparam>
	/// <param name="seriesName">Required. Name of the time series to query.</param>
	/// <param name="from">Optional. Start date and time for filtering the data points.</param>
	/// <param name="to">Optional. End date and time for filtering the data points.</param>
	/// <param name="limit">Optional. Maximum number of data points to retrieve.</param>
	/// <param name="cancellationToken"> Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains a list of data points of type
	/// <typeparamref name="T"/>.</returns>
	/// <exception cref="ObjectDisposedException">Thrown if the <see cref="DumbTsLogger"/> instance has been disposed.</exception>
	/// <exception cref="Exception">Thrown if an error occurs during data retrieval, with details logged.</exception>
	public async Task<List<T>> GetDataAsync<T>(
		string seriesName,
		DateTime? from = null,
		DateTime? to = null,
		int? limit = null,
		CancellationToken cancellationToken = default
	) where T : DataPointBase
	{
		if (_disposed)
			throw new ObjectDisposedException(nameof(DumbTsLogger));

		await using var readContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

		try
		{
			IQueryable<T> query = readContext.Set<T>()
				.AsNoTracking()
				.Where(p => p.SeriesName == seriesName)
				.OrderByDescending(p => p.Timestamp);

			if (from.HasValue)
				query = query.Where(p => p.Timestamp >= from.Value.ToUniversalTime());

			if (to.HasValue)
				query = query.Where(p => p.Timestamp <= to.Value.ToUniversalTime());

			if (limit.HasValue)
				query = query.Take(limit.Value);

			return await query.ToListAsync(cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving DumbTs data");
			throw;
		}
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		_timer?.Dispose();

		// Flush remaining data before disposal
		ProcessQueue(null);

		OnNewData = null;
		OnNewNumericDataPoint = null;
		OnNewBooleanDataPoint = null;
		OnNewTextDataPoint = null;
		GC.SuppressFinalize(this);
		return;
	}

	private static bool IsNumericType<T>()
	{
		var type = typeof(T);
		return type.IsPrimitive && type != typeof(char) && type != typeof(bool);
	}
}