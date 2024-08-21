namespace Shashlik.EventBus
{
    /// <summary>
    /// 消息状态
    /// </summary>
    public enum MessageStatus : byte
    {
        /// <summary>
        /// 未处理
        /// </summary>
        None = 0,

        /// <summary>
        /// 失败
        /// </summary>
        Failed,

        /// <summary>
        /// 定时
        /// </summary>
        Scheduled,

        /// <summary>
        /// 成功
        /// </summary>
        Succeeded,

        /// <summary>
        /// 已删除
        /// </summary>
        Deleted,
    }
}