namespace MicroSocialPlatform.Services
{
   public interface ICommentValidationService
    {
        Task<bool> IsCommentValidAsync(string text);
    }
}
