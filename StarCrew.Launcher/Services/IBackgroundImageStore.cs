namespace StarCrew.Launcher.Services;

/// <summary>背景图片的本地副本存储。</summary>
internal interface IBackgroundImageStore
{
    /// <summary>将用户选择的图片复制到应用数据目录，返回副本路径。</summary>
    /// <remarks>仅接受 PNG、JPG/JPEG 与 BMP；读取或写入失败时抛出 IO 相关异常。</remarks>
    Task<string> ImportAsync(string sourcePath);

    /// <summary>删除自定义图片副本；文件不存在或删除失败时忽略。</summary>
    void Delete(string path);
}
