namespace Hexalith.Projects.E2E.Fixtures;

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>Reads the fixture-owned store through RESP without installing host-side Redis tools.</summary>
internal sealed class RedisFixtureClient(string address)
{
    internal async Task<string> ReadAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        if (!IPEndPoint.TryParse(address, out IPEndPoint? endpoint) || !IPAddress.IsLoopback(endpoint.Address)
            || endpoint.Port is 0 or 6379)
            throw new InvalidOperationException("Fixture observation requires its owned loopback Redis port.");
        if (arguments.Count == 0 || arguments[0] is not ("EVAL" or "GET" or "HGET" or "HMGET" or "KEYS"))
            throw new InvalidOperationException("Fixture Redis observation refused an unsupported command.");
        using TcpClient client = new();
        await client.ConnectAsync(endpoint.Address, endpoint.Port, cancellationToken).ConfigureAwait(false);
        using NetworkStream stream = client.GetStream();
        using MemoryStream command = new();
        byte[] start = Encoding.ASCII.GetBytes("*" + arguments.Count.ToString(CultureInfo.InvariantCulture) + "\r\n");
        command.Write(start);
        foreach (string argument in arguments)
        {
            byte[] value = Encoding.UTF8.GetBytes(argument);
            command.Write(Encoding.ASCII.GetBytes("$" + value.Length.ToString(CultureInfo.InvariantCulture) + "\r\n"));
            command.Write(value); command.Write("\r\n"u8);
        }
        await stream.WriteAsync(command.ToArray(), cancellationToken).ConfigureAwait(false);
        string header = await LineAsync(stream, cancellationToken).ConfigureAwait(false);
        if (header.StartsWith('-')) throw new InvalidOperationException("Fixture Redis observation was refused.");
        if (!header.StartsWith('$') || !int.TryParse(header.AsSpan(1), out int length) || length < 0 || length > 16 * 1024 * 1024)
            throw new InvalidOperationException("Fixture Redis observation returned an invalid bulk value.");
        byte[] payload = new byte[length + 2];
        await stream.ReadExactlyAsync(payload, cancellationToken).ConfigureAwait(false);
        if (payload[^2] != '\r' || payload[^1] != '\n') throw new InvalidOperationException("Malformed fixture Redis response.");
        return Encoding.UTF8.GetString(payload, 0, length);
    }

    private static async Task<string> LineAsync(Stream stream, CancellationToken cancellationToken)
    {
        using MemoryStream value = new();
        byte[] octet = new byte[1];
        while (value.Length < 128)
        {
            await stream.ReadExactlyAsync(octet, cancellationToken).ConfigureAwait(false);
            if (octet[0] == '\n')
            {
                byte[] bytes = value.ToArray();
                if (bytes.Length == 0 || bytes[^1] != '\r') throw new InvalidOperationException("Malformed Redis line.");
                return Encoding.ASCII.GetString(bytes, 0, bytes.Length - 1);
            }
            value.WriteByte(octet[0]);
        }
        throw new InvalidOperationException("Fixture Redis response line exceeds its bound.");
    }
}
