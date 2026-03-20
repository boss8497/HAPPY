using System;
using System.Linq;
using Script.GameInfo.Base;
using UnityEngine;

namespace Script.GameInfo.Info {
    [System.Serializable]
    public class ConfigurationInfo : InfoBase {
        public float gravity;
        public float fallGravity;
        
        public float  minJumpTime;
        public float  maxJumpTime;
        
        
        public ConfigurationInfo Clone() {
            return new() {
                gravity     = gravity,
                fallGravity = fallGravity,
                minJumpTime = minJumpTime,
                maxJumpTime = maxJumpTime,
                UID         = UID,
                Components  = Components.ToArray(),
                ID          = ID,
                Name        = Name,
            };
        }
    }
}