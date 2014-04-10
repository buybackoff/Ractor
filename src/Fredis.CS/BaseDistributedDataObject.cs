using System;
using System.Runtime.Serialization;

namespace Fredis {

    /// <summary>
    /// Base implementation of IDistributedDataObject
    /// </summary>
    [DataContract]
    public abstract class BaseDistributedDataObject : BaseDataObject, IDistributedDataObject {

        private Guid? _guid;

        [DataMember]
        [Index(true), Required]
        public Guid Guid {
            get {
                if (_guid.HasValue) {
                    return _guid.Value;
                }
                _guid = GuidGenerator.NewGuid();
                return _guid.Value;
            }

            set {
                _guid = value;
            }
        }



        //// TODO!!! this should be inside persistor implementation and only if we need shards!
        //// TODO should create methods like "ReadyForEpoch(epoch) and IncreaseEpoch(from, to, fromUpdatedAt, toUpdatedAt)"
        //// TODO That should store the oldest object that was moved from old 
        ///// <summary>
        ///// Get string ID of the shard where the distributed data object is stored
        ///// </summary>
        ///// <param name="epoch">0 - do not use shards,
        ///// 1 - use 16 shards based on Guid first char,
        ///// 2 - use 256 shards based on Guid first two chars,
        ///// 3 - use 4096 shards based on Guid first two chars,
        ///// higher numbers are meaningless</param>
        ///// <returns></returns>
        //[Obsolete]
        //public string GetShard(int epoch) {
        //    if (epoch == 0) {
        //        return String.Empty;
        //    }
        //    if (epoch > 0 && epoch < 4) {
        //        // TODO be carefull here and research from which position to start, 
        //        // e.g. in MySQL uuid() first three numbers depend on current time, 
        //        // so under high load using this function will load only one shard
        //        // NB: it is meaningless to generate Guid inside a shard since shard depends on Guid
        //        // so we basically depend only on .NET algo for it. Need to check Mono
        //        return Guid.ToString("N").Substring(0, epoch);
        //    }
        //    throw new ArgumentOutOfRangeException("epoch", "Epoch could be from 0 to 3");
        //}
    }
}