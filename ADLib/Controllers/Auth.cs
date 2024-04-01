using Microsoft.Identity.Client;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ADLib.Controllers
{
    public class Auth
    {
        public string ClientId { get; }

        public string TenantId { get; }

        public string AccessToken { get; private set; }

        public string[] Scopes { get; } 

        readonly IPublicClientApplication app;

        readonly string cacheFilePath = Path.Combine(
                                     Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                     $"msal_cache_{AppDomain.CurrentDomain}.dat");

        public Auth(string clientId, string[] scopes, string tenantId, string redict = "http://localhost")
        {
            ClientId = clientId;
            TenantId = tenantId;
            Scopes = scopes;

            app = PublicClientApplicationBuilder.Create(ClientId)
               .WithRedirectUri(redict)
               .WithAuthority(AzureCloudInstance.AzurePublic, TenantId)
               .WithDefaultRedirectUri()
               .Build();
        }

        public void CreateCacheHelper(string datPath = null)
        {
            if(datPath == null) datPath = cacheFilePath;
            var cacheHelper = new TokenCacheHelper(datPath);
            app.UserTokenCache.SetBeforeAccess(cacheHelper.BeforeAccessNotification);
            app.UserTokenCache.SetAfterAccess(cacheHelper.AfterAccessNotification);
            app.UserTokenCache.SetBeforeWrite(cacheHelper.BeforeWriteNotification);
        }

        public async Task<string> GetAccessTokenAsync()
        {
            var accounts = await app.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();

            try
            {
                // Если в кэше есть аккаунт, попытаемся получить токен без интерактивного входа
                var silentResult = await app.AcquireTokenSilent(Scopes, firstAccount)
                                            .ExecuteAsync();
                AccessToken = silentResult.AccessToken;
                return silentResult.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                // Если не удалось получить токен немого, запрашиваем интерактивный вход
                try
                {
                    var interactiveResult = await app.AcquireTokenInteractive(Scopes)
                                                     .WithAccount(firstAccount)
                                                     .ExecuteAsync();
                    AccessToken = interactiveResult.AccessToken;
                    return interactiveResult.AccessToken;
                }
                catch
                {
                    try
                    {
                        var result = app.AcquireTokenInteractive(Scopes).ExecuteAsync().Result;
                        AccessToken = result.AccessToken;
                        return result.AccessToken;
                    }
                    catch (MsalClientException exmsal)
                    {
                        Debug.WriteLine("Ошибка при аутентификации: " + exmsal.Message);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }
    }
}
