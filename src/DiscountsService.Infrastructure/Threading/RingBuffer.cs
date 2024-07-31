using System.Collections.Concurrent;

namespace DiscountsService.Infrastructure.Threading;

public class RingBuffer<T>
{
    public event Action<T>? BufferFull; // Event to notify when the buffer is full
    
    private readonly ConcurrentQueue<T> _queue;
    private readonly SemaphoreSlim _signal;
    private readonly uint _capacity;

    public RingBuffer(uint capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be greater than 0", nameof(capacity));
        _queue = new ConcurrentQueue<T>();
        _signal = new SemaphoreSlim(0);
        
        _capacity = capacity;
    }

    public void Enqueue(T item)
    {
        // Check if the queue is full
        if (_queue.Count >= _capacity)
        {
            // Trigger the BufferFull event and pass the item to the event handler
            BufferFull?.Invoke(item);
            return;
        }
        
        // Enqueue the new item and release a semaphore slot
        _queue.Enqueue(item);
        _signal.Release();
    }

    public async Task<T?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        // Wait asynchronously for an item to be available
        await _signal.WaitAsync(cancellationToken).ConfigureAwait(false);

        // Dequeue the item
        _queue.TryDequeue(out var item);
        return item;
    }
}
