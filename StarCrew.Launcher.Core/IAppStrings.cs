namespace StarCrew.Launcher;

/// <summary>用户可见文案的唯一来源（Resources.resw）抽象：界面层经 DI 注入实现，测试注入 resw 解析版。</summary>
public interface IAppStrings
{
    /// <summary>按资源键取文案，键不存在时实现方应抛错以便尽早发现拼写问题。</summary>
    string GetString(string key);

    /// <summary>取带 {0} 占位的文案并格式化。</summary>
    string Format(string key, params object?[] args);
}
