namespace Talk.Core.Rag;

/// <summary>
/// A chunk of corpus text before its embedding has been computed.
/// </summary>
public sealed record UnembeddedChunk(string Id, string Text);
