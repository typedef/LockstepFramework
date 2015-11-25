﻿using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
namespace Lockstep {
    public static class TestManager {  
        static TestManager () {
            ClientManager.NetworkHelper.OnTestData += HandleOnTestData;
        }
        static Dictionary<int,List<int>> frameHashes = new Dictionary<int, List<int>>();
        static void HandleOnTestData (byte[] obj) {
            int frame = BitConverter.ToInt32 (obj, 0);
            int pos = 4;
            List<int> hashes = new List<int>();
            while (pos < obj.Length - (sizeof(int) - 1)) {
                int hash = BitConverter.ToInt32 (obj, pos);
                hashes.Add (hash);
                pos += sizeof (int);
            }
            if (frameHashes.ContainsKey (frame)) {
                frameHashes[frame].AddRange (hashes);
            }
            else {
                frameHashes.Add (frame,hashes);
            }


        }
        private static int lastFrameSent;
        public static void Simulate () {
            List<int> hashes;
            if (frameHashes.TryGetValue (LockstepManager.FrameCount - 10, out hashes)) {
                int mainHash = hashes[0];
                bool desynced = false;
                for (int i = 1; i < hashes.Count; i++) {
                    if (mainHash != hashes[i]) {
                        desynced = true;
                    }
                }
                if (desynced) {
                    Debug.LogError ("DESYCNED");
                }
            }
            if (LockstepManager.FrameCount > lastFrameSent) {
                List<byte> newMessage = new List<byte>();
                newMessage.AddRange (BitConverter.GetBytes (LockstepManager.FrameCount));
                newMessage.AddRange (BitConverter.GetBytes (LockstepManager.GetStateHash()));
                if (ClientManager.NetworkHelper.IsServer) {
                    ClientManager.NetworkHelper.SendMessageToAll (MessageType.Test, newMessage.ToArray ());
                }
                {
                    List<int> newHashes = new List<int>();
                    newHashes.Add (LockstepManager.GetStateHash ());
                    if (frameHashes.ContainsKey(LockstepManager.FrameCount)) {
                        frameHashes[LockstepManager.FrameCount].AddRange (newHashes);
                    }
                    else {
                        frameHashes.Add (LockstepManager.FrameCount,newHashes);
                    }
                }
                lastFrameSent = LockstepManager.FrameCount;
            }
        }
    }
}