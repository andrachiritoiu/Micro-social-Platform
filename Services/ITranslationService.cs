namespace MicroSocialPlatform.Services
{
    public interface ITranslationService
    {
        Task<string> TranslateAsync(string text, string targetLang);
    }
}
