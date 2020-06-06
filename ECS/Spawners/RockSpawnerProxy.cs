//using System;
//using Unity.Entities;
//using UnityEngine;

//[Serializable]
//public struct RockSpawner : ISharedComponentData, IEquatable<RockSpawner>
//{
//    public GameObject[] prefab;
//    public int count;

//    public bool Equals(RockSpawner other)
//    {
//        return
//            prefab == other.prefab &&
//            count == other.count;
//    }


//    public override int GetHashCode()
//    {
//        int hash = count.GetHashCode();
//        if (!ReferenceEquals(prefab, null))
//            hash ^= prefab.GetHashCode();

//        return hash;
//    }
//}

//public class RockSpawnerProxy : SharedComponentDataProxy<RockSpawner> { }
