namespace StarCrew.Launcher;

public interface IAppStrings
{
    /// <summary>通过给定的 <paramref name="key"/> 获取对应文案。</summary>
    string GetString(string key);

    /// <summary>通过给定的 <paramref name="key"/> 获取并格式化对应文案。</summary>
    string Format(string key, params object?[] args);
}
