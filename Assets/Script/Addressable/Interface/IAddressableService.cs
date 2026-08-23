using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Script.Addressable {
    public interface IAddressableService {
        bool IsInitialized { get; }

        UniTask LoadAppLabelsAsync(CancellationToken ct = default);

        UniTask<bool> HasInternetConnectionAsync(
            int               timeout    = 3,
            CancellationToken ct = default
        );

        /// <summary>
        /// key로 로드한 에셋을 RefCount 기반으로 캐싱한다.
        /// 반환된 Handle을 Dispose()해서 사용 종료를 알려야 하며,
        /// 실제 Addressables.Release()는 RefCount가 0이 된 후 일정 시간(유예시간) 뒤에 내부 타이머가 처리한다.
        /// </summary>
        UniTask<AddressableCacheHandle<T>> LoadAsync<T>(string key, CancellationToken ct = default) where T : Object;

        /// <summary>
        /// LoadAsync 캐시의 점검 주기 / 유예시간을 설정한다. GameSettingData 로드 완료 후 호출된다.
        /// </summary>
        void ConfigureCache(float checkIntervalSeconds, float releaseGraceSeconds);
    }
}