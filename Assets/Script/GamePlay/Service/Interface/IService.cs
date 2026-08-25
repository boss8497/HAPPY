using System;

namespace Script.GamePlay.Service.Interface {
    public interface IService : IDisposable {
        bool Initialized { get; }
    }
}