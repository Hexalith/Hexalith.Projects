using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Win32.SafeHandles;

namespace Hexalith.PrivateCaptureCounter;

internal enum Role { Unsupported, Gateway, Tenants, Projects, ProjectsWorkers, Folders, FoldersWorkers }
internal enum Stage { Whole, ComponentFile, ComponentPolicy, AclFile, SettingsEnumerationSortDictionary, AuthenticationSerialization, ActorBoundary, ResultTail }
internal enum Count { Yielded, NullValue, AuthenticationAccepted, NonAuthenticationDiscarded, SensitiveSegmentExcluded, AuthenticationOtherExcluded }
internal enum AllocationMode { None, SynchronousThread }

// PRIVATE DIAGNOSTIC ONLY. No configuration, request, Activity, logging or privacy-scope API is referenced.
// The sink boundary is closed enums and integers. Original configuration data never enters this type.
internal sealed class CaptureCounters : IDisposable
{
    private const string Directory = "/tmp/story65-capture-counter-private-20261107";
    private const int QueueCapacity = 4096, MaximumRows = 65536;
    private const long MaximumBytes = 8L * 1024 * 1024;
    private static Channel<Row>? Queue;
    private static object? AdmissionGate;
    private static long WindowStart, WindowEnd, RunOrdinal, Ordinal, Admitted, Active, Completed, QueueDropped, ClosedDropped, IoFailures;
    private static int WorkerClosed;
    private static Role SeenRole;
    private readonly Role _role;
    private readonly long _run, _ordinal, _started;
    private readonly long[] _counts = new long[6], _elapsed = new long[8], _allocated = new long[8], _allocationMode = new long[8];
    private int _disposed;

