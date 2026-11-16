extern alias PackageFixtureHost;

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Streams;
using Hexalith.Projects.PackageFixtures;

using PackageHost = PackageFixtureHost::Hexalith.Projects.PackageFixtures.AppHost;

using Shouldly;

using Xunit;

namespace Hexalith.Projects.Integration.Tests;

/// <summary>Checks selected image identity and shared SDK contracts without running an AppHost.</summary>
public sealed class PackageOwnerLoadContextTests
{
    /// <summary>A cached assembly cannot mask a replacement of the selected path.</summary>
    [Fact]
    public async Task ReplacingAnAlreadyLoadedPathIsRefused()
    {
        FixtureScratch scratch = new(false);
        await using var cleanup = scratch.ConfigureAwait(true);
        string name = "FixtureOwner" + Guid.NewGuid().ToString("N");
        string path = scratch.File(name + ".dll");
        WriteProbe(path, name, 1);
        var context = new PackageHost.PackageOwnerLoadContext(path);
        Assembly first = context.LoadOwner(path);
        File.Move(path, path + ".old");
        WriteProbe(path, name, 2);

        Should.Throw<InvalidOperationException>(() => context.LoadOwner(path)).Message.ShouldContain("changed after loading");
        first.GetType("Probe")!.GetMethod("Read")!.Invoke(null, null).ShouldBe(1);
    }

    /// <summary>Ordinary dependencies must execute the selected closure even if their own bytes match Default.</summary>
    [Fact]
    public async Task IdenticalMiddleAssemblyCannotImportTheDefaultLeaf()
    {
        FixtureScratch scratch = new(false);
        await using var cleanup = scratch.ConfigureAwait(true);
        string suffix = Guid.NewGuid().ToString("N");
        string defaults = scratch.File("defaults"), selected = scratch.File("selected");
        Directory.CreateDirectory(defaults);
        Directory.CreateDirectory(selected);
        string leafName = "FixtureLeaf" + suffix, middleName = "FixtureMiddle" + suffix, ownerName = "FixtureOwner" + suffix;
        string leafPath = Path.Combine(defaults, leafName + ".dll"), middlePath = Path.Combine(defaults, middleName + ".dll");
        WriteProbe(leafPath, leafName, 1);
        Assembly leaf = AssemblyLoadContext.Default.LoadFromAssemblyPath(leafPath);
        WriteProbe(middlePath, middleName, 0, leaf.GetType("Probe")!.GetMethod("Read"));
        Assembly middle = AssemblyLoadContext.Default.LoadFromAssemblyPath(middlePath);
        middle.GetType("Probe")!.GetMethod("Read")!.Invoke(null, null).ShouldBe(1);
        File.Copy(middlePath, Path.Combine(selected, middleName + ".dll"));
        WriteProbe(Path.Combine(selected, leafName + ".dll"), leafName, 2);
        string ownerPath = Path.Combine(selected, ownerName + ".dll");
        WriteProbe(ownerPath, ownerName, 0, middle.GetType("Probe")!.GetMethod("Read"));
        var context = new PackageHost.PackageOwnerLoadContext(ownerPath);

        context.LoadOwner(ownerPath).GetType("Probe")!.GetMethod("Read")!.Invoke(null, null).ShouldBe(2);
    }

    /// <summary>An output-local link cannot select an owner from outside that output.</summary>
    [Fact]
    public async Task LinkedOwnerOutsideTheOutputIsRefused()
    {
        FixtureScratch scratch = new(false);
        await using var cleanup = scratch.ConfigureAwait(true);
        string directory = scratch.File("selected");
        Directory.CreateDirectory(directory);
        string outside = scratch.File("outside.dll"), linked = Path.Combine(directory, "owner.dll");
        WriteProbe(outside, "FixtureLinkedOwner" + Guid.NewGuid().ToString("N"), 1);
        File.CreateSymbolicLink(linked, outside);
        var context = new PackageHost.PackageOwnerLoadContext(linked);

        Should.Throw<InvalidOperationException>(() => context.LoadOwner(linked)).Message.ShouldContain("regular output file");
    }

