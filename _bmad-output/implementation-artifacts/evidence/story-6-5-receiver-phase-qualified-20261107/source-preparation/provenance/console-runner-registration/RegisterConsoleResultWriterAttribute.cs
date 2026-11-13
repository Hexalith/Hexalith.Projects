namespace Xunit.Runner.Common;

/// <summary>
/// Used to decorate xUnit.net test assemblies to indicate the availability of a custom
/// result writer (in console mode).
/// </summary>
/// <param name="id">The ID of the result writer. Will add a command line switch for the writer
/// as <c>"-result-{ID}"</c>.</param>
/// <param name="resultWriterType">The type of the result writer to register. The type must
/// implement <see cref="IConsoleResultWriter"/>.</param>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed partial class RegisterConsoleResultWriterAttribute(
	string id,
	Type resultWriterType) :
		Attribute
{
	/// <summary>
	/// Gets the ID of the result writer.
	/// </summary>
	public string ID => Guard.ArgumentNotNull(id);

	/// <summary>
	/// Gets the type of the result writer to register.
	/// </summary>
	public Type ResultWriterType => Guard.ArgumentNotNull(resultWriterType);
}

/// <summary>
/// Used to decorate xUnit.net test assemblies to indicate the availability of a custom
/// result writer (in console mode).
/// </summary>
/// <param name="id">The ID of the result writer. Will add a command line switch for the writer
/// as <c>"-result-{ID}"</c>.</param>
/// <typeparam name="TResultWriter">The type of the result writer to register</typeparam>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed partial class RegisterConsoleResultWriterAttribute<TResultWriter>(string id) : Attribute
	where TResultWriter : IConsoleResultWriter
{
	/// <summary>
	/// Gets the ID of the result writer.
	/// </summary>
	public string ID => Guard.ArgumentNotNull(id);

	/// <summary>
	/// Gets the type of the result writer to register.
	/// </summary>
	public Type ResultWriterType => typeof(TResultWriter);
}
