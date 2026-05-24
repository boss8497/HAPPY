using System;
using System.Linq;
using Script.GameInfo.Attribute;
using Script.GameInfo.Base;
using Script.GameInfo.Item;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Script.GameInfo.Info {
    [System.Serializable]
    public class ConfigurationInfo : InfoBase {
        public float gravity;
        public float fallGravity;

        public float minJumpTime;
        public float maxJumpTime;

        [Dungeon]
        public int lobby;

        [Reward]
        public int startItems;


        [AssetPath(typeof(Scene))]
        public string startUpScenePath;

        [AssetPath(typeof(Scene))]
        public string titleScenePath;

        [AssetPath(typeof(Scene))]
        public string lobbyScenePath;

        public ConfigurationInfo Clone() {
            return new() {
                gravity          = gravity,
                fallGravity      = fallGravity,
                minJumpTime      = minJumpTime,
                maxJumpTime      = maxJumpTime,
                UID              = UID,
                Components       = Components.ToArray(),
                ID               = ID,
                Name             = Name,
                lobby            = lobby,
                startItems       = startItems,
                startUpScenePath = startUpScenePath,
                titleScenePath   = titleScenePath,
                lobbyScenePath   = lobbyScenePath,
            };
        }
    }
}