namespace GameFrameX.EventBus
{
    /// <summary>
    /// 消息id生成器,需保证全局唯一，默认guid
    /// </summary>
    public interface IMessageIdGenerator
    {
        /// <summary>
        /// 消息id生成器,需保证全局唯一，最大32位
        /// </summary>
        /// <returns></returns>
        string GenerateId();
    }
}