    /// <summary>A default-context image with the same identity cannot replace a selected owner.</summary>
    [Fact]
    public async Task SameIdentityLoadsTheSelectedBytesAndReusesItsOwnerContext()
    {
        FixtureScratch scratch = new(false);
        await using var cleanup = scratch.ConfigureAwait(true);
        Assembly existing = typeof(PackageHost.PackageActualOwnerComposition).Assembly;
        string selected = scratch.File(Path.GetFileName(existing.Location));
        byte[] bytes = [.. File.ReadAllBytes(existing.Location), 0];
        File.WriteAllBytes(selected, bytes);
        var context = new PackageHost.PackageOwnerLoadContext(selected);

        Assembly loaded = context.LoadOwner(selected);

        loaded.FullName.ShouldBe(existing.FullName);
        loaded.ShouldNotBeSameAs(existing);
        AssemblyLoadContext.GetLoadContext(loaded).ShouldBeSameAs(context);
        context.LoadOwner(selected).ShouldBeSameAs(loaded);
        SHA256.HashData(File.ReadAllBytes(loaded.Location)).ShouldBe(SHA256.HashData(bytes));
    }

    /// <summary>Identical SDK and JSON contracts retain their AppHost type identity.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IdenticalSharedContractsUseTheDefaultContext(bool json)
    {
        FixtureScratch scratch = new(false);
        await using var cleanup = scratch.ConfigureAwait(true);
        Assembly shared = json ? typeof(JsonSerializer).Assembly : typeof(StreamReadRequest).Assembly;
        string selected = scratch.File(Path.GetFileName(shared.Location));
        File.Copy(shared.Location, selected);
        var context = new PackageHost.PackageOwnerLoadContext(selected);

        context.LoadFromAssemblyName(shared.GetName()).ShouldBeSameAs(shared);
    }

    /// <summary>Matching SDK assembly names do not hide byte drift.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChangedSharedContractBytesAreRefused(bool json)
    {
        FixtureScratch scratch = new(false);
        await using var cleanup = scratch.ConfigureAwait(true);
        Assembly shared = json ? typeof(JsonSerializer).Assembly : typeof(StreamReadRequest).Assembly;
        string selected = scratch.File(Path.GetFileName(shared.Location));
        File.WriteAllBytes(selected, [.. File.ReadAllBytes(shared.Location), 0]);
        var context = new PackageHost.PackageOwnerLoadContext(selected);

        FileLoadException failure = Should.Throw<FileLoadException>(() => context.LoadFromAssemblyName(shared.GetName()));

        failure.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldContain("SDK contract differs");
    }

    /// <summary>Another image with the same identity cannot substitute inside an owner's context either.</summary>
    [Fact]
    public async Task AnAlreadyLoadedOwnerCannotReplaceAnotherSelectedPath()
    {
        FixtureScratch scratch = new(false);
        await using var cleanup = scratch.ConfigureAwait(true);
        Assembly existing = typeof(PackageHost.PackageActualOwnerComposition).Assembly;
        string first = scratch.File("first.dll"), second = scratch.File("second.dll");
        File.Copy(existing.Location, first);
        File.WriteAllBytes(second, [.. File.ReadAllBytes(existing.Location), 0]);
        var context = new PackageHost.PackageOwnerLoadContext(first);
        _ = context.LoadOwner(first);

        Should.Throw<InvalidOperationException>(() => context.LoadOwner(second)).Message.ShouldContain("selected executable artifact");
    }

    private static void WriteProbe(string path, string name, int value, MethodInfo? next = null)
    {
        var assembly = new PersistedAssemblyBuilder(new AssemblyName(name) { Version = new Version(1, 0, 0, 0) }, typeof(object).Assembly);
        ModuleBuilder module = assembly.DefineDynamicModule(name);
        TypeBuilder type = module.DefineType("Probe", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
        MethodBuilder method = type.DefineMethod("Read", MethodAttributes.Public | MethodAttributes.Static, typeof(int), Type.EmptyTypes);
        ILGenerator il = method.GetILGenerator();
        if (next is null) il.Emit(OpCodes.Ldc_I4, value);
        else il.Emit(OpCodes.Call, next);
        il.Emit(OpCodes.Ret);
        _ = type.CreateType();
        using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        assembly.Save(output);
    }
}
