using System;
using System.Linq;
using R3;
using Script.GameData.Data;
using Script.GameData.Model;
using Script.GameInfo.Character;
using Script.GameInfo.Info.Stat;
using Script.GameInfo.Item;
using Script.GameInfo.Table;
using Script.GamePlay.ECS.Component;
using Script.Utility.Runtime;
using UnityEngine;

//Reactive 필드 및 로직
namespace Script.GamePlay.Character {
    public partial class Character {
        //Initialize OnNext 순서대로
        public ReactiveProperty<CharacterState> State     { get; private set; } = new();
        public ReactiveProperty<double>         Health    { get; private set; } = new();
        public ReactiveProperty<double>         MaxHealth { get; private set; } = new();
        public ReactiveProperty<ItemInfo>       ItemInfo  { get; private set; } = new();

        public ReadOnlyReactiveProperty<ItemData> Item           { get; private set; }
        public ReadOnlyReactiveProperty<bool>     Initialized    { get; private set; }
        public ReadOnlyReactiveProperty<bool>     Jumping        { get; private set; }
        public ReadOnlyReactiveProperty<bool>     Running        { get; private set; }
        public ReadOnlyReactiveProperty<bool>     Die            { get; private set; }
        public ReadOnlyReactiveProperty<bool>     SystemControl  { get; private set; }
        public ReadOnlyReactiveProperty<bool>     CollisionState { get; private set; }
        public ReadOnlyReactiveProperty<bool>     OutSideMap     { get; private set; }
        public ReadOnlyReactiveProperty<bool>     InSideMap      { get; private set; }


        private DisposableBag _reactiveDisposableBag;


