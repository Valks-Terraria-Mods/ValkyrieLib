using System;

namespace ValkyrieLib;

/// <summary>
/// This element is closable.
/// </summary>
public interface IClosable
{
    /// <summary>
    /// Requests a close event.
    /// </summary>
    event Action CloseRequested;
}
