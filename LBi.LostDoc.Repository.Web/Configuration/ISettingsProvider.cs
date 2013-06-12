namespace LBi.LostDoc.Repository.Web.Configuration
{
    public interface ISettingsProvider
    {
        T GetValue<T>(string key);

        void SetValue<T>(string key, T value);
    }
}