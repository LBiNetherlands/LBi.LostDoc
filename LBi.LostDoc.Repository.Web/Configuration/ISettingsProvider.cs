namespace LBi.LostDoc.Repository.Web.Configuration
{
    public interface ISettingsProvider
    {
        bool TryGetValue<T>(string key, out T value);

        T GetValue<T>(string key);

        void SetValue<T>(string key, T value);
    }
}