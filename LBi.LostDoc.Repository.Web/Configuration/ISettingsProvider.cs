namespace LBi.LostDoc.Repository.Web.Configuration
{
    public interface ISettingsProvider
    {
        bool TryGetValue<T>(string key, out T value);

        T GetValue<T>(string key);

        T GetValueOrDefault<T>(string key, T defaultValue = default(T));

        void SetValue<T>(string key, T value);
    }
}