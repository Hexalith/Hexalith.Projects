namespace Xunit.Runner.Common;

partial class RegisterConsoleResultWriterAttribute : IRegisterConsoleResultWriterAttribute
{ }

/// <remarks>
/// .NET Framework does not support generic attributes. Please use the non-generic <see cref="RegisterConsoleResultWriterAttribute"/>
/// when targeting .NET Framework.
/// </remarks>
partial class RegisterConsoleResultWriterAttribute<TResultWriter> : IRegisterConsoleResultWriterAttribute
{ }
