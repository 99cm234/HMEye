namespace HMEye.Twincat.Cache.PlcCache
{
	/// <summary>
	/// Defines a cache for PLC data, providing methods to read, write, and manage cache items with error handling and polling.
	/// </summary>
	public interface IPlcCache : IHostedService, IDisposable
	{
		/// <summary>
		/// Gets a value indicating whether the cache is in an error state.
		/// </summary>
		bool Error { get; }

		/// <summary>
		/// Gets the last error message, if any.
		/// </summary>
		string ErrorMessage { get; }

		/// <summary>
		/// Gets the timestamp of the last error.
		/// </summary>
		DateTime LastErrorTime { get; }

		/// <summary>
		/// Reads a cached value for the specified address.
		/// </summary>
		/// <typeparam name="T">The type of the value to read.</typeparam>
		/// <param name="address">The address of the variable to read.</param>
		/// <returns>The cached value if available, or default if an error occurs.</returns>
		T? ReadCached<T>(string address);

		/// <summary>
		/// Reads a value immediately from the PLC for the specified address.
		/// </summary>
		/// <typeparam name="T">The type of the value to read.</typeparam>
		/// <param name="address">The address of the variable to read.</param>
		/// <returns>A task that returns the value if successful, or default if an error occurs.</returns>
		Task<T?> ReadImmediateAsync<T>(string address);

		/// <summary>
		/// Queues a write operation for the specified address and value.
		/// </summary>
		/// <typeparam name="T">The type of the value to write.</typeparam>
		/// <param name="address">The address of the variable to write.</param>
		/// <param name="value">The value to write.</param>
		/// <exception cref="InvalidOperationException">Thrown if the write operation cannot be queued.</exception>
		void WriteQueue<T>(string address, T value)
			where T : notnull;

		/// <summary>
		/// Writes a value immediately to the PLC for the specified address.
		/// </summary>
		/// <typeparam name="T">The type of the value to write.</typeparam>
		/// <param name="address">The address of the variable to write.</param>
		/// <param name="value">The value to write.</param>
		/// <returns>A task representing the asynchronous write operation.</returns>
		/// <exception cref="InvalidOperationException">Thrown if the write operation fails.</exception>
		Task WriteImmediateAsync<T>(string address, T value)
			where T : notnull;

		/// <summary>
		/// Resets the error state of the cache, resuming polling if it was disabled.
		/// </summary>
		void ResetError();

		/// <summary>
		/// Attempts to read a cached value for the specified address.
		/// </summary>
		/// <typeparam name="T">The type of the value to read.</typeparam>
		/// <param name="address">The address of the variable to read.</param>
		/// <returns>A <see cref="PlcCacheReadResult{T}"/> containing the cached value or error information.</returns>
		PlcCacheReadResult<T> TryReadCached<T>(string address);

		/// <summary>
		/// Attempts to read a value immediately from the PLC for the specified address.
		/// </summary>
		/// <typeparam name="T">The type of the value to read.</typeparam>
		/// <param name="address">The address of the variable to read.</param>
		/// <returns>A task that returns a <see cref="PlcCacheReadResult{T}"/> containing the value or error information.</returns>
		Task<PlcCacheReadResult<T>> TryReadImmediateAsync<T>(string address);

		/// <summary>
		/// Attempts to write a value immediately to the PLC for the specified address.
		/// </summary>
		/// <typeparam name="T">The type of the value to write.</typeparam>
		/// <param name="address">The address of the variable to write.</param>
		/// <param name="value">The value to write.</param>
		/// <returns>A task that returns a <see cref="PlcCacheWriteResult"/> indicating the success or failure of the operation.</returns>
		Task<PlcCacheWriteResult> TryWriteImmediateAsync<T>(string address, T value)
			where T : notnull;

		/// <summary>
		/// Attempts to queue a write operation for the specified address and value.
		/// </summary>
		/// <typeparam name="T">The type of the value to write.</typeparam>
		/// <param name="address">The address of the variable to write.</param>
		/// <param name="value">The value to write.</param>
		/// <returns>A <see cref="PlcCacheWriteResult"/> indicating the success or failure of queuing the operation.</returns>
		PlcCacheWriteResult TryWriteQueue<T>(string address, T value)
			where T : notnull;

		/// <summary>
		/// Adds a new cache item to the cache based on the provided configuration.
		/// </summary>
		/// <param name="config">The configuration for the new cache item.</param>
		/// <exception cref="InvalidOperationException">Thrown if a cache item with the same address already exists.</exception>
		/// <exception cref="Exception">Thrown if the cache item cannot be created due to invalid configuration or other errors.</exception>
		void AddCacheItem(PlcCacheItemConfig config);

		/// <summary>
		/// Removes a cache item from the cache using its address.
		/// </summary>
		/// <param name="address">The address of the cache item to remove.</param>
		void RemoveCacheItem(string address);
	}
}