using System;

namespace CloudApi.Models
{
    /// <summary>
    ///   The set of information associated with a Ping or Pong operation
    ///   in the game.
    /// </summary>
    /// 
    public sealed class PingPongData : IEquatable<PingPongData>
    {
        /// <summary>The unique identifier of the device that received the ping or responded with the pong.</summary>
        public string DeviceId;

        /// <summary>The date/time associated with the event; this should be represented in UTC.</summary>
        public DateTime EventTimeUtc;

        /// <summary>
        ///   Returns a hash code for this instance.
        /// </summary>
        /// 
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        /// 
        public override int GetHashCode()
        {
            var deviceHash = this.DeviceId?.GetHashCode() ?? 0;
            var timeHash   = this.EventTimeUtc.GetHashCode();
            
            // Numeric values are arbitrary and meant only to add jitter to
            // potential other hash code overrides.  The idea was borrowed from Jon Skeet's 
            // response: https://stackoverflow.com/questions/4420901/c-sharp-equality-checking

            return 72
                * 36 + deviceHash
                * 36 + timeHash;
        }

        /// <summary>
        ///   Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// 
        /// <param name="other">The <see cref="System.Object" /> to compare with this instance.</param>
        /// 
        /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        /// 
        public override bool Equals(object other)
        {
            return Equals(other as PingPongData);
        }

        /// <summary>
        ///   Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// 
        /// <param name="other">An object to compare with this object.</param>
        /// 
        /// <returns><c>true</c> if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, <c>false</c>.</returns>
        /// 
        public bool Equals(PingPongData other)
        {
            if (other == null)
            {
                return false;
            }

            return ((this.DeviceId == other.DeviceId) && (this.EventTimeUtc == other.EventTimeUtc));
        }

        /// <summary>
        ///   Implements the equality operator ==, allowing two instances to be compared.
        /// </summary>
        /// 
        /// <param name="left">The left instance to consider.</param>
        /// <param name="right">The right instance to consider.</param>
        /// 
        /// <returns><c>true</c> if the instances are equal; otherwise, <c>false</c>.</returns>
        /// 
        public static bool operator ==(PingPongData  left, 
                                       PingPongData  right)
        {
            return Object.Equals(left, right);
        }

        /// <summary>
        ///   Implements the equality operator !=, allowing two instances to be compared.
        /// </summary>
        /// 
        /// <param name="left">The left instance to consider.</param>
        /// <param name="right">The right instance to consider.</param>
        /// 
        /// <returns><c>true</c> if the instances are not equal; otherwise, <c>false</c>.</returns>
        /// 
        public static bool operator !=(PingPongData left, 
                                       PingPongData right)
        {
            return !(left == right);
        }
    }
}