    static CaptureCounters()
    {
        try
        {
            Queue = Channel.CreateBounded<Row>(new BoundedChannelOptions(QueueCapacity) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });
            AdmissionGate = new object();
            new Thread(Drain) { IsBackground = true, Name = "PrivateCaptureCounterDrain" }.Start();
        }
        catch { Interlocked.Increment(ref IoFailures); }
    }

    private CaptureCounters(Role role, long run, long ordinal, long started)
    { _role = role; _run = run; _ordinal = ordinal; _started = started; }

    internal static CaptureCounters? Begin(Role role)
    {
        try
        {
            if (role is < Role.Gateway or > Role.FoldersWorkers || Queue is null || AdmissionGate is not { } gate) return null;
            lock (gate) // Registration only. No original observation executes under this lock.
            {
                long now = Stopwatch.GetTimestamp(), end = Volatile.Read(ref WindowEnd);
                if (WorkerClosed != 0 || now < Volatile.Read(ref WindowStart) || end == 0 || now >= end) return null;
                var capture = new CaptureCounters(role, Volatile.Read(ref RunOrdinal), ++Ordinal, now);
                SeenRole = role; Active++; Admitted++;
                return capture;
            }
        }
        catch { return null; }
    }

    internal void Count(Count counter) { try { if ((int)counter is >= 0 and < 6) _counts[(int)counter]++; } catch { Interlocked.Increment(ref IoFailures); } }
    internal Interval? Stage(Stage stage, AllocationMode allocation = AllocationMode.None)
    { try { return (int)stage is > 0 and < 8 ? new Interval(this, stage, allocation) : null; } catch { return null; } }

    internal sealed class Interval : IDisposable
    {
        private readonly CaptureCounters _owner;
        private readonly Stage _stage;
        private readonly long _started, _allocated;
        private readonly int _thread;
        private readonly AllocationMode _allocation;
        private int _disposed;
        internal Interval(CaptureCounters owner, Stage stage, AllocationMode allocation)
        {
            _owner = owner; _stage = stage; _allocation = allocation;
            _thread = allocation == AllocationMode.SynchronousThread ? Environment.CurrentManagedThreadId : 0;
            _allocated = allocation == AllocationMode.SynchronousThread ? GC.GetAllocatedBytesForCurrentThread() : 0;
            _started = Stopwatch.GetTimestamp();
        }
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            try
            {
                long ended = Stopwatch.GetTimestamp();
                long allocated = _allocation == AllocationMode.SynchronousThread ? GC.GetAllocatedBytesForCurrentThread() : 0;
                int index = (int)_stage;
                _owner._elapsed[index] = ended - _started;
                if (_allocation == AllocationMode.SynchronousThread && _thread == Environment.CurrentManagedThreadId)
                { _owner._allocationMode[index] = 1; _owner._allocated[index] = allocated - _allocated; }
            }
            catch { Interlocked.Increment(ref IoFailures); }
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        try
        {
            _elapsed[0] = Stopwatch.GetTimestamp() - _started; // Stop whole wall time before queue operations.
            for (int i = 0; i < 8; i++)
            {
                if (i != 0 && _elapsed[i] == 0) continue;
                var row = new Row(_role, _run, _ordinal, (Stage)i, _elapsed[i], _allocationMode[i], _allocated[i],
                    i == 4 ? _counts[0] : 0, i == 4 ? _counts[1] : 0, i == 4 ? _counts[2] : 0,
                    i == 4 ? _counts[3] : 0, i == 4 ? _counts[4] : 0, i == 4 ? _counts[5] : 0);
                if (Volatile.Read(ref WorkerClosed) != 0) Interlocked.Increment(ref ClosedDropped);
                else if (Queue?.Writer.TryWrite(row) != true) Interlocked.Increment(ref QueueDropped);
            }
        }
        catch { Interlocked.Increment(ref QueueDropped); }
        finally
        {
            if (AdmissionGate is { } gate) lock (gate) { Completed++; Active--; }
        }
    }

    private readonly record struct Row(Role Role, long Run, long Capture, Stage Stage, long Elapsed, long AllocationMode, long Allocated,
        long Yielded, long NullValue, long AuthenticationAccepted, long NonAuthenticationDiscarded, long SensitiveSegmentExcluded, long AuthenticationOtherExcluded);

    // Linux x64 constants/layout are pinned to the local primary libc headers retained in preparation.
    private const int ReadOnly = 0, NonBlock = 2048, WriteCreateExclusive = 1 | 64 | 128, NoFollow = 131072, DirectoryOnly = 65536, CloseOnExec = 524288;
    [StructLayout(LayoutKind.Explicit, Size = 144)]
    private struct LinuxStat
    {
        [FieldOffset(0)] internal ulong Device;
        [FieldOffset(8)] internal ulong Inode;
        [FieldOffset(24)] internal uint Mode;
        [FieldOffset(28)] internal uint Uid;
        [FieldOffset(48)] internal long Size;
    }
    [DllImport("libc", EntryPoint = "open", SetLastError = true)] private static extern int NativeOpen(string path, int flags);
    [DllImport("libc", EntryPoint = "openat", SetLastError = true)] private static extern int NativeOpenAt(int directory, string name, int flags, uint mode);
    [DllImport("libc", EntryPoint = "fstat", SetLastError = true)] private static extern int NativeStat(int file, out LinuxStat stat);
    [DllImport("libc", EntryPoint = "geteuid")] private static extern uint NativeUid();

    private static SafeFileHandle OpenDirectory()
    {
        if (!OperatingSystem.IsLinux() || RuntimeInformation.ProcessArchitecture != Architecture.X64) throw new IOException();
        int fd = NativeOpen(Directory, ReadOnly | NoFollow | DirectoryOnly | CloseOnExec);
        var handle = new SafeFileHandle(fd, ownsHandle: true);
        if (handle.IsInvalid) { handle.Dispose(); throw new IOException(); }
        if (NativeStat(fd, out var stat) != 0 || stat.Device != 2096 || stat.Inode != 8752986 || stat.Uid != 1000
            || stat.Uid != NativeUid() || (stat.Mode & 0xF000) != 0x4000 || (stat.Mode & 0xFFF) != 0x1C0)
        { handle.Dispose(); throw new IOException(); }
        return handle;
    }

    private static FileStream OpenPrivate(SafeFileHandle directory, string name, FileAccess access)
    {
        int fd = NativeOpenAt(directory.DangerousGetHandle().ToInt32(), name,
            (access == FileAccess.Read ? ReadOnly | NonBlock : WriteCreateExclusive) | NoFollow | CloseOnExec, 0x180);
        var handle = new SafeFileHandle(fd, ownsHandle: true);
        if (handle.IsInvalid)
        {
            int error = Marshal.GetLastPInvokeError(); handle.Dispose();
            if (access == FileAccess.Read && error == 2) throw new FileNotFoundException();
            throw new IOException();
        }
        if (NativeStat(fd, out var stat) != 0 || stat.Uid != NativeUid() || (stat.Mode & 0xF000) != 0x8000 || (stat.Mode & 0xFFF) != 0x180
            || access == FileAccess.Read && stat.Size is <= 0 or > 128)
        { handle.Dispose(); throw new IOException(); }
        try { return new FileStream(handle, access); }
        catch { handle.Dispose(); throw; }
    }

    private static void ReadWindow(SafeFileHandle directory)
    {
        if (Volatile.Read(ref WindowEnd) != 0) return;
        try
        {
            using FileStream file = OpenPrivate(directory, "window.control", FileAccess.Read);
            Span<byte> buffer = stackalloc byte[129]; int length = file.Read(buffer);
            if (length is 0 or > 128 || file.ReadByte() != -1) return;
            string[] values = Encoding.ASCII.GetString(buffer[..length]).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (values.Length != 4 || !long.TryParse(values[0], NumberStyles.None, CultureInfo.InvariantCulture, out long run)
                || !long.TryParse(values[1], NumberStyles.None, CultureInfo.InvariantCulture, out long frequency)
                || !long.TryParse(values[2], NumberStyles.None, CultureInfo.InvariantCulture, out long start)
                || !long.TryParse(values[3], NumberStyles.None, CultureInfo.InvariantCulture, out long end)
                || run <= 0 || frequency != Stopwatch.Frequency || start <= 0 || end <= start || end - start > 8 * Stopwatch.Frequency) return;
            long now = Stopwatch.GetTimestamp();
            if (start > now || end > now && end - now > 8 * Stopwatch.Frequency) return;
            Volatile.Write(ref RunOrdinal, run); Volatile.Write(ref WindowStart, start); Volatile.Write(ref WindowEnd, end);
        }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }
        catch { Interlocked.Increment(ref IoFailures); }
    }

    private static string Alias(Role role) => role switch
    {
        Role.Gateway => "gateway", Role.Tenants => "tenants", Role.Projects => "projects", Role.ProjectsWorkers => "projects-workers",
        Role.Folders => "folders", Role.FoldersWorkers => "folders-workers", _ => "unsupported",
    };

    private static void Drain()
    {
        long rows = 0, bytes = 0, capDropped = 0;
        int hardDrain = 0;
        FileStream? output = null;
        SafeFileHandle? directory = null;
        try
        {
            directory = OpenDirectory();
            void DrainRows()
            {
                while (Queue?.Reader.TryRead(out Row row) == true)
                {
                    try
                    {
                        byte[] encoded = JsonSerializer.SerializeToUtf8Bytes(new
                        {
                            role = Alias(row.Role), runOrdinal = row.Run, captureOrdinal = row.Capture, stage = row.Stage.ToString(),
                            elapsedTicks = row.Elapsed, frequency = Stopwatch.Frequency, allocationMode = row.AllocationMode, allocatedBytes = row.Allocated,
                            yielded = row.Yielded, nullValue = row.NullValue, authenticationAccepted = row.AuthenticationAccepted,
                            nonAuthenticationDiscarded = row.NonAuthenticationDiscarded, sensitiveSegmentExcluded = row.SensitiveSegmentExcluded,
                            authenticationOtherExcluded = row.AuthenticationOtherExcluded,
                        });
                        if (rows >= MaximumRows || encoded.Length + 1 > MaximumBytes - bytes) { capDropped++; continue; }
                        output ??= OpenPrivate(directory, Alias(row.Role) + ".jsonl", FileAccess.Write);
                        output.Write(encoded); output.WriteByte(10); rows++; bytes += encoded.Length + 1;
                    }
                    catch { Interlocked.Increment(ref IoFailures); }
                }
            }
            while (true)
            {
                ReadWindow(directory);
                DrainRows();
                long end = Volatile.Read(ref WindowEnd), now = Stopwatch.GetTimestamp();
                if (end > 0 && now >= end)
                {
                    bool close = false;
                    if (AdmissionGate is { } gate) lock (gate)
                    {
                        if (Active == 0 || now - end >= 20 * Stopwatch.Frequency)
                        { WorkerClosed = 1; hardDrain = Active > 0 ? 1 : 0; close = true; }
                    }
                    if (close) { Queue?.Writer.TryComplete(); DrainRows(); break; }
                }
                Thread.Sleep(25);
            }
        }
        catch { Interlocked.Increment(ref IoFailures); }
        finally
        {
            Volatile.Write(ref WorkerClosed, 1);
            try { output?.Dispose(); } catch { Interlocked.Increment(ref IoFailures); }
            try
            {
                if (SeenRole != Role.Unsupported)
                {
                    byte[] receipt = JsonSerializer.SerializeToUtf8Bytes(new
                    {
                        diagnosticOnly = 1, featureAcceptance = 0, role = Alias(SeenRole), runOrdinal = Volatile.Read(ref RunOrdinal),
                        admitted = Volatile.Read(ref Admitted), disposedOrUnwound = Volatile.Read(ref Completed), activeAtDrainEnd = Volatile.Read(ref Active),
                        queuedRowsDropped = Volatile.Read(ref QueueDropped), closedRowsDropped = Volatile.Read(ref ClosedDropped),
                        capRowsDropped = capDropped, ioFailures = Volatile.Read(ref IoFailures), writtenRows = rows, writtenBytes = bytes,
                        queuedAtDrainEnd = Queue?.Reader.Count ?? 0, incompleteHardDrain = hardDrain, countersFinal = hardDrain == 0 ? 1 : 0,
                        lateDisposedOrDropCountersMayIncrease = hardDrain,
                        maximumRows = MaximumRows, maximumBytes = MaximumBytes, queueCapacity = QueueCapacity,
                        windowStartTicks = Volatile.Read(ref WindowStart), windowEndTicks = Volatile.Read(ref WindowEnd), frequency = Stopwatch.Frequency,
                    });
                    if (receipt.Length <= 4096 && directory is not null)
                    { using var file = OpenPrivate(directory, Alias(SeenRole) + ".closed.json", FileAccess.Write); file.Write(receipt); }
                }
            }
            catch { }
            directory?.Dispose();
        }
    }
}
