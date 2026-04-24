using System;

namespace ValkyrieLib;

public interface IClosable
{
    event Action CloseRequested;
}
