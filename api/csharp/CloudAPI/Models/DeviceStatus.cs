namespace CloudApi.Models
{
    /// <summary>
    ///   The status of a device, normally occurring during a registration-related operation.
    /// </summary>
    /// 
    public class DeviceStatus
    {
        /// <summary>The device associated with the status.</summary>
        public readonly GameDevice Device;

        /// <summary>The state of the device, post-operation.</summary>
        public readonly DeviceState Status;

        /// <summary>
        ///   Initializes a new instance of the <see cref="DeviceStatus"/> class.
        /// </summary>
        /// 
        /// <param name="device">The device associated with the status.</param>
        /// <param name="status">The state of the device, post-operation.</param>
        /// 
        public DeviceStatus(GameDevice  device,
                            DeviceState status)
        {
            this.Device = device;
            this.Status = status;
        }
    }
}