        private void InitializeReactiveProperty() {
            _reactiveDisposableBag.Dispose();
            _reactiveDisposableBag = new();
            
            var characterItemInfo = GameInfoManager.Instance.GetCollection<ItemInfo>().FirstOrDefault(r => r.characterInfoUid == CharacterInfo.UID);


            Health.Subscribe(health => {
                      if ((Die?.CurrentValue ?? true) || (Initialized?.CurrentValue ?? false) == false) return;

                      // 죽음
                      if (health <= 0) {
                          AddState(CharacterState.Die);
                          SetEnabledTag<UnitDieEnable>(true);
                      }
                  })
                  .AddTo(ref _reactiveDisposableBag);

            if (CharacterInfo.type == CharacterType.Character) {
                if (characterItemInfo == null) {
                    throw new Exception($"캐릭터 {CharacterInfo.UID}에 해당하는 ItemInfo가 존재하지 않습니다.");
                }
                Item = ItemInfo.CombineLatest(_itemService.SubscribeItemInfoUidUpdate(characterItemInfo.UID), (info, infoSubscribe) => (info, infoSubscribe))
                               .Select(data => {
                                   if (data.info == null) return Observable.Return<ItemData>(null);
                                   var item = _itemService.GetItem(data.info.UID);
                                   return item == null ? Observable.Return<ItemData>(null) : item.AsObservable();
                               })
                               .Switch()
                               .ToReadOnlyReactiveProperty()
                               .AddTo(ref _reactiveDisposableBag);   
            }
            else {
                Item = ItemInfo.Select(i => {
                                   if (i == null) return Observable.Return<ItemData>(null);
                                   var item = _itemService.GetItem(i.UID);
                                   return item == null ? Observable.Return<ItemData>(null) : item.AsObservable();
                               })
                               .Switch()
                               .DistinctUntilChanged()
                               .ToReadOnlyReactiveProperty()
                               .AddTo(ref _reactiveDisposableBag);
            }

            Initialized = State.Select(i => (i & CharacterState.Initialized) != 0)
                               .DistinctUntilChanged()
                               .ToReadOnlyReactiveProperty()
                               .AddTo(ref _reactiveDisposableBag);

            Jumping = State.Select(i => (i & CharacterState.Jumping) != 0)
                           .DistinctUntilChanged()
                           .ToReadOnlyReactiveProperty()
                           .AddTo(ref _reactiveDisposableBag);

            Running = State.Select(i => (i & CharacterState.Running) != 0)
                           .DistinctUntilChanged()
                           .ToReadOnlyReactiveProperty()
                           .AddTo(ref _reactiveDisposableBag);

            Die = State.Select(i => (i & CharacterState.Die) != 0)
                       .DistinctUntilChanged()
                       .ToReadOnlyReactiveProperty()
                       .AddTo(ref _reactiveDisposableBag);

            SystemControl = State.CombineLatest(Initialized, (state, initialized) => !initialized || (state & CharacterState.SystemControl) != 0)
                                 .DistinctUntilChanged()
                                 .ToReadOnlyReactiveProperty()
                                 .AddTo(ref _reactiveDisposableBag);

            CollisionState = State.Select(i => (i & CharacterState.Collision) != 0)
                                  .DistinctUntilChanged()
                                  .ToReadOnlyReactiveProperty()
                                  .AddTo(ref _reactiveDisposableBag);

            OutSideMap = State.Select(i => (i & CharacterState.OutSideMap) != 0)
                              .DistinctUntilChanged()
                              .ToReadOnlyReactiveProperty()
                              .AddTo(ref _reactiveDisposableBag);
            
            InSideMap = State.Select(i => (i & CharacterState.InSideMap) != 0)
                             .DistinctUntilChanged()
                             .ToReadOnlyReactiveProperty()
                             .AddTo(ref _reactiveDisposableBag);

            Item.Select(i => i == null ? Observable.Empty<int>() : Observable.Merge(i.Level, i.Grade, i.Tier))
                .Subscribe((data) => {
                    _status.Clear();
                    using var _ = CreateValueContext();
                    foreach (var statusUid in _characterInfo.statusUids) {
                        _status.Add(GameInfoManager.Instance.Get<StatusInfo>(statusUid));
                    }

                    UpdateStatus();
                })
                .AddTo(ref _reactiveDisposableBag);

            // 여기서는 상태가 겹쳐 있을 거임
            State.Subscribe(SyncHitbox)
                 .AddTo(ref _reactiveDisposableBag);


            SystemControl.CombineLatest(Initialized, (systemControl, initialized) => (systemControl, initialized))
                         .Subscribe(state => {
                             if (state.initialized == false) return;
                             SetEnabledTag<UnitSystemControlEnable>(state.systemControl);
                         })
                         .AddTo(ref _reactiveDisposableBag);

            InSideMap.CombineLatest(Initialized, (inSideMap, initialized) => (inSideMap, initialized))
                      .SubscribeAwait(async (state, ct) => {
                          if (state.initialized == false) return;
                          if (state.inSideMap) {
                              gameObject.SetActiveSafe(true);
                              await StartAsync();
                              SetEnabledTag<UnitCollisionEnable>(true);
                          }
                      })
                      .AddTo(ref _reactiveDisposableBag);
            
            OutSideMap.CombineLatest(Initialized, (outSideMap, initialized) => (outSideMap, initialized))
                      .Subscribe(state => {
                          if (state.initialized == false) return;
                          if (state.outSideMap) {
                              if (!IsPlayer) {
                                  _stageManager.AddRemoveEnemy(this);
                              }
                              SetEnabledTag<UnitCollisionEnable>(false);
                          }
                      })
                      .AddTo(ref _reactiveDisposableBag);

            Die.CombineLatest(Initialized, (die, initialized) => (die, initialized))
               .Subscribe(state => {
                   if (state.initialized == false) return;
                   if (state.die) {
                       SetEnabledTag<UnitCollisionEnable>(false);
                   }
               })
               .AddTo(ref _reactiveDisposableBag);


            switch (CharacterInfo.type) {
                case CharacterType.Character:
                    Running.CombineLatest(Initialized, (running, initialized) => (running, initialized))
                           .Subscribe(state => {
                               if (state.initialized == false) return;
                               SetEnabledTag<UnitRunningEnable>(state.running);
                           })
                           .AddTo(ref _reactiveDisposableBag);

                    Jumping.CombineLatest(Initialized, (jumping, initialized) => (jumping, initialized))
                           .Subscribe(state => {
                               if (state.initialized == false) return;
                               if (state.jumping) {
                                   ResetJumpingStatus();
                               }

                               SetEnabledTag<UnitJumpingEnable>(state.jumping);
                           })
                           .AddTo(ref _reactiveDisposableBag);

                    break;
            }


            ItemInfo.OnNext(characterItemInfo);
            State.OnNext((_stageManager?.SystemControl?.CurrentValue ?? false) ? CharacterState.SystemControl : CharacterState.None);
            Health.OnNext(Status.Hp);
            MaxHealth.OnNext(Status.Hp);
        }

        private void ReleaseReactiveProperty() {
            _reactiveDisposableBag.Dispose();
        }
    }
}