using System.Threading.Channels;

namespace PrivilegeDesk.ExpiryService;

// Channel<T> is a built-in, thread-safe queue.
// One producer writes request IDs; several consumers read them.
public class ExpiryQueue
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>(
        new UnboundedChannelOptions
        {
            SingleWriter = true,
            SingleReader = false
        });

    public ChannelWriter<int> Writer => _channel.Writer;
    public ChannelReader<int> Reader => _channel.Reader;
}
